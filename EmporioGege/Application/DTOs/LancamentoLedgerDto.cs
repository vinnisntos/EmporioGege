using EmporioGege.Core.Enums;

namespace EmporioGege.Application.DTOs
{
    public record LancamentoLedgerDto(Guid CaixaId, decimal Valor, TipoOperacaoLedger TipoOperacao, string Motivo);
}
