namespace EmporioGege.Application.DTOs
{
    public record ProdutoValidadeDto(Guid Id, string Nome, DateOnly DataValidade, int DiasRestantes, int EstoqueAtual);
}
