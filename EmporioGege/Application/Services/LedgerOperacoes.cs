using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Entities;

namespace EmporioGege.Application.Services
{
    // Análogo ao EstoqueOperacoes: lógica de lançamento no ledger isolada de
    // gerenciamento de transação, para o VendaService poder creditar a venda em
    // dinheiro na MESMA transação que baixa o estoque e grava a venda — tudo
    // atômico, sem depender de uma segunda transação separada que poderia
    // confirmar o estoque e falhar ao creditar o caixa (ou vice-versa).
    internal static class LedgerOperacoes
    {
        public static async Task<CaixaLedgerEntry> AdicionarAsync(
            DbConnection connection, DbTransaction transaction, Guid tenantId, LancamentoLedgerDto dto, CancellationToken ct)
        {
            var hashAnterior = await connection.QuerySingleOrDefaultAsync<string?>(
                new CommandDefinition(
                    """
                    SELECT hash_verificacao FROM caixa_ledger
                    WHERE tenant_id = @TenantId AND caixa_id = @CaixaId
                    ORDER BY criado_em DESC
                    LIMIT 1
                    """,
                    new { TenantId = tenantId, dto.CaixaId },
                    transaction, cancellationToken: ct));

            var entry = new CaixaLedgerEntry
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CaixaId = dto.CaixaId,
                Valor = dto.Valor,
                TipoOperacao = dto.TipoOperacao,
                Motivo = dto.Motivo,
                CriadoEm = DateTime.UtcNow,
                HashAnterior = hashAnterior
            };
            entry.HashVerificacao = CalcularHash(entry);

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO caixa_ledger (id, tenant_id, caixa_id, valor, tipo_operacao, motivo, criado_em, hash_anterior, hash_verificacao)
                    VALUES (@Id, @TenantId, @CaixaId, @Valor, @TipoOperacao, @Motivo, @CriadoEm, @HashAnterior, @HashVerificacao)
                    """,
                    new
                    {
                        entry.Id,
                        entry.TenantId,
                        entry.CaixaId,
                        entry.Valor,
                        TipoOperacao = entry.TipoOperacao.ToString().ToUpperInvariant(),
                        entry.Motivo,
                        entry.CriadoEm,
                        entry.HashAnterior,
                        entry.HashVerificacao
                    },
                    transaction, cancellationToken: ct));

            return entry;
        }

        private static string CalcularHash(CaixaLedgerEntry entry)
        {
            var payload = string.Join('|',
                entry.HashAnterior ?? string.Empty,
                entry.TenantId,
                entry.CaixaId,
                entry.Valor.ToString("F2"),
                entry.TipoOperacao,
                entry.CriadoEm.ToString("O"));

            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexStringLower(bytes);
        }
    }
}
