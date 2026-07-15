namespace EmporioGege.Application.DTOs
{
    public record ItemVendaResultadoDto(Guid ProdutoId, int Quantidade, decimal PrecoUnitarioAplicado, decimal Subtotal);

    // Itens carrega o preço REALMENTE aplicado dentro da transação (não uma prévia) - dá
    // pra montar um recibo/comprovante exato sem reconsultar o catálogo depois do fato.
    public record ResultadoVendaDto(Guid VendaId, decimal TotalVenda, decimal TotalCusto, IReadOnlyList<ItemVendaResultadoDto> Itens);
}
