using EmporioGege.Application.DTOs;
using EmporioGege.Core.Entities;

namespace EmporioGege.Core.Interfaces
{
    public interface ICaixaLedgerService
    {
        Task<CaixaLedgerEntry> AdicionarLancamentoAsync(LancamentoLedgerDto dto, CancellationToken ct = default);
    }
}
