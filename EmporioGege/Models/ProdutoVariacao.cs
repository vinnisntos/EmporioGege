using Postgrest.Attributes;
using Postgrest.Models;

namespace EmporioGege.Models
{
    [Table("produtos_variacoes")]
    public class ProdutoVariacao : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = default!;

        [Column("tenant_id")]
        public string TenantId { get; set; } = default!;

        [Column("produto_id")]
        public string ProdutoId { get; set; } = default!;

        [Column("nome_variacao")]
        public string NomeVariacao { get; set; } = default!;

        [Column("fator_conversao")]
        public int FatorConversao { get; set; }

        [Column("preco_venda")]
        public decimal PrecoVenda { get; set; }

        [Column("tipo_canal")]
        public string TipoCanal { get; set; } = "CAIXA";

        [Column("codigo_barras_variacao")]
        public string? CodigoBarrasVariacao { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}