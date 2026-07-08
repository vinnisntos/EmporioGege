using Postgrest.Attributes;
using Postgrest.Models;

namespace EmporioGege.Models
{
    [Table("comandas")]
    public class Comanda : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = default!;

        [Column("tenant_id")]
        public string TenantId { get; set; } = default!;

        [Column("numero_comanda")]
        public string NumeroComanda { get; set; } = default!;

        [Column("status")]
        public string Status { get; set; } = "ABERTA";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}