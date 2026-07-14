using EmporioGege.Application.DTOs;

namespace EmporioGege.Core.Interfaces
{
    public interface IProdutoService
    {
        Task<IReadOnlyList<ProdutoDetalheDto>> ListarAsync(CancellationToken ct = default);

        Task<ProdutoDetalheDto?> ObterAsync(Guid id, CancellationToken ct = default);

        Task<Guid> SalvarAsync(SalvarProdutoDto dto, CancellationToken ct = default);

        Task DefinirAtivoAsync(Guid id, bool ativo, CancellationToken ct = default);
    }
}
