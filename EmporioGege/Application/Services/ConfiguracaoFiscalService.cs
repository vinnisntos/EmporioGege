using Dapper;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Interfaces;
using EmporioGege.Core.Security;

namespace EmporioGege.Application.Services
{
    public class ConfiguracaoFiscalService(
        IDbConnectionFactory connectionFactory, ITenantProvider tenantProvider,
        IFocusNfeClient focusNfeClient, CriptografiaSimetrica criptografia) : IConfiguracaoFiscalService
    {
        public async Task<ConfiguracaoFiscalDto?> ObterAsync(CancellationToken ct = default)
        {
            var tenantId = tenantProvider.RequireTenantId();

            await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
            return await connection.QuerySingleOrDefaultAsync<ConfiguracaoFiscalDto>(new CommandDefinition(
                """
                SELECT razao_social AS RazaoSocial, regime_tributario AS RegimeTributario,
                       inscricao_estadual AS InscricaoEstadual, logradouro AS Logradouro,
                       numero AS Numero, bairro AS Bairro, municipio AS Municipio, uf AS Uf, cep AS Cep,
                       nfce_habilitada AS NfceHabilitada, focus_nfe_empresa_id AS FocusNfeEmpresaId
                FROM tenants
                WHERE id = @TenantId
                """,
                new { TenantId = tenantId }, cancellationToken: ct));
        }

        public async Task SalvarDadosCadastraisAsync(SalvarConfiguracaoFiscalDto dto, CancellationToken ct = default)
        {
            var tenantId = tenantProvider.RequireTenantId();

            await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
            var linhasAfetadas = await connection.ExecuteAsync(new CommandDefinition(
                """
                UPDATE tenants SET
                    razao_social = @RazaoSocial, regime_tributario = @RegimeTributario,
                    inscricao_estadual = @InscricaoEstadual, logradouro = @Logradouro,
                    numero = @Numero, bairro = @Bairro, municipio = @Municipio, uf = @Uf, cep = @Cep
                WHERE id = @TenantId
                """,
                new
                {
                    TenantId = tenantId, dto.RazaoSocial, dto.RegimeTributario, dto.InscricaoEstadual,
                    dto.Logradouro, dto.Numero, dto.Bairro, dto.Municipio, dto.Uf, dto.Cep
                }, cancellationToken: ct));

            if (linhasAfetadas == 0)
                throw new InvalidOperationException($"Loja {tenantId} não encontrada.");
        }

        public async Task<CriarEmpresaFocusNfeResultado> RegistrarNaFocusNfeAsync(byte[] certificadoBytes, string senhaCertificado, CancellationToken ct = default)
        {
            var tenantId = tenantProvider.RequireTenantId();
            var dados = await ObterAsync(ct)
                ?? throw new InvalidOperationException($"Loja {tenantId} não encontrada.");

            if (dados.RazaoSocial is null || dados.RegimeTributario is null || dados.Logradouro is null
                || dados.Numero is null || dados.Bairro is null || dados.Municipio is null || dados.Uf is null || dados.Cep is null)
            {
                return new CriarEmpresaFocusNfeResultado(false, null, null, null,
                    "Preencha e salve todos os dados cadastrais obrigatórios (razão social, regime tributário e endereço completo) antes de registrar na Focus NFe.");
            }

            await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);

            // CNPJ vem do cadastro geral da loja (tenants.cnpj, preenchido em SuperAdmin/Adegas) -
            // não duplicado aqui pra não correr o risco de ficar dessincronizado.
            var cnpj = await connection.QuerySingleAsync<string>(new CommandDefinition(
                "SELECT cnpj FROM tenants WHERE id = @TenantId", new { TenantId = tenantId }, cancellationToken: ct));

            var resultado = await focusNfeClient.CriarEmpresaAsync(new CriarEmpresaFocusNfeDto(
                dados.RazaoSocial, cnpj, dados.RegimeTributario.Value, dados.InscricaoEstadual,
                dados.Logradouro, dados.Numero, dados.Bairro, dados.Municipio, dados.Uf, dados.Cep,
                certificadoBytes, senhaCertificado), ct);

            if (!resultado.Sucesso)
                return resultado;

            var tokenProducaoCifrado = resultado.TokenProducao is not null ? criptografia.Cifrar(resultado.TokenProducao) : null;
            var tokenHomologacaoCifrado = resultado.TokenHomologacao is not null ? criptografia.Cifrar(resultado.TokenHomologacao) : null;

            await connection.ExecuteAsync(new CommandDefinition(
                """
                UPDATE tenants SET
                    focus_nfe_empresa_id = @EmpresaId,
                    focus_nfe_token_producao_cifrado = @TokenProducaoCifrado,
                    focus_nfe_token_homologacao_cifrado = @TokenHomologacaoCifrado,
                    nfce_habilitada = true
                WHERE id = @TenantId
                """,
                new
                {
                    TenantId = tenantId, resultado.EmpresaId,
                    TokenProducaoCifrado = tokenProducaoCifrado, TokenHomologacaoCifrado = tokenHomologacaoCifrado
                },
                cancellationToken: ct));

            return resultado;
        }
    }
}
