using Postgrest.Attributes;
using Postgrest.Models;

namespace EmporioGege.Models
{
    [Table("vendas")]
    public class Venda : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = default!;

        [Column("tenant_id")]
        public string TenantId { get; set; } = default!;

        [Column("turno_id")]
        public string TurnoId { get; set; } = default!;

        [Column("cliente_id")]
        public string? ClienteId { get; set; }

        [Column("comanda_id")]
        public string? ComandaId { get; set; }

        [Column("tipo_origem")]
        public string TipoOrigem { get; set; } = default!; // 'BALCAO', 'COMANDA', 'ZEDELIVERY'

        [Column("status")]
        public string Status { get; set; } = "FECHADA";

        [Column("emitir_nota_fiscal")]
        public bool EmitirNotaFiscal { get; set; }

        [Column("total_custo")]
        public decimal TotalCusto { get; set; }

        [Column("total_venda")]
        public decimal TotalVenda { get; set; }

        [Column("metodo_pagamento")]
        public string? MetodoPagamento { get; set; }

        [Column("data_venda")]
        public DateTime DataVenda { get; set; }
    }
}