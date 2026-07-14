using EmporioGege.Application.DTOs;

namespace EmporioGege.Core.Interfaces
{
    public interface IEstoqueService
    {
        Task<ResultadoBaixaEstoqueDto> ProcessarBaixaEstoqueAsync(BaixaEstoqueDto dto, CancellationToken ct = default);
    }
}
