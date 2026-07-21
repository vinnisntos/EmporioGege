using EmporioGege.Application.DTOs;

namespace EmporioGege.Core.Interfaces
{
    // Assinatura/cobrança da PRÓPRIA loja pra usar o PendurAi (diferente de qualquer cobrança
    // do PDV, que é do cliente final da loja). Escopo desta versão: só automatiza a cobrança
    // de lojas já cadastradas manualmente pelo superadmin - não é cadastro self-service.
    public interface IFaturamentoService
    {
        Task<AssinaturaTenantDto?> ObterAsync(Guid tenantId, CancellationToken ct = default);

        Task<CriarAssinaturaAsaasResultado> CriarAssinaturaAsync(
            Guid tenantId, string plano, decimal valorMensalidade, string tipoCobranca, CancellationToken ct = default);
    }
}
