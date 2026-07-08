using Postgrest.Attributes;
using Postgrest.Models;

namespace EmporioGege.Models
{
    [Table("caixa_turnos")]
    public class CaixaTurno : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = default!;

        [Column("tenant_id")]
        public string TenantId { get; set; } = default!;

        [Column("usuario_id")]
        public string UsuarioId { get; set; } = default!;

        [Column("data_abertura")]
        public DateTime DataAbertura { get; set; }

        [Column("data_fechamento")]
        public DateTime? DataFechamento { get; set; }

        [Column("saldo_inicial")]
        public decimal SaldoInicial { get; set; }

        [Column("saldo_fechamento_sistema")]
        public decimal SaldoFechamentoSistema { get; set; }

        [Column("saldo_fechamento_informado")]
        public decimal SaldoFechamentoInformado { get; set; }

        [Column("status")]
        public string Status { get; set; } = "ABERTO";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}