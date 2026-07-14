namespace EmporioGege.Infrastructure.Tenancy
{
    // Fluxos sem HttpContext (BackgroundService, jobs) não têm Claims de usuário logado
    // para o HttpContextTenantProvider ler. Esta classe permite esses fluxos declararem
    // explicitamente "a partir daqui, o tenant corrente é X" — escopado por AsyncLocal,
    // então não vaza entre itens de fila processados em sequência nem entre requisições.
    public class AmbientTenantContext
    {
        private readonly AsyncLocal<Guid?> _tenantId = new();

        public Guid? TenantId => _tenantId.Value;

        public IDisposable BeginScope(Guid tenantId)
        {
            var anterior = _tenantId.Value;
            _tenantId.Value = tenantId;
            return new Escopo(() => _tenantId.Value = anterior);
        }

        private sealed class Escopo(Action aoDescartar) : IDisposable
        {
            public void Dispose() => aoDescartar();
        }
    }
}
