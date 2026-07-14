namespace EmporioGege.Application.DTOs
{
    public record TurnoAtualDto(Guid Id, DateTime DataAbertura, decimal SaldoInicial, string Status);
}
