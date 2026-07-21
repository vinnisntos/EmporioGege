using EmporioGege.Application.DTOs;

namespace EmporioGege.Core.Interfaces
{
    public interface IConfiguracaoFiscalService
    {
        Task<ConfiguracaoFiscalDto?> ObterAsync(CancellationToken ct = default);

        Task SalvarDadosCadastraisAsync(SalvarConfiguracaoFiscalDto dto, CancellationToken ct = default);

        // Registra a loja como empresa na Focus NFe usando os dados cadastrais JÁ SALVOS
        // (SalvarDadosCadastraisAsync precisa ter rodado antes) + o certificado recebido agora
        // - certificadoBytes/senhaCertificado nunca são persistidos, só passam em memória até
        // essa chamada. Se der certo, guarda o(s) token(ns) da empresa (cifrados) e marca
        // nfce_habilitada = true.
        Task<CriarEmpresaFocusNfeResultado> RegistrarNaFocusNfeAsync(byte[] certificadoBytes, string senhaCertificado, CancellationToken ct = default);
    }
}
