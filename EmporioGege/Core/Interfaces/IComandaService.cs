using EmporioGege.Application.DTOs;
using EmporioGege.Core.Enums;

namespace EmporioGege.Core.Interfaces
{
    public interface IComandaService
    {
        Task<ComandaResumoDto> AbrirAsync(string numeroComanda, CancellationToken ct = default);

        Task<IReadOnlyList<ComandaResumoDto>> ListarAbertasAsync(CancellationToken ct = default);

        Task<IReadOnlyList<ComandaResumoDto>> ListarTodasAsync(CancellationToken ct = default);

        Task<ComandaDetalheDto?> ObterDetalheAsync(Guid comandaId, CancellationToken ct = default);

        Task<ItemComandaDto> AdicionarItemAsync(Guid comandaId, Guid produtoId, int quantidade, TipoPreco tipoPreco, CancellationToken ct = default);

        Task RemoverItemAsync(Guid comandaId, Guid itemId, CancellationToken ct = default);

        Task<ResultadoVendaDto> FecharAsync(Guid comandaId, Guid? turnoId, string metodoPagamento, bool emitirNotaFiscal, Guid? clienteId, CancellationToken ct = default);

        Task CancelarAsync(Guid comandaId, CancellationToken ct = default);
    }
}
