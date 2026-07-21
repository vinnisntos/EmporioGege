using EmporioGege.Application.DTOs;

namespace EmporioGege.Core.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardResumoDto> ObterResumoAsync(DateTime inicio, DateTime fimExclusivo, CancellationToken ct = default);

        Task<int> ContarProdutosEstoqueCriticoAsync(CancellationToken ct = default);

        Task<int> ContarComandasAtivasAsync(CancellationToken ct = default);

        Task<decimal> ObterFiadoPendenteTotalAsync(CancellationToken ct = default);

        Task<IReadOnlyList<ProdutoValidadeDto>> ListarProdutosProximosValidadeAsync(int diasLimite = 30, CancellationToken ct = default);

        Task<IReadOnlyList<ProdutoMaisVendidoDto>> ListarProdutosMaisVendidosAsync(DateTime inicio, DateTime fimExclusivo, int top, CancellationToken ct = default);
    }
}
