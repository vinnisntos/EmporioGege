using System.Security.Cryptography;
using System.Text;

namespace EmporioGege.Core.Security
{
    // Verificação de assinatura HMAC-SHA256 sobre o corpo bruto da requisição — o padrão
    // usado por Stripe/GitHub/iFood para webhooks. NOME DO HEADER E ALGORITMO EXATOS DA
    // ZÉ DELIVERY AINDA NÃO CONFIRMADOS (doc pública não expõe a seção "Validação de
    // autenticidade do evento" sem aprovação de parceiro) — ajustar aqui se divergir.
    // Fica no Core (não em Infrastructure) porque é lógica pura de segurança/algoritmo,
    // sem dependência nenhuma de banco, HTTP ou qualquer detalhe de infraestrutura.
    public static class HmacSignatureValidator
    {
        public static bool IsValid(ReadOnlySpan<byte> corpoBruto, string? assinaturaRecebida, string segredo)
        {
            if (string.IsNullOrWhiteSpace(assinaturaRecebida))
                return false;

            var chave = Encoding.UTF8.GetBytes(segredo);
            var hashCalculado = HMACSHA256.HashData(chave, corpoBruto);
            var hashCalculadoHex = Convert.ToHexStringLower(hashCalculado);

            var assinaturaNormalizada = RemoverPrefixoConhecido(assinaturaRecebida.Trim());

            // Comparação em tempo constante — evita side-channel (timing attack) na validação.
            var bytesEsperados = Encoding.UTF8.GetBytes(hashCalculadoHex);
            var bytesRecebidos = Encoding.UTF8.GetBytes(assinaturaNormalizada.ToLowerInvariant());

            return bytesEsperados.Length == bytesRecebidos.Length
                && CryptographicOperations.FixedTimeEquals(bytesEsperados, bytesRecebidos);
        }

        private static string RemoverPrefixoConhecido(string assinatura) =>
            assinatura.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase)
                ? assinatura["sha256=".Length..]
                : assinatura;
    }
}
