namespace EmporioGege.Application.DTOs
{
    public record MovimentacaoEstoqueDto(
        Guid Id, DateTime DataMovimento, string ProdutoNome, string TipoMovimentacao,
        int Quantidade, string? Justificativa, string? UsuarioNome);
}
