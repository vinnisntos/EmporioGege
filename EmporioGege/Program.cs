using Microsoft.AspNetCore.Authentication.Cookies;
using Supabase;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuração do Supabase (Ajustado com '!' para sumir o Warning CS8604)
var url = builder.Configuration["Supabase:Url"]!;
var key = builder.Configuration["Supabase:Key"]!;

builder.Services.AddScoped<Client>(_ => new Client(url, key, new SupabaseOptions { AutoConnectRealtime = true }));

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
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// 5. ATENÇÃO: A ordem aqui importa muito!
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();