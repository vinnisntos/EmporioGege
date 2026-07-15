using System.Data;
using Dapper;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Enums;
using EmporioGege.Core.Exceptions;
using EmporioGege.Core.Interfaces;
using Npgsql;

namespace EmporioGege.Application.Services
{
    public class VendaService(IDbConnectionFactory connectionFactory, ITenantProvider tenantProvider) : IVendaService
    {
        private const int MaxTentativas = 3;

        public async Task<ResultadoVendaDto> FinalizarVendaAsync(FinalizarVendaDto dto, CancellationToken ct = default)
        {
            if (dto.Itens.Count == 0)
                throw new ArgumentException("A venda precisa ter ao menos um item.", nameof(dto));

            var tenantId = tenantProvider.RequireTenantId();

            for (var tentativa = 1; tentativa <= MaxTentativas; tentativa++)
            {
                await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
                await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);

                try
                {
                    await connectionFactory.SetTenantContextAsync(connection, transaction, tenantId, ct);

                    // Baixa todos os itens na MESMA transação: se um item não tiver estoque,
                    // EstoqueInsuficienteException derruba a transação inteira — sem baixa
                    // parcial do carrinho, diferente de chamar EstoqueService item a item.
                    var itensBaixados = new List<ResultadoBaixaEstoqueDto>();
                    foreach (var item in dto.Itens)
                    {
                        var baixaDto = new BaixaEstoqueDto(item.ProdutoId, item.Quantidade, dto.TipoOrigem, item.TipoPreco, dto.ReferenciaExterna);
                        itensBaixados.Add(await EstoqueOperacoes.DecrementarAsync(connection, transaction, tenantId, baixaDto, ct));
                    }

                    // Preço/custo vêm do produto lido dentro da transação (EstoqueOperacoes),
                    // nunca do cliente — evita venda finalizar com preço adulterado no request.
                    var totalVenda = dto.Itens.Zip(itensBaixados, (item, r) => r.PrecoUnitarioAplicado * item.Quantidade).Sum();
                    var totalCusto = dto.Itens.Zip(itensBaixados, (item, r) => r.CustoUnitarioAplicado * item.Quantidade).Sum();

                    // Fiado: trava a linha do cliente (FOR UPDATE) e checa o limite de crédito
                    // na MESMA transação da venda — se estourar, a venda inteira reverte junto
                    // com a baixa de estoque já feita acima (nada fica pela metade).
                    if (string.Equals(dto.MetodoPagamento, "FIADO", StringComparison.OrdinalIgnoreCase))
                    {
                        if (dto.ClienteId is not { } clienteIdFiado)
                            throw new InvalidOperationException("Venda fiado precisa de um cliente selecionado.");

                        var cliente = await connection.QuerySingleOrDefaultAsync<ClienteCreditoRow>(new CommandDefinition(
                            "SELECT limite_credito AS LimiteCredito, saldo_devedor AS SaldoDevedor FROM clientes WHERE id = @ClienteId AND tenant_id = @TenantId FOR UPDATE",
                            new { ClienteId = clienteIdFiado, TenantId = tenantId }, transaction, cancellationToken: ct));

                        if (cliente is null)
                            throw new InvalidOperationException($"Cliente {clienteIdFiado} não encontrado.");

                        var novoSaldo = cliente.SaldoDevedor + totalVenda;
                        if (novoSaldo > cliente.LimiteCredito)
                            throw new LimiteCreditoExcedidoException(clienteIdFiado, cliente.LimiteCredito, cliente.SaldoDevedor, totalVenda);

                        await connection.ExecuteAsync(new CommandDefinition(
                            "UPDATE clientes SET saldo_devedor = @NovoSaldo WHERE id = @ClienteId AND tenant_id = @TenantId",
                            new { NovoSaldo = novoSaldo, ClienteId = clienteIdFiado, TenantId = tenantId }, transaction, cancellationToken: ct));
                    }

                    var vendaId = Guid.NewGuid();
                    var dataVenda = DateTime.UtcNow;
                    var tipoOrigemTexto = dto.TipoOrigem switch
                    {
                        TipoOrigemVenda.ZeDelivery => "ZEDELIVERY",
                        TipoOrigemVenda.Comanda => "COMANDA",
                        _ => "BALCAO"
                    };

                    await connection.ExecuteAsync(new CommandDefinition(
                        """
                        INSERT INTO vendas (id, tenant_id, turno_id, cliente_id, comanda_id, tipo_origem, status, emitir_nota_fiscal, total_custo, total_venda, metodo_pagamento, data_venda)
                        VALUES (@Id, @TenantId, @TurnoId, @ClienteId, @ComandaId, @TipoOrigem, 'FECHADA', @EmitirNotaFiscal, @TotalCusto, @TotalVenda, @MetodoPagamento, @DataVenda)
                        """,
                        new
                        {
                            Id = vendaId,
                            TenantId = tenantId,
                            dto.TurnoId,
                            dto.ClienteId,
                            dto.ComandaId,
                            TipoOrigem = tipoOrigemTexto,
                            dto.EmitirNotaFiscal,
                            TotalCusto = totalCusto,
                            TotalVenda = totalVenda,
                            dto.MetodoPagamento,
                            DataVenda = dataVenda
                        },
                        transaction, cancellationToken: ct));

                    foreach (var (item, resultado) in dto.Itens.Zip(itensBaixados))
                    {
                        var subtotal = resultado.PrecoUnitarioAplicado * item.Quantidade;
                        await connection.ExecuteAsync(new CommandDefinition(
                            """
                            INSERT INTO vendas_itens (id, tenant_id, venda_id, produto_id, quantidade, preco_unitario_aplicado, custo_unitario_aplicado, subtotal)
                            VALUES (gen_random_uuid(), @TenantId, @VendaId, @ProdutoId, @Quantidade, @PrecoUnitario, @CustoUnitario, @Subtotal)
                            """,
                            new
                            {
                                TenantId = tenantId,
                                VendaId = vendaId,
                                ProdutoId = item.ProdutoId,
                                item.Quantidade,
                                PrecoUnitario = resultado.PrecoUnitarioAplicado,
                                CustoUnitario = resultado.CustoUnitarioAplicado,
                                Subtotal = subtotal
                            },
                            transaction, cancellationToken: ct));
                    }

                    // Só credita o ledger (dinheiro físico no caixa) pra venda de balcão paga
                    // em dinheiro dentro de um turno aberto — cartão/Pix não passam pela
                    // gaveta física, e Zé Delivery não tem turno associado (TurnoId nulo).
                    if (dto.TurnoId is { } turnoId && string.Equals(dto.MetodoPagamento, "DINHEIRO", StringComparison.OrdinalIgnoreCase))
                    {
                        var lancamento = new LancamentoLedgerDto(turnoId, totalVenda, TipoOperacaoLedger.Credito, $"Venda {vendaId}");
                        await LedgerOperacoes.AdicionarAsync(connection, transaction, tenantId, lancamento, ct);
                    }

                    // Fechamento de comanda vira venda: marca a comanda FECHADA na MESMA
                    // transação da venda — se a comanda já não estiver mais ABERTA (fechada ou
                    // cancelada por outra requisição concorrente), a venda inteira reverte junto
                    // em vez de gravar uma venda "órfã" de uma comanda que já não existe mais.
                    if (dto.ComandaId is { } comandaIdFechada)
                    {
                        var linhasComanda = await connection.ExecuteAsync(new CommandDefinition(
                            "UPDATE comandas SET status = 'FECHADA', atualizado_em = now() WHERE id = @ComandaId AND tenant_id = @TenantId AND status = 'ABERTA'",
                            new { ComandaId = comandaIdFechada, TenantId = tenantId }, transaction, cancellationToken: ct));

                        if (linhasComanda == 0)
                            throw new InvalidOperationException($"Comanda {comandaIdFechada} não está aberta ou não existe.");
                    }

                    await transaction.CommitAsync(ct);
                    return new ResultadoVendaDto(vendaId, totalVenda, totalCusto);
                }
                catch (PostgresException ex) when (ex.SqlState == "40001" && tentativa < MaxTentativas)
                {
                    await transaction.RollbackAsync(ct);
                }
            }

            throw new InvalidOperationException($"Falha ao finalizar venda após {MaxTentativas} tentativas (conflitos de concorrência).");
        }

        private sealed record ClienteCreditoRow(decimal LimiteCredito, decimal SaldoDevedor);
    }
}
