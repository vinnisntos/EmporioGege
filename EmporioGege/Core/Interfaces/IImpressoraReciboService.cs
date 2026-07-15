using EmporioGege.Application.DTOs;

namespace EmporioGege.Core.Interfaces
{
    // Nunca lança: impressão é sempre "melhor esforço" — a venda já está gravada no banco
    // antes de qualquer tentativa de imprimir, então uma impressora desligada/sem papel/porta
    // errada não pode derrubar a resposta da venda. O chamador decide o que fazer com `false`
    // (hoje: só avisa na tela, sem bloquear nada).
    public interface IImpressoraReciboService
    {
        Task<bool> ImprimirAsync(ReciboVendaDto recibo, CancellationToken ct = default);
    }
}
