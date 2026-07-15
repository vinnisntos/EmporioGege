using System.IO.Ports;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmporioGege.Infrastructure.Impressao
{
    public class SerialPortImpressoraReciboService(
        IOptions<ImpressoraOptions> opcoes, ILogger<SerialPortImpressoraReciboService> logger) : IImpressoraReciboService
    {
        public Task<bool> ImprimirAsync(ReciboVendaDto recibo, CancellationToken ct = default)
        {
            var config = opcoes.Value;
            if (!config.Habilitada)
                return Task.FromResult(false);

            try
            {
                var bytes = EscPosReciboFormatter.Montar(recibo, config.ColunasPorLinha);

                using var porta = new SerialPort(config.Porta, config.BaudRate, Parity.None, 8, StopBits.One)
                {
                    WriteTimeout = 3000
                };
                porta.Open();
                porta.Write(bytes, 0, bytes.Length);
                porta.Close();

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                // Impressora desligada, porta inexistente/ocupada, sem papel etc. - a venda
                // já foi gravada no banco antes de chegar aqui, então isso nunca pode
                // propagar e derrubar a resposta pro caixa; só loga e avisa via retorno false.
                logger.LogWarning(ex, "Falha ao imprimir recibo da venda {VendaId} na porta {Porta}.", recibo.VendaId, config.Porta);
                return Task.FromResult(false);
            }
        }
    }
}
