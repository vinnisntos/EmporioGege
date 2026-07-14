using EmporioGege.Core.Enums;

namespace EmporioGege.Application.DTOs
{
    // Quantidade é em "unidades de venda" (ex.: 2 caixas), não em unidades de estoque —
    // EstoqueOperacoes converte usando o fator de conversão do produto quando TipoPreco == Caixa.
    public record BaixaEstoqueDto(Guid ProdutoId, int Quantidade, TipoOrigemVenda TipoOrigem, TipoPreco TipoPreco = TipoPreco.Balcao, string? ReferenciaExterna = null);
}
