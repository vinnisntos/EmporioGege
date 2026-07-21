namespace EmporioGege.Application.DTOs
{
    public record CriarEmpresaFocusNfeDto(
        string RazaoSocial, string Cnpj, short RegimeTributario,
        string? InscricaoEstadual, string Logradouro, string Numero, string Bairro,
        string Municipio, string Uf, string Cep,
        byte[] CertificadoBytes, string SenhaCertificado);

    public record CriarEmpresaFocusNfeResultado(
        bool Sucesso, string? EmpresaId, string? TokenProducao, string? TokenHomologacao, string? MensagemErro);
}
