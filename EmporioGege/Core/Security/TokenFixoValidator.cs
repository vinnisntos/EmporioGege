using System.Security.Cryptography;
using System.Text;

namespace EmporioGege.Core.Security
{
    // Validação de token fixo enviado num header (ex.: "asaas-access-token" do webhook do
    // Asaas) - diferente do HmacSignatureValidator, aqui não há assinatura sobre o corpo,
    // só um valor configurado no cadastro do webhook que o provedor ecoa de volta em toda
    // chamada. Ainda assim comparado em tempo constante, pra não abrir side-channel (timing
    // attack) que ajude a descobrir o token por tentativa e erro.
    public static class TokenFixoValidator
    {
        public static bool IsValid(string? tokenRecebido, string tokenEsperado)
        {
            if (string.IsNullOrEmpty(tokenRecebido))
                return false;

            var bytesRecebidos = Encoding.UTF8.GetBytes(tokenRecebido);
            var bytesEsperados = Encoding.UTF8.GetBytes(tokenEsperado);

            return bytesRecebidos.Length == bytesEsperados.Length
                && CryptographicOperations.FixedTimeEquals(bytesRecebidos, bytesEsperados);
        }
    }
}
