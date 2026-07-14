using System.Data;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Entities;
using EmporioGege.Core.Interfaces;
using Npgsql;

namespace EmporioGege.Application.Services
{
    public class CaixaLedgerService(IDbConnectionFactory connectionFactory, ITenantProvider tenantProvider) : ICaixaLedgerService
    {
        private const int MaxTentativas = 3;

        public async Task<CaixaLedgerEntry> AdicionarLancamentoAsync(LancamentoLedgerDto dto, CancellationToken ct = default)
        {
            var tenantId = tenantProvider.RequireTenantId();

            for (var tentativa = 1; tentativa <= MaxTentativas; tentativa++)
            {
                await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
                await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);

                try
                {
                    await connectionFactory.SetTenantContextAsync(connection, transaction, tenantId, ct);

                    var entry = await LedgerOperacoes.AdicionarAsync(connection, transaction, tenantId, dto, ct);

                    await transaction.CommitAsync(ct);
                    return entry;
                }
                catch (PostgresException ex) when (ex.SqlState == "40001" && tentativa < MaxTentativas)
                {
                    await transaction.RollbackAsync(ct);
                }
            }

            throw new InvalidOperationException($"Falha ao registrar lançamento no ledger do caixa {dto.CaixaId} após {MaxTentativas} tentativas (conflitos de concorrência).");
        }
    }
}
