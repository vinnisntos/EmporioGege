using Supabase;

var builder = WebApplication.CreateBuilder(args);

// 1. Pegamos as credenciais que vamos colocar no appsettings.json
var url = builder.Configuration["Supabase:Url"];
var key = builder.Configuration["Supabase:Key"];

// 2. Registramos o cliente do Supabase para usar nas páginas
builder.Services.AddScoped<Client>(_ => new Client(url, key, new SupabaseOptions { AutoConnectRealtime = true }));

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();