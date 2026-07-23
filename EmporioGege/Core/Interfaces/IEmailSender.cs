namespace EmporioGege.Core.Interfaces
{
    // Envio de e-mail transacional da própria aplicação (boas-vindas, instruções de
    // pagamento, confirmação de pagamento) - diferente do e-mail de confirmação de conta,
    // que é disparado automaticamente pelo Supabase Auth no SignUp e não passa por aqui.
    public interface IEmailSender
    {
        Task EnviarAsync(string destinatarioEmail, string destinatarioNome, string assunto, string corpoHtml, CancellationToken ct = default);
    }
}
