using System.Threading.RateLimiting;

namespace EmporioGege.Infrastructure.Auth
{
    // Proteção de força bruta no login. Combina DUAS regras independentes - as duas precisam
    // permitir, senão nenhuma sozinha é suficiente:
    //   - por e-mail tentado: impede credential stuffing contra UMA conta específica, não
    //     importa de quantos IPs diferentes venha o ataque (ex.: botnet/proxy rotativo).
    //   - por IP de origem: impede um único atacante testando senha contra várias contas
    //     rapidamente a partir da mesma origem.
    // Ambas de janela fixa e generosas o bastante pra não travar um usuário legítimo que
    // erra a senha algumas vezes.
    public class LoginTentativaLimiter
    {
        private readonly PartitionedRateLimiter<string> _porEmail = PartitionedRateLimiter.Create<string, string>(email =>
            RateLimitPartition.GetFixedWindowLimiter(email, _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(5),
                PermitLimit = 8,
                QueueLimit = 0
            }));

        private readonly PartitionedRateLimiter<string> _porIp = PartitionedRateLimiter.Create<string, string>(ip =>
            RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(5),
                PermitLimit = 20,
                QueueLimit = 0
            }));

        public bool PodeTentar(string email, string ip)
        {
            var emailNormalizado = email.Trim().ToLowerInvariant();

            // Sempre consome de AMBOS os limitadores, mesmo se um já negar - a tentativa
            // aconteceu de qualquer forma e conta pras duas dimensões.
            using var leaseEmail = _porEmail.AttemptAcquire(emailNormalizado);
            using var leaseIp = _porIp.AttemptAcquire(ip);

            return leaseEmail.IsAcquired && leaseIp.IsAcquired;
        }
    }
}
