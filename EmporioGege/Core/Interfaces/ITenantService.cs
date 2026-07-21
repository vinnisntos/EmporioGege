using EmporioGege.Application.DTOs;

namespace EmporioGege.Core.Interfaces
{
    // Diferente dos outros serviços, não usa ITenantProvider: o superadmin gerencia
    // TODOS os tenants aqui, não um tenant específico da própria sessão.
    public interface ITenantService
    {
        Task<IReadOnlyList<TenantDto>> ListarAsync(CancellationToken ct = default);

        Task<TenantDto?> ObterAsync(Guid id, CancellationToken ct = default);

        Task<Guid> SalvarAsync(SalvarTenantDto dto, CancellationToken ct = default);

        Task AtualizarStatusLicencaAsync(Guid id, string statusLicenca, CancellationToken ct = default);

        Task<int> ContarUsuariosTotalAsync(CancellationToken ct = default);

        Task<decimal> ObterFaturamentoGlobalAsync(CancellationToken ct = default);
    }
}
