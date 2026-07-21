namespace EmporioGege.Core.Interfaces
{
    public interface IAsaasWebhookService
    {
        Task ProcessarAsync(string corpoJson, CancellationToken ct = default);
    }
}
