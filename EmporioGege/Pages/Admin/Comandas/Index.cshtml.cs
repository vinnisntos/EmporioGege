using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Exceptions;
using EmporioGege.Core.Interfaces;

namespace EmporioGege.Pages.Admin.Comandas
{
    [Authorize(Policy = "AdminOnly")]
    public class IndexModel(IComandaService comandaService) : PageModel
    {
        public IReadOnlyList<ComandaResumoDto> Comandas { get; private set; } = [];

        [TempData]
        public string? MensagemSucesso { get; set; }

        [TempData]
        public string? MensagemErro { get; set; }

        public async Task OnGetAsync(CancellationToken ct)
        {
            Comandas = await comandaService.ListarTodasAsync(ct);
        }

        // AdminOnly já garante que só administrador/superadmin chegam aqui — diferente do
        // caixa (Pages/Caixa/Comandas), não precisa de senha de supervisor pra cancelar.
        public async Task<JsonResult> OnGetDetalheAsync(Guid id, CancellationToken ct)
        {
            var detalhe = await comandaService.ObterDetalheAsync(id, ct);
            if (detalhe is null)
            {
                return new JsonResult(new { sucesso = false, mensagem = "Comanda não encontrada." })
                { StatusCode = StatusCodes.Status404NotFound };
            }

            return new JsonResult(new { sucesso = true, comanda = detalhe });
        }

        public async Task<IActionResult> OnPostCancelarAsync(Guid id, CancellationToken ct)
        {
            try
            {
                await comandaService.CancelarAsync(id, ct);
                MensagemSucesso = "Comanda cancelada.";
            }
            catch (ComandaInvalidaException ex)
            {
                MensagemErro = ex.Message;
            }

            return RedirectToPage();
        }
    }
}
