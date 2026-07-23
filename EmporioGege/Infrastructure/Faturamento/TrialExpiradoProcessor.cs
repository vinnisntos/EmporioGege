using Dapper;
using EmporioGege.Core.Interfaces;

namespace EmporioGege.Infrastructure.Faturamento
{
    // Varre periodicamente os tenants em trial vencido (data_expiracao no passado) e converte
    // automaticamente em cobrança de verdade - gera a assinatura no Asaas, move status_licenca
    // pra "pendente" (bloqueia login até o pagamento ser confirmado) e dispara o e-mail com o
    // link de pagamento. Sem isso, um lojista que nunca voltou ao painel durante o trial
    // ficaria com acesso liberado pra sempre (data_expiracao só é checada no login, e mesmo lá
    // "trial" nunca bloqueia sozinho - ver Login.cshtml.cs).
    //
    // Intervalo de 1h é suficiente pra esse caso de uso (não é uma cobrança em tempo real tipo
    // webhook) - mesmo padrão defensivo do ZeDeliveryWebhookProcessor: nenhuma exceção pode
    // escapar de ExecuteAsync, senão derruba a aplicação inteira.
    public class TrialExpiradoProcessor(IServiceScopeFactory scopeFactory, ILogger<TrialExpiradoProcessor> logger) : BackgroundService
    {
        private static readonly TimeSpan Intervalo = TimeSpan.FromHours(1);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Intervalo);

            // Roda uma vez já na subida (não espera 1h pro primeiro ciclo) - importante pra
            // ambientes que reiniciam com frequência não deixarem trials vencidos acumulando.
            await ProcessarTrialsVencidosAsync(stoppingToken);

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                    await ProcessarTrialsVencidosAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Encerramento normal do host.
            }
        }

        private async Task ProcessarTrialsVencidosAsync(CancellationToken ct)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var connectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();

                await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
                var tenantsVencidos = await connection.QueryAsync<Guid>(new CommandDefinition(
                    "SELECT id FROM tenants WHERE status_licenca = 'trial' AND data_expiracao < now()",
                    cancellationToken: ct));

                foreach (var tenantId in tenantsVencidos)
                    await ConverterTrialAsync(tenantId, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao varrer trials vencidos.");
            }
        }

        private async Task ConverterTrialAsync(Guid tenantId, CancellationToken ct)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var faturamentoService = scope.ServiceProvider.GetRequiredService<IFaturamentoService>();
                var billingNotificationService = scope.ServiceProvider.GetRequiredService<IBillingNotificationService>();

                var resultado = await faturamentoService.IniciarCobrancaTrialAsync(tenantId, ct);
                if (!resultado.Sucesso)
                {
                    logger.LogWarning("Falha ao converter trial vencido do tenant {TenantId}: {Mensagem}", tenantId, resultado.MensagemErro);
                    return;
                }

                await billingNotificationService.EnviarInstrucoesPagamentoAsync(tenantId, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro inesperado convertendo trial vencido do tenant {TenantId}.", tenantId);
            }
        }
    }
}
