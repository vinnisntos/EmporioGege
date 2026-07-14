using EmporioGege.Core.Enums;

namespace EmporioGege.Application.DTOs
{
    // Quantidade é em "unidades de venda" — 1 caixa quando TipoPreco == Caixa, 1 unidade
    // nos demais casos. O VendaService resolve o fator de conversão e o preço aplicável
    // (de precos_produto, nunca confiando em valor vindo do cliente).
    public record ItemVendaDto(Guid ProdutoId, int Quantidade, TipoPreco TipoPreco = TipoPreco.Balcao);
}
