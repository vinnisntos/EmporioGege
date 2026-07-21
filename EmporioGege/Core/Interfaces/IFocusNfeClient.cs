using EmporioGege.Application.DTOs;

namespace EmporioGege.Core.Interfaces
{
    // Cliente HTTP da API do Focus NFe (emissão de NFC-e). Autenticação HTTP Basic: token
    // como usuário, senha vazia - diferente pra cada operação (token DE CONTA, configurado
    // globalmente, pra criar/consultar empresas; token DA EMPRESA, devolvido na criação, pra
    // emitir notas daquela empresa especificamente - ver CriarEmpresaFocusNfeResultado).
    public interface IFocusNfeClient
    {
        Task<CriarEmpresaFocusNfeResultado> CriarEmpresaAsync(CriarEmpresaFocusNfeDto dto, CancellationToken ct = default);
    }
}
