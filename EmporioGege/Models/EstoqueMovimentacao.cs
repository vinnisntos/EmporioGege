using Postgrest.Attributes;
using Postgrest.Models;

namespace EmporioGege.Models
{
    [Table("estoque_movimentacoes")]
    public class EstoqueMovimentacao : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = default!;

        [Column("tenant_id")]
        public string TenantId { get; set; } = default!;

        [Column("produto_id")]
        public string ProdutoId { get; set; } = default!;

        [Column("tipo_movimentacao")]
        public string TipoMovimentacao { get; set; } = default!; // 'ENTRADA_COMPRA', 'SAIDA_VENDA', etc.

        [Column("quantidade")]
        public int Quantidade { get; set; }

        [Column("usuario_id")]
        public string? UsuarioId { get; set; }

        [Column("justificativa")]
        public string? Justificativa { get; set; }

        [Column("data_movimento")]
        public DateTime DataMovimento { get; set; }
    }
}