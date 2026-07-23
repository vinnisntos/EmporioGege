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

        // Converte um tenant em trial (status_licenca = 'trial') pra cobrança de verdade:
        // lê plano/valor/forma de pagamento já escolhidos no cadastro, cria a assinatura no
        // Asaas (reaproveitando CriarAssinaturaAsync) e move o status pra 'pendente' (bloqueia
        // login até o webhook confirmar o primeiro pagamento). Chamado tanto pelo próprio
        // lojista ("Assinar agora" em Admin/Index) quanto automaticamente pelo
        // TrialExpiradoProcessor quando o trial vence sem ação.
        Task<CriarAssinaturaAsaasResultado> IniciarCobrancaTrialAsync(Guid tenantId, CancellationToken ct = default);
    }
}
