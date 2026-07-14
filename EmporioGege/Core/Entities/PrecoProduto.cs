using EmporioGege.Core.Enums;

namespace EmporioGege.Core.Entities
{
    public class PrecoProduto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid ProdutoId { get; set; }
        public TipoPreco TipoPreco { get; set; }
        public decimal Valor { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
