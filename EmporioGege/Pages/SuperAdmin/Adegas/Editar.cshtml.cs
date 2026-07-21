using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Interfaces;

namespace EmporioGege.Pages.SuperAdmin.Adegas
{
    [Authorize(Policy = "SuperAdminOnly")]
    public class EditarModel(ITenantService tenantService, IFaturamentoService faturamentoService) : PageModel
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

        // Assinatura/cobrança (Asaas) - SEM [Required]/[Range] de propósito, e TODAS
        // declaradas nullable (string?) mesmo com um valor padrão sensato em mente: essa
        // página tem outro handler (OnPostAsync, acima) com ModelState.IsValid genérico, que
        // valida TODAS as propriedades bound da classe a cada POST, não só as do form/handler
        // chamado (mesma classe de bug já corrigida em Caixa/Turno - ver README.txt, bug #15).
        // Armadilha específica encontrada aqui: com Nullable Reference Types habilitado no
        // projeto, o ASP.NET Core torna qualquer "string" (não "string?") IMPLICITAMENTE
        // obrigatória pra validação de model, mesmo sem [Required] explícito - declarar
        // TipoCobranca como "string" (não nullable) fazia o formulário "Salvar" (que não
        // envia esse campo) falhar ModelState.IsValid silenciosamente, sem nenhuma mensagem
        // de erro na tela (o "Salvar" não seta MensagemErro no ramo de ModelState inválido).
        // Confirmado ao vivo: o CNPJ nunca era persistido, apesar da tela mostrar o valor
        // digitado (Razor re-exibe os valores postados mesmo quando Page() é retornado).
        [BindProperty]
        public string? Plano { get; set; }

        [BindProperty]
        public decimal ValorMensalidade { get; set; }

        [BindProperty]
        public string? TipoCobranca { get; set; }

        public AssinaturaTenantDto? Assinatura { get; set; }

        [TempData]
        public string? MensagemErro { get; set; }

        [TempData]
        public string? MensagemSucesso { get; set; }

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

            Assinatura = await faturamentoService.ObterAsync(Id.Value, ct);
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

        public async Task<IActionResult> OnPostCriarAssinaturaAsync(CancellationToken ct)
        {
            if (Id is null)
                return RedirectToPage("Index");

            if (string.IsNullOrWhiteSpace(Plano) || ValorMensalidade <= 0)
            {
                MensagemErro = "Selecione o plano e informe um valor mensal maior que zero.";
                return RedirectToPage(new { Id });
            }

            var resultado = await faturamentoService.CriarAssinaturaAsync(Id.Value, Plano, ValorMensalidade, TipoCobranca ?? "UNDEFINED", ct);

            MensagemErro = resultado.Sucesso ? null : resultado.MensagemErro;
            MensagemSucesso = resultado.Sucesso ? "Assinatura criada no Asaas com sucesso." : null;

            return RedirectToPage(new { Id });
        }
    }
}
