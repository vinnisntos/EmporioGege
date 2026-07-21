using System.Threading.RateLimiting;

namespace EmporioGege.Infrastructure.Auth
{
    // Mesma ideia do LoginTentativaLimiter (duas regras independentes, por e-mail e por IP),
    // mas bem mais restritivo: cada tentativa bem-sucedida aqui cria uma conta Supabase Auth
    // REAL e um cliente/assinatura REAL no Asaas - não é só uma tentativa de senha errada.
    public class CadastroLojaTentativaLimiter
    {
        private readonly PartitionedRateLimiter<string> _porEmail = PartitionedRateLimiter.Create<string, string>(email =>
            RateLimitPartition.GetFixedWindowLimiter(email, _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromHours(1),
                PermitLimit = 3,
                QueueLimit = 0
            }));

        private readonly PartitionedRateLimiter<string> _porIp = PartitionedRateLimiter.Create<string, string>(ip =>
            RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromHours(1),
                PermitLimit = 5,
                QueueLimit = 0
            }));

        public bool PodeTentar(string email, string ip)
        {
            var emailNormalizado = email.Trim().ToLowerInvariant();

            using var leaseEmail = _porEmail.AttemptAcquire(emailNormalizado);
            using var leaseIp = _porIp.AttemptAcquire(ip);

            return leaseEmail.IsAcquired && leaseIp.IsAcquired;
        }
    }
}
