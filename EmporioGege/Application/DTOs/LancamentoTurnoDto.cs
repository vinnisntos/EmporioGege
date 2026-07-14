namespace EmporioGege.Application.DTOs
{
    public record LancamentoTurnoDto(Guid Id, decimal Valor, string TipoOperacao, string Motivo, DateTime CriadoEm);
}
