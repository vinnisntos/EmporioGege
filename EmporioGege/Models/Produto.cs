using Postgrest.Attributes;
using Postgrest.Models;

namespace EmporioGege.Models
{
    [Table("produtos")]
    public class Produto : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = default!;

        [Column("tenant_id")]
        public string TenantId { get; set; } = default!;

        [Column("nome")]
        public string Nome { get; set; } = default!;

        [Column("codigo_barras")]
        public string? CodigoBarras { get; set; }

        [Column("custo_medio")]
        public decimal CustoMedio { get; set; }

        [Column("preco_venda_base")]
        public decimal PrecoVendaBase { get; set; }

        [Column("estoque_atual")]
        public int EstoqueAtual { get; set; }

        [Column("estoque_minimo")]
        public int EstoqueMinimo { get; set; }

        [Column("data_validade")]
        public DateTime? DataValidade { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}