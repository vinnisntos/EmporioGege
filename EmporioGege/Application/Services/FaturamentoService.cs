using Dapper;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Interfaces;

namespace EmporioGege.Application.Services
{
    public class FaturamentoService(IDbConnectionFactory connectionFactory, IAsaasClient asaasClient) : IFaturamentoService
    {
        public async Task<AssinaturaTenantDto?> ObterAsync(Guid tenantId, CancellationToken ct = default)
        {
            await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
            return await connection.QuerySingleOrDefaultAsync<AssinaturaTenantDto>(new CommandDefinition(
                """
                SELECT plano AS Plano, valor_mensalidade AS ValorMensalidade, status_licenca AS StatusLicenca,
                       data_expiracao AS DataExpiracao, asaas_customer_id AS AsaasCustomerId,
                       asaas_subscription_id AS AsaasSubscriptionId
                FROM tenants
                WHERE id = @TenantId
                """,
                new { TenantId = tenantId }, cancellationToken: ct));
        }

        public async Task<CriarAssinaturaAsaasResultado> CriarAssinaturaAsync(
            Guid tenantId, string plano, decimal valorMensalidade, string tipoCobranca, CancellationToken ct = default)
        {
            await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);

            var loja = await connection.QuerySingleOrDefaultAsync<LojaResumoRow>(new CommandDefinition(
                """
                SELECT nome_fantasia AS NomeFantasia, cnpj AS Cnpj, email_dono AS EmailDono,
                       telefone_dono AS TelefoneDono, asaas_customer_id AS AsaasCustomerId
                FROM tenants WHERE id = @TenantId
                """,
                new { TenantId = tenantId }, cancellationToken: ct));

            if (loja is null)
                return new CriarAssinaturaAsaasResultado(false, null, null, $"Loja {tenantId} não encontrada.");

            var clienteId = loja.AsaasCustomerId;
            if (string.IsNullOrEmpty(clienteId))
            {
                var resultadoCliente = await asaasClient.CriarClienteAsync(
                    new CriarClienteAsaasDto(loja.NomeFantasia, loja.Cnpj, loja.EmailDono, loja.TelefoneDono), ct);

                if (!resultadoCliente.Sucesso)
                    return new CriarAssinaturaAsaasResultado(false, null, null, resultadoCliente.MensagemErro);

                clienteId = resultadoCliente.ClienteId!;
                await connection.ExecuteAsync(new CommandDefinition(
                    "UPDATE tenants SET asaas_customer_id = @ClienteId WHERE id = @TenantId",
                    new { ClienteId = clienteId, TenantId = tenantId }, cancellationToken: ct));
            }

            // Primeira cobrança daqui a 7 dias - dá tempo da loja receber o boleto/link antes
            // do vencimento (nextDueDate é OBRIGATÓRIO na criação, mesmo pra assinatura recorrente).
            var resultadoAssinatura = await asaasClient.CriarAssinaturaAsync(new CriarAssinaturaAsaasDto(
                clienteId, valorMensalidade, "MONTHLY", tipoCobranca, $"PendurAi - Plano {plano}",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))), ct);

            if (!resultadoAssinatura.Sucesso)
                return resultadoAssinatura;

            await connection.ExecuteAsync(new CommandDefinition(
                """
                UPDATE tenants SET plano = @Plano, valor_mensalidade = @ValorMensalidade, asaas_subscription_id = @AssinaturaId
                WHERE id = @TenantId
                """,
                new { Plano = plano, ValorMensalidade = valorMensalidade, AssinaturaId = resultadoAssinatura.AssinaturaId, TenantId = tenantId },
                cancellationToken: ct));

            return resultadoAssinatura;
        }

        private sealed record LojaResumoRow(string NomeFantasia, string Cnpj, string EmailDono, string TelefoneDono, string? AsaasCustomerId);
    }
}
