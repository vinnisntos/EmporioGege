using EmporioGege.Core.Enums;

namespace EmporioGege.Core.Entities
{
    public class CaixaLedgerEntry
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid CaixaId { get; set; }
        public decimal Valor { get; set; }
        public TipoOperacaoLedger TipoOperacao { get; set; }
        public string Motivo { get; set; } = default!;
        public DateTime CriadoEm { get; set; }
        public string? HashAnterior { get; set; }
        public string HashVerificacao { get; set; } = default!;
    }
}
