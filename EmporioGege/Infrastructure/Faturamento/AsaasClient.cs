using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Interfaces;

namespace EmporioGege.Infrastructure.Faturamento
{
    public class AsaasClient(HttpClient httpClient, ILogger<AsaasClient> logger) : IAsaasClient
    {
        private static readonly JsonSerializerOptions SerializacaoOpcoes = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public async Task<CriarClienteAsaasResultado> CriarClienteAsync(CriarClienteAsaasDto dto, CancellationToken ct = default)
        {
            var corpo = new
            {
                name = dto.Nome,
                cpfCnpj = dto.CpfCnpj is not null ? SomenteDigitos(dto.CpfCnpj) : null,
                email = dto.Email,
                mobilePhone = dto.Telefone
            };

            using var conteudoRequisicao = new StringContent(JsonSerializer.Serialize(corpo, SerializacaoOpcoes), Encoding.UTF8, "application/json");
            using var resposta = await httpClient.PostAsync("customers", conteudoRequisicao, ct);
            var conteudo = await resposta.Content.ReadAsStringAsync(ct);

            if (!resposta.IsSuccessStatusCode)
            {
                logger.LogWarning("Asaas recusou a criação do cliente: {Status} {Conteudo}", resposta.StatusCode, conteudo);
                return new CriarClienteAsaasResultado(false, null,
                    ExtrairMensagemErro(conteudo) ?? $"Asaas recusou a criação do cliente (HTTP {(int)resposta.StatusCode}).");
            }

            using var documento = JsonDocument.Parse(conteudo);
            var id = documento.RootElement.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            return new CriarClienteAsaasResultado(true, id, null);
        }

        public async Task<CriarAssinaturaAsaasResultado> CriarAssinaturaAsync(CriarAssinaturaAsaasDto dto, CancellationToken ct = default)
        {
            var corpo = new
            {
                customer = dto.ClienteId,
                billingType = dto.TipoCobranca,
                value = dto.Valor,
                cycle = dto.Ciclo,
                nextDueDate = dto.ProximoVencimento.ToString("yyyy-MM-dd"),
                description = dto.Descricao
            };

            using var conteudoRequisicao = new StringContent(JsonSerializer.Serialize(corpo, SerializacaoOpcoes), Encoding.UTF8, "application/json");
            using var resposta = await httpClient.PostAsync("subscriptions", conteudoRequisicao, ct);
            var conteudo = await resposta.Content.ReadAsStringAsync(ct);

            if (!resposta.IsSuccessStatusCode)
            {
                logger.LogWarning("Asaas recusou a criação da assinatura: {Status} {Conteudo}", resposta.StatusCode, conteudo);
                return new CriarAssinaturaAsaasResultado(false, null, null,
                    ExtrairMensagemErro(conteudo) ?? $"Asaas recusou a criação da assinatura (HTTP {(int)resposta.StatusCode}).");
            }

            using var documento = JsonDocument.Parse(conteudo);
            var raiz = documento.RootElement;
            var id = raiz.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            var status = raiz.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null;
            return new CriarAssinaturaAsaasResultado(true, id, status, null);
        }

        private static string? ExtrairMensagemErro(string corpoResposta)
        {
            try
            {
                using var documento = JsonDocument.Parse(corpoResposta);
                if (documento.RootElement.TryGetProperty("errors", out var errosEl) && errosEl.GetArrayLength() > 0)
                {
                    var primeiro = errosEl[0];
                    if (primeiro.TryGetProperty("description", out var msgEl))
                        return msgEl.GetString();
                }
            }
            catch (JsonException)
            {
                // Resposta não veio em JSON - sem detalhe extra pra extrair.
            }
            return null;
        }

        private static string SomenteDigitos(string valor) => new([.. valor.Where(char.IsDigit)]);
    }
}
