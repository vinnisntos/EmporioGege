namespace EmporioGege.Core.Exceptions
{
    public class EstoqueInsuficienteException(Guid produtoId, int quantidadeSolicitada, int quantidadeDisponivel)
        : Exception($"Estoque insuficiente para o produto {produtoId}: solicitado {quantidadeSolicitada}, disponível {quantidadeDisponivel}.")
    {
        public Guid ProdutoId { get; } = produtoId;
        public int QuantidadeSolicitada { get; } = quantidadeSolicitada;
        public int QuantidadeDisponivel { get; } = quantidadeDisponivel;
    }
}
