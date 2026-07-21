using System.Data.Common;
using Dapper;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Interfaces;

namespace EmporioGege.Application.Services
{
    public class ProdutoService(IDbConnectionFactory connectionFactory, ITenantProvider tenantProvider) : IProdutoService
    {
        private const string Selecao = """
            SELECT
                p.id AS Id, p.nome AS Nome, p.codigo_barras AS CodigoBarras, p.custo_medio AS CustoMedio,
                p.preco_venda_base AS PrecoVendaBase, p.estoque_atual AS EstoqueAtual, p.estoque_minimo AS EstoqueMinimo,
                p.unidade_medida AS UnidadeMedida, p.quantidade_por_caixa AS QuantidadePorCaixa,
                p.data_validade AS DataValidade, p.ativo AS Ativo,
                (SELECT valor FROM precos_produto WHERE tenant_id = p.tenant_id AND produto_id = p.id AND tipo_preco = 'CAIXA') AS PrecoCaixa,
                (SELECT valor FROM precos_produto WHERE tenant_id = p.tenant_id AND produto_id = p.id AND tipo_preco = 'ATACADO') AS PrecoAtacado,
                p.codigo_ncm AS CodigoNcm, p.cfop AS Cfop
            FROM produtos p
            """;

        public async Task<IReadOnlyList<ProdutoDetalheDto>> ListarAsync(CancellationToken ct = default)
        {
            var tenantId = tenantProvider.RequireTenantId();

            await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
            var linhas = await connection.QueryAsync<ProdutoDetalheDto>(new CommandDefinition(
                $"{Selecao} WHERE p.tenant_id = @TenantId ORDER BY p.nome",
                new { TenantId = tenantId }, cancellationToken: ct));

            return linhas.AsList();
        }

        public async Task<ProdutoDetalheDto?> ObterAsync(Guid id, CancellationToken ct = default)
        {
            var tenantId = tenantProvider.RequireTenantId();

            await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
            return await connection.QuerySingleOrDefaultAsync<ProdutoDetalheDto>(new CommandDefinition(
                $"{Selecao} WHERE p.tenant_id = @TenantId AND p.id = @Id",
                new { TenantId = tenantId, Id = id }, cancellationToken: ct));
        }

        public async Task<Guid> SalvarAsync(SalvarProdutoDto dto, CancellationToken ct = default)
        {
            var tenantId = tenantProvider.RequireTenantId();
            var produtoId = dto.Id ?? Guid.NewGuid();

            await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
            await using var transaction = await connection.BeginTransactionAsync(ct);

            if (dto.Id is null)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    """
                    INSERT INTO produtos (id, tenant_id, nome, codigo_barras, custo_medio, preco_venda_base, estoque_atual, estoque_minimo, unidade_medida, quantidade_por_caixa, data_validade, ativo, codigo_ncm, cfop, created_at)
                    VALUES (@Id, @TenantId, @Nome, @CodigoBarras, @CustoMedio, @PrecoVendaBase, @EstoqueAtual, @EstoqueMinimo, @UnidadeMedida, @QuantidadePorCaixa, @DataValidade, true, @CodigoNcm, @Cfop, now())
                    """,
                    new
                    {
                        Id = produtoId, TenantId = tenantId, dto.Nome, dto.CodigoBarras, dto.CustoMedio, dto.PrecoVendaBase,
                        dto.EstoqueAtual, dto.EstoqueMinimo, dto.UnidadeMedida, dto.QuantidadePorCaixa, dto.DataValidade,
                        dto.CodigoNcm, dto.Cfop
                    },
                    transaction, cancellationToken: ct));
            }
            else
            {
                var linhasAfetadas = await connection.ExecuteAsync(new CommandDefinition(
                    """
                    UPDATE produtos SET
                        nome = @Nome, codigo_barras = @CodigoBarras, custo_medio = @CustoMedio, preco_venda_base = @PrecoVendaBase,
                        estoque_atual = @EstoqueAtual, estoque_minimo = @EstoqueMinimo, unidade_medida = @UnidadeMedida,
                        quantidade_por_caixa = @QuantidadePorCaixa, data_validade = @DataValidade,
                        codigo_ncm = @CodigoNcm, cfop = @Cfop
                    WHERE id = @Id AND tenant_id = @TenantId
                    """,
                    new
                    {
                        Id = produtoId, TenantId = tenantId, dto.Nome, dto.CodigoBarras, dto.CustoMedio, dto.PrecoVendaBase,
                        dto.EstoqueAtual, dto.EstoqueMinimo, dto.UnidadeMedida, dto.QuantidadePorCaixa, dto.DataValidade,
                        dto.CodigoNcm, dto.Cfop
                    },
                    transaction, cancellationToken: ct));

                if (linhasAfetadas == 0)
                    throw new InvalidOperationException($"Produto {produtoId} não encontrado para o tenant {tenantId}.");
            }

            await SalvarPrecoDiferenciadoAsync(connection, transaction, tenantId, produtoId, "CAIXA", dto.PrecoCaixa, ct);
            await SalvarPrecoDiferenciadoAsync(connection, transaction, tenantId, produtoId, "ATACADO", dto.PrecoAtacado, ct);

            await transaction.CommitAsync(ct);
            return produtoId;
        }

        public async Task DefinirAtivoAsync(Guid id, bool ativo, CancellationToken ct = default)
        {
            var tenantId = tenantProvider.RequireTenantId();

            await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
            await connection.ExecuteAsync(new CommandDefinition(
                "UPDATE produtos SET ativo = @Ativo WHERE id = @Id AND tenant_id = @TenantId",
                new { Ativo = ativo, Id = id, TenantId = tenantId }, cancellationToken: ct));
        }

        // Sem valor configurado (@Valor null) = remove o preço diferenciado, voltando pro
        // fallback (preco_venda_base × fator de conversão) já calculado em EstoqueOperacoes.
        private static async Task SalvarPrecoDiferenciadoAsync(
            DbConnection connection, DbTransaction transaction, Guid tenantId, Guid produtoId, string tipoPreco, decimal? valor, CancellationToken ct)
        {
            if (valor is null)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    "DELETE FROM precos_produto WHERE tenant_id = @TenantId AND produto_id = @ProdutoId AND tipo_preco = @TipoPreco",
                    new { TenantId = tenantId, ProdutoId = produtoId, TipoPreco = tipoPreco }, transaction, cancellationToken: ct));
                return;
            }

            await connection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO precos_produto (id, tenant_id, produto_id, tipo_preco, valor, created_at)
                VALUES (gen_random_uuid(), @TenantId, @ProdutoId, @TipoPreco, @Valor, now())
                ON CONFLICT (tenant_id, produto_id, tipo_preco) DO UPDATE SET valor = EXCLUDED.valor
                """,
                new { TenantId = tenantId, ProdutoId = produtoId, TipoPreco = tipoPreco, Valor = valor }, transaction, cancellationToken: ct));
        }
    }
}
