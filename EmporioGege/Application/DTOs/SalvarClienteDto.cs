namespace EmporioGege.Application.DTOs
{
    // Id nulo = criar cliente novo; Id preenchido = editar existente. saldo_devedor não é
    // editável por aqui de propósito — só muda via venda fiado ou RegistrarPagamentoAsync.
    public record SalvarClienteDto(Guid? Id, string Nome, string? Telefone, string CpfRg, decimal LimiteCredito);
}
