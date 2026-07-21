namespace EmporioGege.Application.DTOs
{
    public record CriarClienteAsaasDto(string Nome, string? CpfCnpj, string? Email, string? Telefone);

    public record CriarClienteAsaasResultado(bool Sucesso, string? ClienteId, string? MensagemErro);

    public record CriarAssinaturaAsaasDto(
        string ClienteId, decimal Valor, string Ciclo, string TipoCobranca, string Descricao, DateOnly ProximoVencimento);

    public record CriarAssinaturaAsaasResultado(bool Sucesso, string? AssinaturaId, string? Status, string? MensagemErro);

    // Leitura: estado atual da assinatura da loja (pra tela SuperAdmin/Adegas/Editar).
    public record AssinaturaTenantDto(
        string? Plano, decimal? ValorMensalidade, string StatusLicenca, DateTime DataExpiracao,
        string? AsaasCustomerId, string? AsaasSubscriptionId);
}
