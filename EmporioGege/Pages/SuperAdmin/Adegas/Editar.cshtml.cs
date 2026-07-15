using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Interfaces;

namespace EmporioGege.Pages.SuperAdmin.Adegas
{
    [Authorize(Policy = "SuperAdminOnly")]
    public class EditarModel(ITenantService tenantService) : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public Guid? Id { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Informe o nome fantasia.")]
        public string NomeFantasia { get; set; } = default!;

        [BindProperty]
        [Required(ErrorMessage = "Informe o nome do representante/dono.")]
        public string NomeRepresentante { get; set; } = default!;

        [BindProperty]
        [Required(ErrorMessage = "Informe o CPF/RG do dono.")]
        public string CpfRgDono { get; set; } = default!;

        [BindProperty]
        [Required(ErrorMessage = "Informe o CNPJ.")]
        public string Cnpj { get; set; } = default!;

        [BindProperty]
        [Required(ErrorMessage = "Informe cidade/UF.")]
        public string CidadeEstado { get; set; } = default!;

        [BindProperty]
        public string? TelefoneEmpresa { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Informe o telefone do dono.")]
        public string TelefoneDono { get; set; } = default!;

        [BindProperty]
        [EmailAddress(ErrorMessage = "E-mail da empresa inválido.")]
        public string? EmailEmpresa { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Informe o e-mail do dono.")]
        [EmailAddress(ErrorMessage = "E-mail do dono inválido.")]
        public string EmailDono { get; set; } = default!;

        [BindProperty]
        [Required]
        public string StatusLicenca { get; set; } = "ativo";

        [BindProperty]
        [Required(ErrorMessage = "Informe a data de expiração da licença.")]
        public DateTime DataExpiracao { get; set; } = DateTime.UtcNow.AddYears(1);

        [TempData]
        public string? MensagemErro { get; set; }

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            if (Id is null)
                return Page();

            var loja = await tenantService.ObterAsync(Id.Value, ct);
            if (loja is null)
            {
                MensagemErro = "Loja não encontrada.";
                return RedirectToPage("Index");
            }

            NomeFantasia = loja.NomeFantasia;
            NomeRepresentante = loja.NomeRepresentante;
            CpfRgDono = loja.CpfRgDono;
            Cnpj = loja.Cnpj;
            CidadeEstado = loja.CidadeEstado;
            TelefoneEmpresa = loja.TelefoneEmpresa;
            TelefoneDono = loja.TelefoneDono;
            EmailEmpresa = loja.EmailEmpresa;
            EmailDono = loja.EmailDono;
            StatusLicenca = loja.StatusLicenca;
            DataExpiracao = loja.DataExpiracao;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return Page();

            try
            {
                await tenantService.SalvarAsync(new SalvarTenantDto(
                    Id, NomeFantasia, NomeRepresentante, CpfRgDono, Cnpj, CidadeEstado,
                    TelefoneEmpresa, TelefoneDono, EmailEmpresa, EmailDono, StatusLicenca, DataExpiracao), ct);

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
