namespace EmporioGege.Core.Interfaces
{
    // Centraliza o texto e o disparo dos e-mails do funil de cobrança da própria loja
    // (trial -> pagamento -> confirmação) - ver Application/Services/BillingNotificationService
    // pro copy exato de cada um. Mantém CadastroLojaService, FaturamentoService e
    // AsaasWebhookService livres de HTML de e-mail no meio da lógica de negócio.
    public interface IBillingNotificationService
    {
        Task EnviarBoasVindasTrialAsync(Guid tenantId, int diasTrial, CancellationToken ct = default);

        Task EnviarInstrucoesPagamentoAsync(Guid tenantId, CancellationToken ct = default);

        Task EnviarPagamentoConfirmadoAsync(Guid tenantId, CancellationToken ct = default);
    }
}
