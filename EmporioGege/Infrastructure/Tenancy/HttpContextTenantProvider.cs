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

                var usuario = httpContextAccessor.HttpContext?.User;

                // Superadmin "puro" não tem TenantId (claim vazia) — mas pode ter "entrado"
                // no contexto de uma loja específica via SuperAdmin/Adegas (Entrar), o que
                // grava a claim ImpersonatedTenantId no mesmo cookie assinado. Só superadmin
                // consegue gravar essa claim (handler gated por SuperAdminOnly), então não dá
                // pra um administrador/vendedor forjar acesso a outro tenant por aqui.
                var claimImpersonada = usuario?.FindFirst("ImpersonatedTenantId")?.Value;
                if (Guid.TryParse(claimImpersonada, out var tenantImpersonado))
                    return tenantImpersonado;

                var claimValue = usuario?.FindFirst("TenantId")?.Value;
                return Guid.TryParse(claimValue, out var tenantId) ? tenantId : null;
            }
        }

        public Guid RequireTenantId() =>
            TenantId ?? throw new InvalidOperationException("Usuário autenticado não possui TenantId (ex.: superadmin fora de um contexto de loja).");
    }
}
