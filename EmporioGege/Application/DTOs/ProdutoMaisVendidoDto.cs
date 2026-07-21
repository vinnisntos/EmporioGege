namespace EmporioGege.Application.DTOs
{
    public record ProdutoMaisVendidoDto(Guid ProdutoId, string Nome, int QuantidadeVendida, decimal TotalVendido);
}
