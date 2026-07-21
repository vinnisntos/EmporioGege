using Dapper;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace EmporioGege.Application.Services
{
    public class CadastroLojaService(
        IDbConnectionFactory connectionFactory,
        ITenantService tenantService,
        IFaturamentoService faturamentoService,
        IConfiguration configuration,
        ILogger<CadastroLojaService> logger) : ICadastroLojaService
    {
        // Preços fixos por plano, resolvidos aqui no servidor - nunca aceitos de um campo do
        // formulário público (ver comentário em CadastroLojaDtos.cs). Mesmos valores sugeridos
        // usados em Pages/Index.cshtml e em SuperAdmin/Adegas/Editar.
        private static readonly Dictionary<string, decimal> PrecosPlano = new()
        {
            ["start"] = 119.00m,
            ["pro"] = 199.00m,
            ["enterprise"] = 349.00m
        };

        public async Task<CadastroLojaResultado> CadastrarAsync(CadastrarLojaDto dto, CancellationToken ct = default)
        {
            if (!PrecosPlano.TryGetValue(dto.Plano, out var valorMensalidade))
                return new CadastroLojaResultado(false, null, "Plano inválido.", "validacao");

            await using (var connection = await connectionFactory.CreateOpenConnectionAsync(ct))
            {
                // Guard contra reenvio duplicado (F5, back-button) - não é uma constraint de
                // unicidade real no banco, só uma checagem barata pra evitar criar um segundo
                // tenant órfão quando o mesmo formulário é enviado de novo.
                var jaExiste = await connection.QuerySingleOrDefaultAsync<int?>(new CommandDefinition(
                    "SELECT 1 FROM tenants WHERE email_dono = @EmailDono OR cnpj = @Cnpj LIMIT 1",
                    new { dto.EmailDono, dto.Cnpj }, cancellationToken: ct));

                if (jaExiste is not null)
                {
                    return new CadastroLojaResultado(false, null,
                        "Já existe um cadastro em andamento com esse e-mail ou CNPJ. Confirme seu e-mail ou entre em contato com o suporte.",
                        "duplicado");
                }
            }

            // 1) Cria o tenant como "pendente" - bloqueia login (ver Login.cshtml.cs) até o
            // primeiro pagamento ser confirmado pelo webhook do Asaas. DataExpiracao aqui é só
            // um placeholder (o "pendente" tem seu próprio case no switch de bloqueio, então
            // nunca cai na checagem de expiração) - o webhook seta a data real quando ativa.
            Guid tenantId;
            try
            {
                tenantId = await tenantService.SalvarAsync(new SalvarTenantDto(
                    null, dto.NomeFantasia, dto.NomeRepresentante, dto.CpfRgDono, dto.Cnpj, dto.CidadeEstado,
                    null, dto.TelefoneDono, null, dto.EmailDono, "pendente", DateTime.UtcNow), ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao criar tenant no autocadastro (CNPJ {Cnpj}).", dto.Cnpj);
                return new CadastroLojaResultado(false, null, "Não foi possível registrar sua loja. Tente novamente em instantes.", "tenant");
            }

            // 2) Persiste plano/valor já aqui, mesmo antes da assinatura existir de verdade -
            // não fatal se falhar, serve só pra quem for finalizar manualmente (ver
            // SuperAdmin/Adegas/Editar) saber qual plano foi escolhido.
            try
            {
                await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
                await connection.ExecuteAsync(new CommandDefinition(
                    "UPDATE tenants SET plano = @Plano, valor_mensalidade = @ValorMensalidade WHERE id = @TenantId",
                    new { dto.Plano, ValorMensalidade = valorMensalidade, TenantId = tenantId }, cancellationToken: ct));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Falha ao persistir plano/valor no autocadastro (tenant {TenantId}) - não fatal.", tenantId);
            }

            // 3) Cria o login (Supabase Auth) - cliente isolado, mesmo padrão de
            // FuncionarioService.CriarAsync, já que aqui também não existe sessão/tenant
            // ambiente (o "chamador" é um visitante anônimo, não um admin logado).
            Guid usuarioId;
            try
            {
                var url = configuration["Supabase:Url"]!;
                var key = configuration["Supabase:Key"]!;
                var clienteIsolado = new Supabase.Client(url, key, new Supabase.SupabaseOptions { AutoConnectRealtime = false });

                var sessao = await clienteIsolado.Auth.SignUp(dto.EmailDono, dto.Senha);
                if (sessao?.User?.Id is null)
                {
                    return new CadastroLojaResultado(false, tenantId,
                        "Sua loja foi registrada, mas não conseguimos criar seu login (verifique se o e-mail já está cadastrado ou se a senha é forte o suficiente). Entre em contato com o suporte informando o CNPJ para finalizarmos manualmente.",
                        "supabase");
                }

                usuarioId = Guid.Parse(sessao.User.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao criar login Supabase no autocadastro (tenant {TenantId}).", tenantId);
                return new CadastroLojaResultado(false, tenantId,
                    "Sua loja foi registrada, mas houve um erro ao criar seu login. Entre em contato com o suporte informando o CNPJ para finalizarmos manualmente.",
                    "supabase");
            }

            // 4) Vincula o login criado ao tenant como administrador.
            try
            {
                await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
                await connection.ExecuteAsync(new CommandDefinition(
                    """
                    INSERT INTO profiles (id, tenant_id, nome, role, updated_at)
                    VALUES (@Id, @TenantId, @Nome, 'administrador', now())
                    """,
                    new { Id = usuarioId, TenantId = tenantId, Nome = dto.NomeRepresentante }, cancellationToken: ct));
            }
            catch (Exception ex)
            {
                // Loja e login existem, mas sem o vínculo o dono não consegue acessar nada -
                // fica registrado aqui pra alguém linkar manualmente (INSERT de uma linha só).
                logger.LogError(ex, "Falha ao vincular perfil no autocadastro (tenant {TenantId}, usuário {UsuarioId}).", tenantId, usuarioId);
                return new CadastroLojaResultado(false, tenantId,
                    "Sua loja e seu login foram criados, mas houve um problema ao finalizar o vínculo. Entre em contato com o suporte informando o CNPJ para finalizarmos manualmente.",
                    "perfil");
            }

            // 5) Cria a assinatura de verdade no Asaas - reaproveita o mesmo serviço já usado
            // pelo superadmin em SuperAdmin/Adegas/Editar. Diferente das etapas acima, uma
            // falha aqui NÃO invalida o cadastro: loja, login e perfil já são válidos e
            // funcionais (só ficam bloqueados em "pendente" até alguém acionar "Criar
            // Assinatura" manualmente naquela mesma tela).
            var resultadoAssinatura = await faturamentoService.CriarAssinaturaAsync(tenantId, dto.Plano, valorMensalidade, dto.TipoCobranca, ct);

            if (!resultadoAssinatura.Sucesso)
            {
                logger.LogWarning("Assinatura Asaas falhou no autocadastro (tenant {TenantId}): {Mensagem}", tenantId, resultadoAssinatura.MensagemErro);
                return new CadastroLojaResultado(true, tenantId,
                    "Cadastro recebido! Falta confirmar seu e-mail (enviamos um link de confirmação) - o pagamento da assinatura será configurado pela nossa equipe em instantes, e seu acesso libera automaticamente assim que for confirmado.",
                    "asaas");
            }

            return new CadastroLojaResultado(true, tenantId,
                "Cadastro recebido! Confirme seu e-mail pelo link que enviamos, e aguarde a confirmação do primeiro pagamento - seu acesso libera automaticamente assim que identificarmos.",
                null);
        }
    }
}
