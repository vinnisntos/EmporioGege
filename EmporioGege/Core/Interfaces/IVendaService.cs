using EmporioGege.Application.DTOs;

namespace EmporioGege.Core.Interfaces
{
    public interface IVendaService
    {
        // Atômico: baixa de estoque de todos os itens + registro da venda/itens + crédito
        // no ledger do caixa (quando aplicável) numa única transação. Lança
        // EstoqueInsuficienteException (sem gravar nada) se qualquer item não tiver saldo.
        Task<ResultadoVendaDto> FinalizarVendaAsync(FinalizarVendaDto dto, CancellationToken ct = default);

        // Extrato de vendas: lista vendas já fechadas num intervalo [inicio, fimExclusivo).
        Task<IReadOnlyList<VendaResumoDto>> ListarAsync(DateTime inicio, DateTime fimExclusivo, CancellationToken ct = default);

        // Detalhe de uma venda (com itens) - usado tanto pelo extrato quanto pela reimpressão manual de recibo.
        Task<VendaDetalheDto?> ObterDetalheAsync(Guid id, CancellationToken ct = default);
    }
}
