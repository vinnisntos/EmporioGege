namespace EmporioGege.Application.DTOs
{
    public record ProdutoDetalheDto(
        Guid Id,
        string Nome,
        string? CodigoBarras,
        decimal CustoMedio,
        decimal PrecoVendaBase,
        int EstoqueAtual,
        int EstoqueMinimo,
        string UnidadeMedida,
        int QuantidadePorCaixa,
        DateOnly? DataValidade,
        bool Ativo,
        decimal? PrecoCaixa,
        decimal? PrecoAtacado);
}
