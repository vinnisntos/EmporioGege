using EmporioGege.Application.DTOs;

namespace EmporioGege.Core.Interfaces
{
    public interface IProdutoService
    {
        Task<IReadOnlyList<ProdutoDetalheDto>> ListarAsync(CancellationToken ct = default);

        Task<ProdutoDetalheDto?> ObterAsync(Guid id, CancellationToken ct = default);

        // usuarioId: quem fez a alteração - registrado como AJUSTE_MANUAL em
        // estoque_movimentacoes quando o Estoque Atual mudar (ver ProdutoService).
        Task<Guid> SalvarAsync(SalvarProdutoDto dto, Guid usuarioId, CancellationToken ct = default);

        Task DefinirAtivoAsync(Guid id, bool ativo, CancellationToken ct = default);
    }
}
