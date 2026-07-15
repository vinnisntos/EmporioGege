namespace EmporioGege.Core.Interfaces
{
    // Validação pontual de credenciais de um administrador/superadmin para autorizar
    // uma ação sensível do caixa (ex.: cancelar comanda), sem afetar a sessão/cookie
    // do usuário atualmente logado.
    public interface ISupervisorAutorizacaoService
    {
        Task<bool> AutorizarAsync(string email, string senha, CancellationToken ct = default);
    }
}
