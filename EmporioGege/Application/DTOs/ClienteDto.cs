namespace EmporioGege.Application.DTOs
{
    public record ClienteDto(Guid Id, string Nome, string? Telefone, string? CpfRg, decimal LimiteCredito, decimal SaldoDevedor);
}
