namespace EmporioGege.Application.DTOs
{
    // Id nulo = criar produto novo; Id preenchido = editar existente.
    public record SalvarProdutoDto(
        Guid? Id,
        string Nome,
        string? CodigoBarras,
        decimal CustoMedio,
        decimal PrecoVendaBase,
        int EstoqueAtual,
        int EstoqueMinimo,
        string UnidadeMedida,
        int QuantidadePorCaixa,
        DateOnly? DataValidade,
        decimal? PrecoCaixa,
        decimal? PrecoAtacado);
}
