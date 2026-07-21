using System.Threading.RateLimiting;

namespace EmporioGege.Infrastructure.Auth
{
    // Proteção de força bruta pra senha de supervisor (SupervisorAutorizacaoService): sem isso,
    // um vendedor mal-intencionado podia tentar senha de administrador/superadmin indefinidamente,
    // cada tentativa disparando um SignIn de verdade contra o Supabase Auth. Particionado pelo id
    // do vendedor QUE ESTÁ TENTANDO (claim de sessão já autenticada, não um campo de formulário),
    // então cada terminal/pessoa tem sua própria janela - não é possível um vendedor travar a
    // tentativa de outro.
    public class SupervisorTentativaLimiter
    {
        private readonly PartitionedRateLimiter<string> _limiter = PartitionedRateLimiter.Create<string, string>(solicitanteId =>
            RateLimitPartition.GetFixedWindowLimiter(solicitanteId, _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(2),
                PermitLimit = 5,
                QueueLimit = 0
            }));

        public bool PodeTentar(string solicitanteId)
        {
            using var lease = _limiter.AttemptAcquire(solicitanteId);
            return lease.IsAcquired;
        }
    }
}
