using Dapper;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Interfaces;

namespace EmporioGege.Application.Services
{
    public class EstoqueMovimentacaoService(IDbConnectionFactory connectionFactory, ITenantProvider tenantProvider) : IEstoqueMovimentacaoService
    {
        public async Task RegistrarEntradaAsync(Guid produtoId, int quantidade, string? justificativa, Guid usuarioId, CancellationToken ct = default)
        {
            if (quantidade <= 0)
                throw new ArgumentException("Quantidade de entrada precisa ser maior que zero.", nameof(quantidade));

            var tenantId = tenantProvider.RequireTenantId();

            await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
            await using var transaction = await connection.BeginTransactionAsync(ct);

            // Incremento puro (col = col + X) é atômico por natureza no Postgres - diferente
            // da baixa de venda (EstoqueOperacoes), não precisa de SELECT ... FOR UPDATE antes,
            // já que não há decisão de negócio (ex.: checar estoque insuficiente) dependendo
            // do valor lido primeiro.
            var linhasAfetadas = await connection.ExecuteAsync(new CommandDefinition(
                "UPDATE produtos SET estoque_atual = estoque_atual + @Quantidade WHERE id = @ProdutoId AND tenant_id = @TenantId",
                new { Quantidade = quantidade, ProdutoId = produtoId, TenantId = tenantId }, transaction, cancellationToken: ct));

            if (linhasAfetadas == 0)
                throw new InvalidOperationException($"Produto {produtoId} não encontrado para o tenant {tenantId}.");

            await EstoqueMovimentacaoOperacoes.RegistrarAsync(
                connection, transaction, tenantId, produtoId, "ENTRADA_COMPRA", quantidade, justificativa, usuarioId, ct);

            await transaction.CommitAsync(ct);
        }

        public async Task<IReadOnlyList<MovimentacaoEstoqueDto>> ListarHistoricoAsync(
            DateTime inicio, DateTime fimExclusivo, Guid? produtoId, CancellationToken ct = default)
        {
            var tenantId = tenantProvider.RequireTenantId();

            await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
            var linhas = await connection.QueryAsync<MovimentacaoEstoqueDto>(new CommandDefinition(
                """
                SELECT em.id AS Id, em.data_movimento AS DataMovimento, p.nome AS ProdutoNome,
                       em.tipo_movimentacao AS TipoMovimentacao, em.quantidade AS Quantidade,
                       em.justificativa AS Justificativa, perfil.nome AS UsuarioNome
                FROM estoque_movimentacoes em
                JOIN produtos p ON p.id = em.produto_id AND p.tenant_id = em.tenant_id
                LEFT JOIN profiles perfil ON perfil.id = em.usuario_id
                WHERE em.tenant_id = @TenantId AND em.data_movimento >= @Inicio AND em.data_movimento < @Fim
                      AND (@ProdutoId IS NULL OR em.produto_id = @ProdutoId)
                ORDER BY em.data_movimento DESC
                """,
                new { TenantId = tenantId, Inicio = inicio, Fim = fimExclusivo, ProdutoId = produtoId }, cancellationToken: ct));

            return linhas.AsList();
        }
    }
}
