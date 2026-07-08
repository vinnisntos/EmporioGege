using Postgrest.Attributes;
using Postgrest.Models;

namespace EmporioGege.Models
{
    [Table("caixa_movimentacoes")]
    public class CaixaMovimentacao : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = default!;

        [Column("tenant_id")]
        public string TenantId { get; set; } = default!;

        [Column("turno_id")]
        public string TurnoId { get; set; } = default!;

        [Column("tipo")]
        public string Tipo { get; set; } = default!; // 'SANGRIA' ou 'SUPRIMENTO'

        [Column("valor")]
        public decimal Valor { get; set; }

        [Column("justificativa")]
        public string Justificativa { get; set; } = default!;

        [Column("data_movimento")]
        public DateTime DataMovimento { get; set; }
    }
}