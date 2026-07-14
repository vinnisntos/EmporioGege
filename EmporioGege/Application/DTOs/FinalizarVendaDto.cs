using EmporioGege.Core.Enums;

namespace EmporioGege.Application.DTOs
{
    // TurnoId é null para vendas sem turno de caixa físico associado (ex.: Zé Delivery).
    // MetodoPagamento só faz sentido pra vendas de balcão; só "DINHEIRO" com TurnoId
    // preenchido gera crédito no caixa_ledger (cartão/Pix não passam pela gaveta física).
    public record FinalizarVendaDto(
        Guid? TurnoId,
        List<ItemVendaDto> Itens,
        TipoOrigemVenda TipoOrigem,
        bool EmitirNotaFiscal,
        string? MetodoPagamento,
        Guid? ClienteId = null,
        Guid? ComandaId = null,
        string? ReferenciaExterna = null);
}
