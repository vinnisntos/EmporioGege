namespace EmporioGege.Infrastructure.Faturamento
{
    public class AsaasOptions
    {
        // Token de conta - vai no header "access_token" em toda chamada (criar cliente,
        // criar assinatura). Um único token serve pra tudo, diferente do Focus NFe.
        public string Token { get; set; } = default!;

        // "sandbox" (ambiente de testes, sem cobrança real) ou "producao".
        public string Ambiente { get; set; } = "sandbox";

        // authToken configurado no cadastro do webhook no painel/API do Asaas - a Asaas ecoa
        // esse mesmo valor no header "asaas-access-token" em toda chamada de webhook, e é
        // isso que validamos pra confirmar que a chamada realmente veio da Asaas (não é HMAC,
        // é comparação direta de token fixo - ver Infrastructure/Faturamento/AsaasWebhookService).
        public string WebhookToken { get; set; } = default!;
    }
}
