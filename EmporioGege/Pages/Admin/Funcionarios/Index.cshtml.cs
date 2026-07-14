using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Interfaces;

namespace EmporioGege.Pages.Admin.Funcionarios
{
    [Authorize(Policy = "AdminOnly")]
    public class IndexModel(IFuncionarioService funcionarioService) : PageModel
    {
        public IReadOnlyList<FuncionarioDto> Funcionarios { get; private set; } = [];

        public async Task OnGetAsync(CancellationToken ct)
        {
            Funcionarios = await funcionarioService.ListarAsync(ct);
        }
    }
}
