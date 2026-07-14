using System.Globalization;

namespace EmporioGege.Core.Exceptions
{
    public class LimiteCreditoExcedidoException(Guid clienteId, decimal limiteCredito, decimal saldoAtual, decimal valorVenda)
        : Exception($"Limite de crédito excedido para o cliente {clienteId}: limite {limiteCredito.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))}, " +
                     $"saldo atual {saldoAtual.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))}, venda {valorVenda.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))}.")
    {
        public Guid ClienteId { get; } = clienteId;
        public decimal LimiteCredito { get; } = limiteCredito;
        public decimal SaldoAtual { get; } = saldoAtual;
        public decimal ValorVenda { get; } = valorVenda;
    }
}
