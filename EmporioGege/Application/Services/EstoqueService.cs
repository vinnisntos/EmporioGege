using System.Data;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Interfaces;
using Npgsql;

namespace EmporioGege.Application.Services
{
    public class EstoqueService(IDbConnectionFactory connectionFactory, ITenantProvider tenantProvider) : IEstoqueService
    {
        private const int MaxTentativas = 3;

        public async Task<ResultadoBaixaEstoqueDto> ProcessarBaixaEstoqueAsync(BaixaEstoqueDto dto, CancellationToken ct = default)
        {
            var tenantId = tenantProvider.RequireTenantId();

            for (var tentativa = 1; tentativa <= MaxTentativas; tentativa++)
            {
                await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
                await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);

                try
                {
                    await connectionFactory.SetTenantContextAsync(connection, transaction, tenantId, ct);

                    var resultado = await EstoqueOperacoes.DecrementarAsync(connection, transaction, tenantId, dto, ct);

                    await transaction.CommitAsync(ct);
                    return resultado;
                }
                catch (PostgresException ex) when (ex.SqlState == "40001" && tentativa < MaxTentativas)
                {
                    await transaction.RollbackAsync(ct);
                }
            }

            throw new InvalidOperationException($"Falha ao processar baixa de estoque do produto {dto.ProdutoId} após {MaxTentativas} tentativas (conflitos de concorrência).");
        }
    }
}
