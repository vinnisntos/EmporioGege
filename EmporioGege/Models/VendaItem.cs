using Postgrest.Attributes;
using Postgrest.Models;

namespace EmporioGege.Models
{
    [Table("vendas_itens")]
    public class VendaItem : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = default!;

        [Column("tenant_id")]
        public string TenantId { get; set; } = default!;

        [Column("venda_id")]
        public string VendaId { get; set; } = default!;

        [Column("variacao_id")]
        public string VariacaoId { get; set; } = default!;

        [Column("quantidade")]
        public int Quantidade { get; set; }

        [Column("preco_unitario_aplicado")]
        public decimal PrecoUnitarioAplicado { get; set; }

        [Column("custo_unitario_aplicado")]
        public decimal CustoUnitarioAplicado { get; set; }

        [Column("subtotal")]
        public decimal Subtotal { get; set; }
    }
}