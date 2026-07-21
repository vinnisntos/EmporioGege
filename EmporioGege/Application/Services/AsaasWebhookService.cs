using System.Text.Json;
using Dapper;
using EmporioGege.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace EmporioGege.Application.Services
{
    // Mapeia eventos de webhook do Asaas pra status_licenca do tenant. Diferente do webhook
    // Zé Delivery (que CRIA vendas e por isso precisa de fila durável + idempotência via
    // event id), aqui só SETAMOS um status - aplicar o mesmo evento duas vezes é inofensivo
    // por natureza, então não há fila própria: se o processamento falhar, devolvemos 500 e
    // deixamos a própria Asaas reentregar o evento (comportamento padrão deles).
    public class AsaasWebhookService(IDbConnectionFactory connectionFactory, ILogger<AsaasWebhookService> logger) : IAsaasWebhookService
    {
        public async Task ProcessarAsync(string corpoJson, CancellationToken ct = default)
        {
            using var documento = JsonDocument.Parse(corpoJson);
            var raiz = documento.RootElement;
            var evento = raiz.TryGetProperty("event", out var eventoEl) ? eventoEl.GetString() : null;

            if (evento is null)
            {
                logger.LogWarning("Webhook Asaas recebido sem campo 'event'.");
                return;
            }

            string? subscriptionId = null;
            DateOnly? vencimentoPagamento = null;

            if (raiz.TryGetProperty("payment", out var paymentEl))
            {
                if (paymentEl.TryGetProperty("subscription", out var subEl) && subEl.ValueKind == JsonValueKind.String)
                    subscriptionId = subEl.GetString();

                if (paymentEl.TryGetProperty("dueDate", out var dueDateEl) && dueDateEl.ValueKind == JsonValueKind.String
                    && DateOnly.TryParse(dueDateEl.GetString(), out var dueDate))
                    vencimentoPagamento = dueDate;
            }
            else if (raiz.TryGetProperty("subscription", out var subObjEl) && subObjEl.TryGetProperty("id", out var subIdEl))
            {
                subscriptionId = subIdEl.GetString();
            }

            if (subscriptionId is null)
            {
                logger.LogWarning("Webhook Asaas ({Evento}) recebido sem subscription associada - ignorado.", evento);
                return;
            }

            var novoStatus = evento switch
            {
                "PAYMENT_CONFIRMED" or "PAYMENT_RECEIVED" => "ativo",
                "PAYMENT_OVERDUE" => "suspenso",
                "SUBSCRIPTION_DELETED" or "SUBSCRIPTION_INACTIVATED" => "cancelado",
                _ => null
            };

            if (novoStatus is null)
                return; // Evento que não mapeamos pra status_licenca (ex.: PAYMENT_CREATED) - ignorado de propósito.

            await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);

            // Grace period de 5 dias além do vencimento da cobrança confirmada - tolera
            // pequenas diferenças de fuso/relógio entre o Asaas e a nossa checagem; quem
            // realmente controla o acesso é status_licenca (checado no login), isso aqui é
            // só o campo informativo (data_expiracao) usado como segunda camada.
            DateTime? novaDataExpiracao = novoStatus == "ativo" && vencimentoPagamento is not null
                ? vencimentoPagamento.Value.AddDays(5).ToDateTime(TimeOnly.MinValue)
                : null;

            var sql = novaDataExpiracao is not null
                ? "UPDATE tenants SET status_licenca = @Status, data_expiracao = @DataExpiracao WHERE asaas_subscription_id = @SubscriptionId"
                : "UPDATE tenants SET status_licenca = @Status WHERE asaas_subscription_id = @SubscriptionId";

            var linhasAfetadas = await connection.ExecuteAsync(new CommandDefinition(
                sql, new { Status = novoStatus, DataExpiracao = novaDataExpiracao, SubscriptionId = subscriptionId }, cancellationToken: ct));

            if (linhasAfetadas == 0)
                logger.LogWarning("Webhook Asaas ({Evento}) referenciou subscription {SubscriptionId} sem tenant correspondente.", evento, subscriptionId);
        }
    }
}
