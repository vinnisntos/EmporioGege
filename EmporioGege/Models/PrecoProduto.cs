using Postgrest.Attributes;
using Postgrest.Models;

namespace EmporioGege.Models
{
    [Table("precos_produto")]
    public class PrecoProduto : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = default!;

        [Column("tenant_id")]
        public string TenantId { get; set; } = default!;

        [Column("produto_id")]
        public string ProdutoId { get; set; } = default!;

        [Column("tipo_preco")]
        public string TipoPreco { get; set; } = default!;

        [Column("valor")]
        public decimal Valor { get; set; }
    }
}
