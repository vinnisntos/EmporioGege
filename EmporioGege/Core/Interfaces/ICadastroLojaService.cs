using EmporioGege.Application.DTOs;

namespace EmporioGege.Core.Interfaces
{
    // Orquestra o autocadastro público de uma loja nova: cria o tenant, o primeiro login
    // (administrador) via Supabase Auth, e a assinatura no Asaas - tudo sem nenhum tenant/
    // usuário autenticado ambiente (diferente de IFuncionarioService, que pressupõe um
    // administrador já logado criando um colega de trabalho DENTRO do próprio tenant dele).
    public interface ICadastroLojaService
    {
        Task<CadastroLojaResultado> CadastrarAsync(CadastrarLojaDto dto, CancellationToken ct = default);
    }
}
