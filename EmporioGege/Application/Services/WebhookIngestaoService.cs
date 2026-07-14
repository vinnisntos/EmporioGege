using System.Text;
using System.Text.Json;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Enums;
using EmporioGege.Core.Interfaces;
using EmporioGege.Core.Security;
using Microsoft.Extensions.Logging;

namespace EmporioGege.Application.Services
{
    public class WebhookIngestaoService(IWebhookRepository webhookRepository, IWebhookQueue webhookQueue, ILogger<WebhookIngestaoService> logger) : IWebhookIngestaoService
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public async Task<ResultadoIngestaoWebhook> ReceberAsync(CanalIntegracao canal, string tokenUrl, byte[] corpoBruto, string? assinaturaRecebida, CancellationToken ct = default)
        {
            var integracao = await webhookRepository.BuscarIntegracaoAtivaPorTokenAsync(tokenUrl, ct);
            if (integracao is null || integracao.Canal != canal)
            {
                // Não loga o token em si (é um segredo de URL) — só que uma tentativa com
                // token desconhecido/inativo chegou, útil para detectar scanners/abuso.
                logger.LogWarning("Webhook {Canal} recebido com token inválido ou inativo.", canal);
                return ResultadoIngestaoWebhook.TokenInvalido;
            }

            if (!HmacSignatureValidator.IsValid(corpoBruto, assinaturaRecebida, integracao.SegredoHmac))
            {
                logger.LogWarning("Webhook {Canal} do tenant {TenantId} rejeitado: assinatura inválida.", canal, integracao.TenantId);
                return ResultadoIngestaoWebhook.AssinaturaInvalida;
            }

            ZeDeliveryWebhookPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<ZeDeliveryWebhookPayload>(Encoding.UTF8.GetString(corpoBruto), JsonOptions);
            }
            catch (JsonException)
            {
                payload = null;
            }

            if (payload is null || string.IsNullOrWhiteSpace(payload.EventId))
            {
                logger.LogWarning("Webhook {Canal} do tenant {TenantId} rejeitado: payload inválido.", canal, integracao.TenantId);
                return ResultadoIngestaoWebhook.PayloadInvalido;
            }

            var idRegistrado = await webhookRepository.RegistrarRecebimentoAsync(
                integracao.TenantId, canal, payload.EventId, Encoding.UTF8.GetString(corpoBruto), ct);

            if (idRegistrado is null)
            {
                // Evento já recebido antes (reentrega) — responde OK sem reprocessar.
                return ResultadoIngestaoWebhook.Duplicado;
            }

            webhookQueue.Sinalizar(idRegistrado.Value);
            return ResultadoIngestaoWebhook.Aceito;
        }
    }
}
