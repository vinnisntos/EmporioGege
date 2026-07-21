using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Common;
using EmporioGege.Core.Interfaces;

namespace EmporioGege.Pages.Admin.ProdutosMaisVendidos
{
    [Authorize(Policy = "AdminOnly")]
    public class IndexModel(IDashboardService dashboardService, IRelatorioExportService exportService) : PageModel
    {
        private const int LimiteRanking = 20;

        [BindProperty(SupportsGet = true)]
        public DateOnly? De { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateOnly? Ate { get; set; }

        public IReadOnlyList<ProdutoMaisVendidoDto> Produtos { get; private set; } = [];

        public async Task OnGetAsync(CancellationToken ct)
        {
            Produtos = await CarregarAsync(ct);
        }

        public async Task<IActionResult> OnGetExportarXlsxAsync(CancellationToken ct)
        {
            var relatorio = await MontarRelatorioAsync(ct);
            var bytes = exportService.ExportarXlsx(relatorio);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "produtos-mais-vendidos.xlsx");
        }

        public async Task<IActionResult> OnGetExportarPdfAsync(CancellationToken ct)
        {
            var relatorio = await MontarRelatorioAsync(ct);
            var bytes = exportService.ExportarPdf(relatorio);
            return File(bytes, "application/pdf", "produtos-mais-vendidos.pdf");
        }

        public async Task<IActionResult> OnGetExportarXmlAsync(CancellationToken ct)
        {
            var relatorio = await MontarRelatorioAsync(ct);
            var bytes = exportService.ExportarXml(relatorio);
            return File(bytes, "application/xml", "produtos-mais-vendidos.xml");
        }

        public async Task<IActionResult> OnGetExportarCsvAsync(CancellationToken ct)
        {
            var relatorio = await MontarRelatorioAsync(ct);
            var bytes = exportService.ExportarCsv(relatorio);
            return File(bytes, "text/csv", "produtos-mais-vendidos.csv");
        }

        private async Task<IReadOnlyList<ProdutoMaisVendidoDto>> CarregarAsync(CancellationToken ct)
        {
            var hoje = FusoHorarioBrasil.HojeLocal();
            De ??= new DateOnly(hoje.Year, hoje.Month, 1);
            Ate ??= hoje;

            var inicio = FusoHorarioBrasil.InicioDoDiaLocalEmUtc(De.Value);
            var fimExclusivo = FusoHorarioBrasil.InicioDoDiaLocalEmUtc(Ate.Value.AddDays(1));

            return await dashboardService.ListarProdutosMaisVendidosAsync(inicio, fimExclusivo, LimiteRanking, ct);
        }

        private async Task<RelatorioTabularDto> MontarRelatorioAsync(CancellationToken ct)
        {
            var produtos = await CarregarAsync(ct);
            var cultura = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");

            var linhas = produtos
                .Select((p, i) => (IReadOnlyList<string>)
                [
                    (i + 1).ToString(),
                    p.Nome,
                    p.QuantidadeVendida.ToString(),
                    p.TotalVendido.ToString("C", cultura)
                ])
                .ToList();

            return new RelatorioTabularDto(
                $"Produtos Mais Vendidos ({De:dd/MM/yyyy} a {Ate:dd/MM/yyyy})",
                ["#", "Produto", "Quantidade Vendida", "Total Vendido"],
                linhas);
        }
    }
}
