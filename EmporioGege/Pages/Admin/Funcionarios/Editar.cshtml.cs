using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Interfaces;

namespace EmporioGege.Pages.Admin.Funcionarios
{
    [Authorize(Policy = "AdminOnly")]
    public class EditarModel(IFuncionarioService funcionarioService) : PageModel
    {
        // De propósito, NÃO inclui "superadmin" — um administrador de loja não pode criar
        // nem promover ninguém a superadmin por essa tela (isso escalaria privilégio pra
        // fora do próprio tenant). Só o acesso direto ao banco cria um superadmin.
        private static readonly string[] RolesPermitidas = ["vendedor", "administrador"];

        [BindProperty(SupportsGet = true)]
        public Guid? Id { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Informe o nome.")]
        public string Nome { get; set; } = default!;

        [BindProperty]
        [Required(ErrorMessage = "Informe o e-mail.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        public string Email { get; set; } = default!;

        [BindProperty]
        public string? Senha { get; set; }

        [BindProperty]
        [Required]
        public string Role { get; set; } = "vendedor";

        [TempData]
        public string? MensagemErro { get; set; }

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            if (Id is null)
                return Page();

            var funcionario = (await funcionarioService.ListarAsync(ct)).FirstOrDefault(f => f.Id == Id);
            if (funcionario is null)
            {
                MensagemErro = "Funcionário não encontrado.";
                return RedirectToPage("Index");
            }

            Nome = funcionario.Nome;
            Email = funcionario.Email;
            Role = funcionario.Role;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            if (!RolesPermitidas.Contains(Role))
                ModelState.AddModelError(nameof(Role), "Selecione um cargo válido.");

            if (Id is null && string.IsNullOrWhiteSpace(Senha))
                ModelState.AddModelError(nameof(Senha), "Informe uma senha para o novo funcionário.");

            if (!ModelState.IsValid)
                return Page();

            try
            {
                if (Id is null)
                    await funcionarioService.CriarAsync(new CriarFuncionarioDto(Nome, Email, Senha!, Role), ct);
                else
                    await funcionarioService.AtualizarAsync(Id.Value, Nome, Role, ct);

                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                MensagemErro = ex.Message;
                return Page();
            }
        }
    }
}
