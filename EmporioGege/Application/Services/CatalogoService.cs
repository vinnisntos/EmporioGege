using Dapper;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Interfaces;

namespace EmporioGege.Application.Services
{
    public class CatalogoService(IDbConnectionFactory connectionFactory, ITenantProvider tenantProvider) : ICatalogoService
    {
        public async Task<IReadOnlyList<ProdutoCatalogoDto>> ListarProdutosAsync(CancellationToken ct = default)
        {
            var tenantId = tenantProvider.RequireTenantId();

            await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
            var linhas = await connection.QueryAsync<ProdutoCatalogoDto>(new CommandDefinition(
                """
                SELECT id AS Id, nome AS Nome, codigo_barras AS CodigoBarras, preco_venda_base AS PrecoVendaBase,
                       estoque_atual AS EstoqueAtual, quantidade_por_caixa AS QuantidadePorCaixa
                FROM produtos
                WHERE tenant_id = @TenantId AND ativo = true
                ORDER BY nome
                """,
                new { TenantId = tenantId }, cancellationToken: ct));

            return linhas.AsList();
        }

        public async Task<IReadOnlyList<PrecoProdutoDto>> ListarPrecosDiferenciadosAsync(CancellationToken ct = default)
        {
            var tenantId = tenantProvider.RequireTenantId();

            await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
            var linhas = await connection.QueryAsync<PrecoProdutoDto>(new CommandDefinition(
                """
                SELECT produto_id AS ProdutoId, tipo_preco AS TipoPreco, valor AS Valor
                FROM precos_produto
                WHERE tenant_id = @TenantId
                """,
                new { TenantId = tenantId }, cancellationToken: ct));

            return linhas.AsList();
        }

        public async Task<Guid?> BuscarProdutoIdPorCodigoBarrasAsync(string codigoBarras, CancellationToken ct = default)
        {
            var tenantId = tenantProvider.RequireTenantId();

            await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
            return await connection.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition(
                "SELECT id FROM produtos WHERE tenant_id = @TenantId AND codigo_barras = @CodigoBarras AND ativo = true",
                new { TenantId = tenantId, CodigoBarras = codigoBarras }, cancellationToken: ct));
        }
    }
}
