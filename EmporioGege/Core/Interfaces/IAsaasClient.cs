using EmporioGege.Application.DTOs;

namespace EmporioGege.Core.Interfaces
{
    // Cliente HTTP da API do Asaas (cobrança/assinatura da própria loja pra usar o PendurAi -
    // diferente do PDV, que é cobrança do CLIENTE FINAL da loja). Autenticação via header
    // "access_token" - um único token de conta serve pra toda operação (diferente do Focus
    // NFe, que usa um token por empresa cadastrada).
    public interface IAsaasClient
    {
        Task<CriarClienteAsaasResultado> CriarClienteAsync(CriarClienteAsaasDto dto, CancellationToken ct = default);

        Task<CriarAssinaturaAsaasResultado> CriarAssinaturaAsync(CriarAssinaturaAsaasDto dto, CancellationToken ct = default);
    }
}
