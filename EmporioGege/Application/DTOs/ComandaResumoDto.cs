namespace EmporioGege.Application.DTOs
{
    public record ComandaResumoDto(
        Guid Id,
        string NumeroComanda,
        string Status,
        DateTime CreatedAt,
        DateTime AtualizadoEm,
        decimal Total,
        int QuantidadeItens);
}
