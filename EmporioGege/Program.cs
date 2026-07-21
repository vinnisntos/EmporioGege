using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Supabase;
using EmporioGege.Application.Services;
using EmporioGege.Core.Enums;
using EmporioGege.Core.Interfaces;
using EmporioGege.Infrastructure.Auth;
using EmporioGege.Infrastructure.Data;
using EmporioGege.Infrastructure.Impressao;
using EmporioGege.Infrastructure.Tenancy;
using EmporioGege.Infrastructure.Webhooks;

var builder = WebApplication.CreateBuilder(args);

// 0. Dapper não conhece DateOnly como parâmetro nativamente (ver comentário no handler).
Dapper.SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

// 1. Configuração do Supabase (Ajustado com '!' para sumir o Warning CS8604)
var url = builder.Configuration["Supabase:Url"]!;
var key = builder.Configuration["Supabase:Key"]!;

builder.Services.AddScoped<Client>(_ => new Client(url, key, new SupabaseOptions { AutoConnectRealtime = true }));

// 1.1 Acesso direto ao Postgres (Npgsql/Dapper) para operações transacionais
// (ledger do caixa, baixa de estoque) que o cliente REST do Supabase não cobre.
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
builder.Services.AddScoped<ITenantProvider, HttpContextTenantProvider>();
builder.Services.AddScoped<IEstoqueService, EstoqueService>();
builder.Services.AddScoped<ICaixaLedgerService, CaixaLedgerService>();
builder.Services.AddScoped<ITurnoService, TurnoService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IVendaService, VendaService>();
builder.Services.AddScoped<ICatalogoService, CatalogoService>();
builder.Services.AddScoped<IProdutoService, ProdutoService>();
builder.Services.AddScoped<IFuncionarioService, FuncionarioService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IComandaService, ComandaService>();
builder.Services.AddSingleton<SupervisorTentativaLimiter>();
builder.Services.AddScoped<ISupervisorAutorizacaoService, SupervisorAutorizacaoService>();
builder.Services.AddScoped<ITenantService, TenantService>();

// Recibo impresso: impressora térmica ligada por porta serial (COM), configurada por
// máquina física (ver seção "Impressora" em appsettings.json / user-secrets locais) - cada
// PC de caixa tem a sua própria impressora pareada, muitas vezes numa porta diferente.
builder.Services.Configure<ImpressoraOptions>(builder.Configuration.GetSection("Impressora"));
builder.Services.AddScoped<IImpressoraReciboService, SerialPortImpressoraReciboService>();

// 1.2 Permite validar o antiforgery token via header em chamadas AJAX/fetch (PDV),
// já que essas requisições não enviam o campo de formulário __RequestVerificationToken.
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

// 1.3 Webhooks de integração (Zé Delivery): fila em memória + processamento assíncrono
// em BackgroundService. AmbientTenantContext permite esse worker (sem HttpContext/Claims)
// declarar o tenant corrente por evento processado.
builder.Services.AddSingleton<AmbientTenantContext>();
builder.Services.AddSingleton<IWebhookQueue, InMemoryWebhookQueue>();
builder.Services.AddScoped<IWebhookRepository, WebhookRepository>();
builder.Services.AddScoped<IWebhookIngestaoService, WebhookIngestaoService>();
builder.Services.AddHostedService<ZeDeliveryWebhookProcessor>();

// Rate limit por token de webhook (não por IP — parceiros costumam disparar de vários IPs/CDNs).
// Protege contra flood/DoS no endpoint público sem exigir autenticação de sessão.
//
// O limite por token sozinho é inútil contra um chamador anônimo: como o token vem da própria
// URL (rota controlada por quem chama), mandar um token diferente a cada request cria uma
// partição nova do FixedWindowRateLimiter toda vez, e o limite nunca acumula. Por isso ele é
// combinado (CreateChained = as duas regras precisam permitir) com um limite GLOBAL do endpoint,
// que não depende de nada que o chamador controle - nenhum volume de tokens distintos escapa dele.
var webhookPorToken = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
{
    var token = httpContext.Request.RouteValues["token"]?.ToString() ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
    return RateLimitPartition.GetFixedWindowLimiter(token, _ => new FixedWindowRateLimiterOptions
    {
        Window = TimeSpan.FromSeconds(10),
        PermitLimit = 30,
        QueueLimit = 0
    });
});

var webhookGlobal = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
    RateLimitPartition.GetFixedWindowLimiter("zedelivery-global", _ => new FixedWindowRateLimiterOptions
    {
        // Teto agregado do endpoint inteiro - existe só pra impedir que trocar de token vire uma
        // forma de nunca ser limitado. Testado ao vivo com 320 requests concorrentes usando tokens
        // distintos: um teto de 300/10s deixava passar gente demais pro pool de conexão do Supabase
        // (só 15 conexões no pooler em modo sessão), derrubando o app inteiro (500 em cascata pra
        // TODOS os tenants, não só o webhook) antes mesmo do rate limiter barrar alguém. 60/10s
        // ainda é bem mais que o tráfego normal de um parceiro sozinho (30/10s já é o teto por
        // token), mas protege o pool compartilhado de verdade.
        Window = TimeSpan.FromSeconds(10),
        PermitLimit = 60,
        QueueLimit = 0
    }));

// RateLimiterOptions.AddPolicy só aceita um Func<HttpContext, RateLimitPartition<TKey>> - não dá
// pra registrar um PartitionedRateLimiter<HttpContext> já pronto (como o CreateChained abaixo) por
// esse caminho. Por isso o limite combinado é aplicado direto no endpoint via AddEndpointFilter
// (ver mapeamento da rota), em vez de builder.Services.AddRateLimiter/app.UseRateLimiter.
var webhookRateLimiter = PartitionedRateLimiter.CreateChained(webhookPorToken, webhookGlobal);

// 2. Ativa a Autenticação por Cookies no navegador
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";         // Se tentar acessar algo trancado deslogado, vai para cá
        options.AccessDeniedPath = "/Auth/Login";   // Se tentar acessar algo que não tem permissão, volta para cá
        options.ExpireTimeSpan = TimeSpan.FromHours(8); // Cookie dura 1 turno de trabalho (8 horas)
    });

// 3. Define as Políticas de Segurança baseado nas Roles do banco
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("superadmin"));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("administrador", "superadmin"));
    options.AddPolicy("CaixaOnly", policy => policy.RequireRole("vendedor", "administrador", "superadmin"));
});

// 4. Registra o Razor Pages e tranca as pastas específicas do sistema
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/SuperAdmin", "SuperAdminOnly");
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
    options.Conventions.AuthorizeFolder("/Caixa", "CaixaOnly");

    // Superadmin pode acessar essas pastas sem ter "entrado" numa loja (SuperAdmin/Adegas) -
    // sem este filtro, qualquer página aqui dentro derrubava com erro 500 genérico ao tentar
    // resolver o tenant (ver ExigeTenantPageFilter).
    options.Conventions.AddFolderApplicationModelConvention("/Admin", model => model.Filters.Add(new TypeFilterAttribute(typeof(ExigeTenantPageFilter))));
    options.Conventions.AddFolderApplicationModelConvention("/Caixa", model => model.Filters.Add(new TypeFilterAttribute(typeof(ExigeTenantPageFilter))));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// ATENÇÃO: sem isso, o binding de decimal em formulários (preço, limite de crédito, saldo)
// usa a cultura pt-BR da máquina, que lê "." como separador de MILHAR — um <input type=number>
// sempre envia "20.00" (ponto decimal, padrão HTML5, independente do idioma do navegador),
// e sob pt-BR isso vira 2000. Confirmado ao vivo: um limite de crédito digitado como 20.00
// foi salvo como 2000.00. Fixamos a cultura de parsing em invariant; exibição de moeda (R$,
// vírgula decimal) continua controlada explicitamente com CultureInfo pt-BR em cada view.
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture),
    SupportedCultures = [CultureInfo.InvariantCulture],
    SupportedUICultures = [CultureInfo.InvariantCulture]
});

app.UseHttpsRedirection();
app.UseRouting();

// 5. ATENÇÃO: A ordem aqui importa muito!
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

// 6. Webhook do Zé Delivery — endpoint público (sem cookie de sessão), autenticado por
// token de URL + assinatura HMAC do corpo. Minimal API porque não é uma página navegável.
app.MapPost("/webhooks/zedelivery/{token}", async (
    string token,
    HttpRequest request,
    IWebhookIngestaoService ingestao,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    try
    {
        // Lê o corpo bruto ANTES de qualquer parse/model binding — a assinatura HMAC é
        // calculada sobre os bytes originais recebidos, não sobre uma reserialização.
        using var buffer = new MemoryStream();
        await request.Body.CopyToAsync(buffer, ct);
        var corpoBruto = buffer.ToArray();

        // Nome do header de assinatura AINDA NÃO CONFIRMADO com a doc de parceiro da Zé
        // Delivery — ajustar assim que tiver acesso real (ver comentário em HmacSignatureValidator).
        var assinatura = request.Headers["X-Ze-Signature"].FirstOrDefault();

        var resultado = await ingestao.ReceberAsync(CanalIntegracao.ZeDelivery, token, corpoBruto, assinatura, ct);

        return resultado switch
        {
            ResultadoIngestaoWebhook.TokenInvalido => Results.NotFound(),
            ResultadoIngestaoWebhook.AssinaturaInvalida => Results.Unauthorized(),
            ResultadoIngestaoWebhook.PayloadInvalido => Results.BadRequest(),
            ResultadoIngestaoWebhook.Duplicado => Results.Ok(new { status = "ja_processado" }),
            ResultadoIngestaoWebhook.Aceito => Results.Accepted(),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
    catch (Exception ex)
    {
        // Endpoint público e sem sessão: nunca deixar uma exceção não tratada vazar
        // stack trace/caminho de arquivo pra fora (visto isso acontecer ao testar sem
        // banco acessível — o handler global só cobre Razor Pages, não Minimal APIs).
        logger.LogError(ex, "Erro inesperado no webhook Zé Delivery.");
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
})
.AddEndpointFilter(async (context, next) =>
{
    using var lease = webhookRateLimiter.AttemptAcquire(context.HttpContext);
    if (!lease.IsAcquired)
        return Results.StatusCode(StatusCodes.Status429TooManyRequests);

    return await next(context);
})
.WithName("ZeDeliveryWebhook");

app.Run();