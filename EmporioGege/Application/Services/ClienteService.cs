using Dapper;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Interfaces;

namespace EmporioGege.Application.Services
{
    public class ClienteService(IDbConnectionFactory connectionFactory, ITenantProvider tenantProvider) : IClienteService
    {
        private const string Selecao = """
            SELECT id AS Id, nome AS Nome, telefone AS Telefone, limite_credito AS LimiteCredito, saldo_devedor AS SaldoDevedor
            FROM clientes
            """;

        public async Task<IReadOnlyList<ClienteDto>> ListarAsync(CancellationToken ct = default)
        {
            var tenantId = tenantProvider.RequireTenantId();

            await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
            var linhas = await connection.QueryAsync<ClienteDto>(new CommandDefinition(
                $"{Selecao} WHERE tenant_id = @TenantId ORDER BY nome",
                new { TenantId = tenantId }, cancellationToken: ct));

            return linhas.AsList();
        }

        public async Task<ClienteDto?> ObterAsync(Guid id, CancellationToken ct = default)
        {
            var tenantId = tenantProvider.RequireTenantId();

            await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
            return await connection.QuerySingleOrDefaultAsync<ClienteDto>(new CommandDefinition(
                $"{Selecao} WHERE tenant_id = @TenantId AND id = @Id",
                new { TenantId = tenantId, Id = id }, cancellationToken: ct));
        }

        public async Task<Guid> SalvarAsync(SalvarClienteDto dto, CancellationToken ct = default)
        {
            var tenantId = tenantProvider.RequireTenantId();
            var clienteId = dto.Id ?? Guid.NewGuid();

            await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);

            if (dto.Id is null)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    """
                    INSERT INTO clientes (id, tenant_id, nome, telefone, limite_credito, saldo_devedor, created_at)
                    VALUES (@Id, @TenantId, @Nome, @Telefone, @LimiteCredito, 0, now())
                    """,
                    new { Id = clienteId, TenantId = tenantId, dto.Nome, dto.Telefone, dto.LimiteCredito }, cancellationToken: ct));
            }
            else
            {
                var linhasAfetadas = await connection.ExecuteAsync(new CommandDefinition(
                    "UPDATE clientes SET nome = @Nome, telefone = @Telefone, limite_credito = @LimiteCredito WHERE id = @Id AND tenant_id = @TenantId",
                    new { Id = clienteId, TenantId = tenantId, dto.Nome, dto.Telefone, dto.LimiteCredito }, cancellationToken: ct));

                if (linhasAfetadas == 0)
                    throw new InvalidOperationException($"Cliente {clienteId} não encontrado para o tenant {tenantId}.");
            }

            return clienteId;
        }

        public async Task RegistrarPagamentoAsync(Guid id, decimal valor, CancellationToken ct = default)
        {
            var tenantId = tenantProvider.RequireTenantId();

            await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
            var linhasAfetadas = await connection.ExecuteAsync(new CommandDefinition(
                "UPDATE clientes SET saldo_devedor = GREATEST(saldo_devedor - @Valor, 0) WHERE id = @Id AND tenant_id = @TenantId",
                new { Valor = valor, Id = id, TenantId = tenantId }, cancellationToken: ct));

            if (linhasAfetadas == 0)
                throw new InvalidOperationException($"Cliente {id} não encontrado para o tenant {tenantId}.");
        }
    }
}
