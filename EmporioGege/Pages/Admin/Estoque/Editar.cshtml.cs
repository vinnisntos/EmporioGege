using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Interfaces;

namespace EmporioGege.Pages.Admin.Estoque
{
    [Authorize(Policy = "AdminOnly")]
    public class EditarModel(IProdutoService produtoService) : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public Guid? Id { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Informe o nome do produto.")]
        public string Nome { get; set; } = default!;

        [BindProperty]
        public string? CodigoBarras { get; set; }

        [BindProperty]
        [Range(0, double.MaxValue, ErrorMessage = "Custo não pode ser negativo.")]
        public decimal CustoMedio { get; set; }

        [BindProperty]
        [Range(0.01, double.MaxValue, ErrorMessage = "Informe um preço de venda maior que zero.")]
        public decimal PrecoVendaBase { get; set; }

        [BindProperty]
        [Range(0, int.MaxValue, ErrorMessage = "Estoque não pode ser negativo.")]
        public int EstoqueAtual { get; set; }

        [BindProperty]
        [Range(0, int.MaxValue, ErrorMessage = "Estoque mínimo não pode ser negativo.")]
        public int EstoqueMinimo { get; set; } = 5;

        [BindProperty]
        [Required]
        public string UnidadeMedida { get; set; } = "Un";

        [BindProperty]
        [Range(1, int.MaxValue, ErrorMessage = "Quantidade por caixa precisa ser pelo menos 1.")]
        public int QuantidadePorCaixa { get; set; } = 1;

        [BindProperty]
        [DataType(DataType.Date)]
        public DateOnly? DataValidade { get; set; }

        [BindProperty]
        [Range(0, double.MaxValue, ErrorMessage = "Preço da caixa não pode ser negativo.")]
        public decimal? PrecoCaixa { get; set; }

        [BindProperty]
        [Range(0, double.MaxValue, ErrorMessage = "Preço de atacado não pode ser negativo.")]
        public decimal? PrecoAtacado { get; set; }

        [TempData]
        public string? MensagemErro { get; set; }

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            if (Id is null)
                return Page();

            var produto = await produtoService.ObterAsync(Id.Value, ct);
            if (produto is null)
            {
                MensagemErro = "Produto não encontrado.";
                return RedirectToPage("Index");
            }

            Nome = produto.Nome;
            CodigoBarras = produto.CodigoBarras;
            CustoMedio = produto.CustoMedio;
            PrecoVendaBase = produto.PrecoVendaBase;
            EstoqueAtual = produto.EstoqueAtual;
            EstoqueMinimo = produto.EstoqueMinimo;
            UnidadeMedida = produto.UnidadeMedida;
            QuantidadePorCaixa = produto.QuantidadePorCaixa;
            DataValidade = produto.DataValidade;
            PrecoCaixa = produto.PrecoCaixa;
            PrecoAtacado = produto.PrecoAtacado;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return Page();

            var dto = new SalvarProdutoDto(
                Id, Nome, CodigoBarras, CustoMedio, PrecoVendaBase, EstoqueAtual, EstoqueMinimo,
                UnidadeMedida, QuantidadePorCaixa, DataValidade, PrecoCaixa, PrecoAtacado);

            await produtoService.SalvarAsync(dto, ct);

            return RedirectToPage("Index");
        }
    }
}
