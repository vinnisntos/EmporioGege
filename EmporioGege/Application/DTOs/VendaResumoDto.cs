namespace EmporioGege.Application.DTOs
{
    public record VendaResumoDto(
        Guid Id,
        DateTime DataVenda,
        string TipoOrigem,
        string? MetodoPagamento,
        string? NumeroComanda,
        string? ClienteNome,
        decimal TotalVenda,
        decimal TotalCusto);
}
