using Postgrest.Attributes;
using Postgrest.Models;

namespace EmporioGege.Models
{
    [Table("clientes")]
    public class Cliente : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = default!;

        [Column("tenant_id")]
        public string TenantId { get; set; } = default!;

        [Column("nome")]
        public string Nome { get; set; } = default!;

        [Column("telefone")]
        public string? Telefone { get; set; }

        [Column("limite_credito")]
        public decimal LimiteCredito { get; set; }

        [Column("saldo_devedor")]
        public decimal SaldoDevedor { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}