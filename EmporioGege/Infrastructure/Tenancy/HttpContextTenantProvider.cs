using EmporioGege.Core.Interfaces;

namespace EmporioGege.Infrastructure.Tenancy
{
    public class HttpContextTenantProvider(IHttpContextAccessor httpContextAccessor, AmbientTenantContext ambientTenantContext) : ITenantProvider
    {
        public Guid? TenantId
        {
            get
            {
                // BackgroundServices (ex.: processador de webhooks) não têm HttpContext/Claims;
                // eles declaram o tenant explicitamente via AmbientTenantContext.BeginScope(...).
                if (ambientTenantContext.TenantId is { } tenantDoEscopoAmbiente)
                    return tenantDoEscopoAmbiente;

                var claimValue = httpContextAccessor.HttpContext?.User.FindFirst("TenantId")?.Value;
                return Guid.TryParse(claimValue, out var tenantId) ? tenantId : null;
            }
        }

        public Guid RequireTenantId() =>
            TenantId ?? throw new InvalidOperationException("Usuário autenticado não possui TenantId (ex.: superadmin fora de um contexto de loja).");
    }
}
