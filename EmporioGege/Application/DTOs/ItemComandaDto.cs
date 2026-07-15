namespace EmporioGege.Application.DTOs
{
    public record ItemComandaDto(
        Guid Id,
        Guid ProdutoId,
        string ProdutoNome,
        int Quantidade,
        string TipoPreco,
        decimal PrecoUnitarioAplicado,
        decimal Subtotal);
}
