namespace EmporioGege.Application.DTOs
{
    public record ClienteDto(Guid Id, string Nome, string? Telefone, decimal LimiteCredito, decimal SaldoDevedor);
}
