using EmporioGege.Core.Enums;

namespace EmporioGege.Core.Entities
{
    public class IntegracaoWebhook
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public CanalIntegracao Canal { get; set; }
        public string TokenUrl { get; set; } = default!;
        public string SegredoHmac { get; set; } = default!;
        public bool Ativo { get; set; }
    }
}
