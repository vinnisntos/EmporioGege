using EmporioGege.Core.Enums;

namespace EmporioGege.Core.Entities
{
    public class WebhookRecebido
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public CanalIntegracao Canal { get; set; }
        public string EventoExternoId { get; set; } = default!;
        public string Payload { get; set; } = default!;
        public StatusWebhook Status { get; set; }
    }
}
