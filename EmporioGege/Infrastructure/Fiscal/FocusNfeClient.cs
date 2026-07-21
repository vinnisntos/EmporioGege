using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace EmporioGege.Infrastructure.Fiscal
{
    public class FocusNfeClient(HttpClient httpClient, IOptions<FocusNfeOptions> opcoes, ILogger<FocusNfeClient> logger) : IFocusNfeClient
    {
        private static readonly JsonSerializerOptions SerializacaoOpcoes = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public async Task<CriarEmpresaFocusNfeResultado> CriarEmpresaAsync(CriarEmpresaFocusNfeDto dto, CancellationToken ct = default)
        {
            var corpo = new
            {
                nome = dto.RazaoSocial,
                cnpj = SomenteDigitos(dto.Cnpj),
                regime_tributario = dto.RegimeTributario,
                inscricao_estadual = dto.InscricaoEstadual,
                logradouro = dto.Logradouro,
                numero = dto.Numero,
                bairro = dto.Bairro,
                municipio = dto.Municipio,
                uf = dto.Uf,
                cep = SomenteDigitos(dto.Cep),
                habilita_nfce = true,
                arquivo_certificado_base64 = Convert.ToBase64String(dto.CertificadoBytes),
                senha_certificado = dto.SenhaCertificado
            };

            using var requisicao = new HttpRequestMessage(HttpMethod.Post, "v2/empresas")
            {
                Content = new StringContent(JsonSerializer.Serialize(corpo, SerializacaoOpcoes), Encoding.UTF8, "application/json")
            };
            requisicao.Headers.Authorization = CriarAutenticacaoBasica(opcoes.Value.Token);

            using var resposta = await httpClient.SendAsync(requisicao, ct);
            var conteudo = await resposta.Content.ReadAsStringAsync(ct);

            if (!resposta.IsSuccessStatusCode)
            {
                // Nunca logar `conteudo` se algum dia esse payload passar a incluir dado sensível
                // devolvido pela Focus NFe - hoje é só erro de validação (campo + mensagem), sem
                // segredo, mas vale revisar se o formato de erro deles mudar no futuro.
                logger.LogWarning("Focus NFe recusou o cadastro da empresa {Cnpj}: {Status} {Conteudo}",
                    dto.Cnpj, resposta.StatusCode, conteudo);
                var mensagem = ExtrairMensagemErro(conteudo) ?? $"Focus NFe recusou o cadastro (HTTP {(int)resposta.StatusCode}).";
                return new CriarEmpresaFocusNfeResultado(false, null, null, null, mensagem);
            }

            using var documento = JsonDocument.Parse(conteudo);
            var raiz = documento.RootElement;
            var empresaId = raiz.TryGetProperty("id", out var idEl) ? idEl.ToString() : null;
            var tokenProducao = raiz.TryGetProperty("token_producao", out var tpEl) ? tpEl.GetString() : null;
            var tokenHomologacao = raiz.TryGetProperty("token_homologacao", out var thEl) ? thEl.GetString() : null;

            return new CriarEmpresaFocusNfeResultado(true, empresaId, tokenProducao, tokenHomologacao, null);
        }

        private static string? ExtrairMensagemErro(string corpoResposta)
        {
            try
            {
                using var documento = JsonDocument.Parse(corpoResposta);
                if (documento.RootElement.TryGetProperty("erros", out var errosEl) && errosEl.GetArrayLength() > 0)
                {
                    var primeiro = errosEl[0];
                    if (primeiro.TryGetProperty("mensagem", out var msgEl))
                        return msgEl.GetString();
                }
                if (documento.RootElement.TryGetProperty("mensagem", out var mensagemGeral))
                    return mensagemGeral.GetString();
            }
            catch (JsonException)
            {
                // Resposta não veio em JSON (ex.: erro 5xx genérico de infraestrutura) - sem
                // detalhe extra pra extrair, fica só o HTTP status na mensagem genérica.
            }
            return null;
        }

        private static string SomenteDigitos(string valor) => new([.. valor.Where(char.IsDigit)]);

        private static AuthenticationHeaderValue CriarAutenticacaoBasica(string token) =>
            new("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{token}:")));
    }
}
