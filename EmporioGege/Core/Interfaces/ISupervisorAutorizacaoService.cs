namespace EmporioGege.Core.Interfaces
{
    // Validação pontual de credenciais de um administrador/superadmin para autorizar
    // uma ação sensível do caixa (ex.: cancelar comanda), sem afetar a sessão/cookie
    // do usuário atualmente logado.
    public interface ISupervisorAutorizacaoService
    {
        // solicitanteId identifica quem está TENTANDO a autorização (o vendedor logado no
        // terminal, não o supervisor sendo autenticado) - vem da claim de sessão já validada,
        // nunca de um campo de formulário, e existe só pra limitar tentativas por pessoa/terminal
        // (proteção de força bruta contra a senha de supervisor).
        Task<bool> AutorizarAsync(string solicitanteId, string email, string senha, CancellationToken ct = default);
    }
}
