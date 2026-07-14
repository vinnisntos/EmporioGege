using System.Threading.Channels;
using EmporioGege.Core.Interfaces;

namespace EmporioGege.Infrastructure.Webhooks
{
    public class InMemoryWebhookQueue : IWebhookQueue
    {
        // Bounded + DropWrite: a fila em memória é só um sinal de "acorda e olha a tabela";
        // se ela encher (fluxo anormal de webhooks), o evento continua durável em
        // webhooks_recebidos e é recuperado no próximo catch-up do BackgroundService —
        // não faz sentido bloquear a resposta HTTP do webhook por causa da fila em memória.
        private readonly Channel<Guid> _channel = Channel.CreateBounded<Guid>(
            new BoundedChannelOptions(1000) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });

        public void Sinalizar(Guid webhookId) => _channel.Writer.TryWrite(webhookId);

        public IAsyncEnumerable<Guid> LerAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
    }
}
