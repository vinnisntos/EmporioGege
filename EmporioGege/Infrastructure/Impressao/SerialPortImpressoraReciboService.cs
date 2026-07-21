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

            // SerialPort.Open()/Write() são 100% síncronos (não existe equivalente async
            // multiplataforma confiável) - rodar isso direto na thread da requisição prendia
            // uma thread do pool compartilhado entre TODOS os tenants por até WriteTimeout (3s)
            // toda vez que uma impressora estivesse travada/desconectada. Task.Run devolve a
            // thread da requisição pro pool imediatamente; só a impressão em si (bem mais barata
            // de sobrar threads pra isso) roda bloqueada numa worker thread.
            return Task.Run(() =>
            {
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

                    return true;
                }
                catch (Exception ex)
                {
                    // Impressora desligada, porta inexistente/ocupada, sem papel etc. - a venda
                    // já foi gravada no banco antes de chegar aqui, então isso nunca pode
                    // propagar e derrubar a resposta pro caixa; só loga e avisa via retorno false.
                    logger.LogWarning(ex, "Falha ao imprimir recibo da venda {VendaId} na porta {Porta}.", recibo.VendaId, config.Porta);
                    return false;
                }
            }, ct);
        }
    }
}
