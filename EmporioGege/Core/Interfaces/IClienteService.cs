using EmporioGege.Application.DTOs;

namespace EmporioGege.Core.Interfaces
{
    public interface IClienteService
    {
        Task<IReadOnlyList<ClienteDto>> ListarAsync(CancellationToken ct = default);

        Task<ClienteDto?> ObterAsync(Guid id, CancellationToken ct = default);

        Task<Guid> SalvarAsync(SalvarClienteDto dto, CancellationToken ct = default);

        Task RegistrarPagamentoAsync(Guid id, decimal valor, CancellationToken ct = default);
    }
}
