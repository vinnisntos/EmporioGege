namespace EmporioGege.Application.DTOs
{
    // Leitura: dados fiscais da loja atual + status de habilitação de NFC-e. Nunca inclui
    // o token da Focus NFe (fica só cifrado no banco, nunca sai num DTO de leitura).
    public record ConfiguracaoFiscalDto(
        string? RazaoSocial, short? RegimeTributario, string? InscricaoEstadual,
        string? Logradouro, string? Numero, string? Bairro, string? Municipio, string? Uf, string? Cep,
        bool NfceHabilitada, string? FocusNfeEmpresaId);

    // Escrita: só os dados cadastrais (sem certificado, sem token) - o certificado é
    // recebido separadamente e nunca persistido (ver IConfiguracaoFiscalService).
    public record SalvarConfiguracaoFiscalDto(
        string RazaoSocial, short RegimeTributario, string? InscricaoEstadual,
        string Logradouro, string Numero, string Bairro, string Municipio, string Uf, string Cep);
}
