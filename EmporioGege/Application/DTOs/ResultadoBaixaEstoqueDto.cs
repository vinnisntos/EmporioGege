namespace EmporioGege.Application.DTOs
{
    // PrecoUnitarioAplicado/CustoUnitarioAplicado já vêm resolvidos por unidade de VENDA
    // (ex.: preço da caixa inteira, não da unidade, quando TipoPreco == Caixa) — calculados
    // dentro da mesma transação que trava a linha do produto, nunca a partir de valor
    // vindo do cliente/carrinho. EstoqueAnterior/EstoqueAtual continuam em unidades de estoque.
    public record ResultadoBaixaEstoqueDto(Guid ProdutoId, int EstoqueAnterior, int EstoqueAtual, decimal PrecoUnitarioAplicado, decimal CustoUnitarioAplicado);
}
