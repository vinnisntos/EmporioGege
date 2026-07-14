using EmporioGege.Core.Enums;

namespace EmporioGege.Core.Interfaces
{
    public enum ResultadoIngestaoWebhook
    {
        TokenInvalido,
        AssinaturaInvalida,
        PayloadInvalido,
        Duplicado,
        Aceito
    }

    public interface IWebhookIngestaoService
    {
        Task<ResultadoIngestaoWebhook> ReceberAsync(CanalIntegracao canal, string tokenUrl, byte[] corpoBruto, string? assinaturaRecebida, CancellationToken ct = default);
    }
}
