using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace EmporioGege.Core.Security
{
    public class CriptografiaOptions
    {
        public string ChaveCriptografia { get; set; } = default!;
    }

    // Cifra/decifra segredos que precisam ficar em repouso no banco (hoje só o token
    // por-empresa que a Focus NFe devolve depois de cadastrar a empresa - ver
    // Application/Services/ConfiguracaoFiscalService.cs). AES-256-GCM autenticado, com
    // nonce aleatório por chamada. Formato do texto cifrado (tudo em base64):
    // nonce (12 bytes) + tag (16 bytes) + texto cifrado.
    //
    // NÃO usado para o certificado digital (.pfx) da loja - esse nunca é persistido aqui
    // (nem cifrado); vai direto da tela pro Focus NFe, que passa a guardá-lo, por decisão
    // explícita de produto (menor superfície de risco pra um segredo dessa gravidade).
    public class CriptografiaSimetrica(IOptions<CriptografiaOptions> opcoes)
    {
        public string Cifrar(string textoPlano)
        {
            var chave = Convert.FromBase64String(opcoes.Value.ChaveCriptografia);
            var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
            var textoPlanoBytes = Encoding.UTF8.GetBytes(textoPlano);
            var textoCifrado = new byte[textoPlanoBytes.Length];
            var tag = new byte[AesGcm.TagByteSizes.MaxSize];

            using (var aes = new AesGcm(chave, tag.Length))
                aes.Encrypt(nonce, textoPlanoBytes, textoCifrado, tag);

            var resultado = new byte[nonce.Length + tag.Length + textoCifrado.Length];
            Buffer.BlockCopy(nonce, 0, resultado, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, resultado, nonce.Length, tag.Length);
            Buffer.BlockCopy(textoCifrado, 0, resultado, nonce.Length + tag.Length, textoCifrado.Length);

            return Convert.ToBase64String(resultado);
        }

        public string Decifrar(string textoCifradoBase64)
        {
            var chave = Convert.FromBase64String(opcoes.Value.ChaveCriptografia);
            var dados = Convert.FromBase64String(textoCifradoBase64);

            var tamanhoNonce = AesGcm.NonceByteSizes.MaxSize;
            var tamanhoTag = AesGcm.TagByteSizes.MaxSize;

            var nonce = dados[..tamanhoNonce];
            var tag = dados[tamanhoNonce..(tamanhoNonce + tamanhoTag)];
            var textoCifrado = dados[(tamanhoNonce + tamanhoTag)..];
            var textoPlano = new byte[textoCifrado.Length];

            using (var aes = new AesGcm(chave, tamanhoTag))
                aes.Decrypt(nonce, textoCifrado, tag, textoPlano);

            return Encoding.UTF8.GetString(textoPlano);
        }
    }
}
