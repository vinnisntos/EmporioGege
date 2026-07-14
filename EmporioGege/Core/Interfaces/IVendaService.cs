using EmporioGege.Application.DTOs;

namespace EmporioGege.Core.Interfaces
{
    public interface IVendaService
    {
        // Atômico: baixa de estoque de todos os itens + registro da venda/itens + crédito
        // no ledger do caixa (quando aplicável) numa única transação. Lança
        // EstoqueInsuficienteException (sem gravar nada) se qualquer item não tiver saldo.
        Task<ResultadoVendaDto> FinalizarVendaAsync(FinalizarVendaDto dto, CancellationToken ct = default);
    }
}
