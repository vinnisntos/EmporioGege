namespace EmporioGege.Application.DTOs
{
    public record ComandaDetalheDto(
        Guid Id,
        string NumeroComanda,
        string Status,
        DateTime CreatedAt,
        IReadOnlyList<ItemComandaDto> Itens,
        decimal Total);
}
