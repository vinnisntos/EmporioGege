namespace EmporioGege.Application.DTOs
{
    public record ItemVendaDetalheDto(string ProdutoNome, int Quantidade, decimal PrecoUnitarioAplicado, decimal Subtotal);

    public record VendaDetalheDto(
        Guid Id,
        DateTime DataVenda,
        string TipoOrigem,
        string? MetodoPagamento,
        string? NumeroComanda,
        string? ClienteNome,
        decimal TotalVenda,
        decimal TotalCusto,
        IReadOnlyList<ItemVendaDetalheDto> Itens);
}
