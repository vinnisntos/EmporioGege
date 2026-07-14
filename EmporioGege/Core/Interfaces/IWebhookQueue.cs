namespace EmporioGege.Core.Interfaces
{
    // Fila em memória: só um "toque de campainha" de baixa latência para o BackgroundService
    // acordar. A fonte da verdade durável é a tabela webhooks_recebidos (via IWebhookRepository) —
    // se o processo reiniciar com itens ainda não processados na fila em memória, o
    // BackgroundService os recupera via ListarPendentesAsync no startup.
    public interface IWebhookQueue
    {
        void Sinalizar(Guid webhookId);

        IAsyncEnumerable<Guid> LerAsync(CancellationToken ct);
    }
}
