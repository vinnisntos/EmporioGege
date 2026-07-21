using System.Data.Common;
using Dapper;

namespace EmporioGege.Application.Services
{
    // Registro de movimentação de estoque isolado de qualquer gerenciamento de transação -
    // mesma ideia do EstoqueOperacoes/LedgerOperacoes: opera dentro de uma conexão/transação
    // que o chamador já abriu, pra poder logar a movimentação na MESMA transação da mudança
    // real do estoque (entrada, ajuste manual, ou a baixa de venda já feita em
    // EstoqueOperacoes.DecrementarAsync).
    internal static class EstoqueMovimentacaoOperacoes
    {
        public static async Task RegistrarAsync(
            DbConnection connection, DbTransaction transaction, Guid tenantId, Guid produtoId,
            string tipoMovimentacao, int quantidade, string? justificativa, Guid? usuarioId, CancellationToken ct)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO estoque_movimentacoes (id, tenant_id, produto_id, tipo_movimentacao, quantidade, usuario_id, justificativa, data_movimento)
                VALUES (gen_random_uuid(), @TenantId, @ProdutoId, @TipoMovimentacao, @Quantidade, @UsuarioId, @Justificativa, now())
                """,
                new { TenantId = tenantId, ProdutoId = produtoId, TipoMovimentacao = tipoMovimentacao, Quantidade = quantidade, UsuarioId = usuarioId, Justificativa = justificativa },
                transaction, cancellationToken: ct));
        }
    }
}
