using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Interfaces;

namespace EmporioGege.Pages.Admin.Estoque
{
    [Authorize(Policy = "AdminOnly")]
    public class IndexModel(IProdutoService produtoService) : PageModel
    {
        public IReadOnlyList<ProdutoDetalheDto> Produtos { get; private set; } = [];

        [TempData]
        public string? MensagemSucesso { get; set; }

        [TempData]
        public string? MensagemErro { get; set; }

        public async Task OnGetAsync(CancellationToken ct)
        {
            Produtos = await produtoService.ListarAsync(ct);
        }

        public async Task<IActionResult> OnPostAlternarAtivoAsync(Guid id, bool ativoAtual, CancellationToken ct)
        {
            await produtoService.DefinirAtivoAsync(id, !ativoAtual, ct);
            MensagemSucesso = !ativoAtual ? "Produto reativado." : "Produto desativado.";
            return RedirectToPage();
        }
    }
}
