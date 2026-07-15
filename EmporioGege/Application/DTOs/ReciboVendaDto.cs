namespace EmporioGege.Application.DTOs
{
    public record ItemReciboDto(string ProdutoNome, int Quantidade, decimal PrecoUnitario, decimal Subtotal);

    // NumeroComanda só é preenchido quando a venda veio do fechamento de uma comanda
    // (Pages/Caixa/Comandas) — vendas de balcão direto (Pages/Caixa/Index) não têm.
    public record ReciboVendaDto(
        string NomeLoja,
        Guid VendaId,
        DateTime DataVenda,
        IReadOnlyList<ItemReciboDto> Itens,
        decimal Total,
        string MetodoPagamento,
        string? NumeroComanda = null);
}
