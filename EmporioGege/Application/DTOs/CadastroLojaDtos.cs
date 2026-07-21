namespace EmporioGege.Application.DTOs
{
    // Sem ValorMensalidade aqui de propósito: o preço é resolvido no servidor a partir do
    // Plano (ver CadastroLojaService), nunca aceito de um campo do formulário público -
    // senão um visitante poderia mandar Plano=enterprise com um valor de centavos.
    public record CadastrarLojaDto(
        string NomeFantasia,
        string NomeRepresentante,
        string CpfRgDono,
        string Cnpj,
        string CidadeEstado,
        string TelefoneDono,
        string EmailDono,
        string Senha,
        string Plano,
        string TipoCobranca);

    public record CadastroLojaResultado(bool Sucesso, Guid? TenantId, string Mensagem, string? EtapaFalha);
}
