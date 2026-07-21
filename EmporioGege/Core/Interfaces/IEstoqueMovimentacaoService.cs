using EmporioGege.Application.DTOs;

namespace EmporioGege.Core.Interfaces
{
    public interface IEstoqueMovimentacaoService
    {
        // Entrada de mercadoria (compra/reposição) - incrementa estoque_atual E deixa rastro
        // em estoque_movimentacoes, diferente de editar o número direto no cadastro do
        // produto (que só fica registrado como AJUSTE_MANUAL, sem motivo/fornecedor).
        Task RegistrarEntradaAsync(Guid produtoId, int quantidade, string? justificativa, Guid usuarioId, CancellationToken ct = default);

        Task<IReadOnlyList<MovimentacaoEstoqueDto>> ListarHistoricoAsync(
            DateTime inicio, DateTime fimExclusivo, Guid? produtoId, CancellationToken ct = default);
    }
}
