using EmporioGege.Application.DTOs;

namespace EmporioGege.Core.Interfaces
{
    public interface ICatalogoService
    {
        Task<IReadOnlyList<ProdutoCatalogoDto>> ListarProdutosAsync(CancellationToken ct = default);

        Task<IReadOnlyList<PrecoProdutoDto>> ListarPrecosDiferenciadosAsync(CancellationToken ct = default);

        Task<Guid?> BuscarProdutoIdPorCodigoBarrasAsync(string codigoBarras, CancellationToken ct = default);
    }
}
