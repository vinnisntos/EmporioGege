using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Common;
using EmporioGege.Core.Interfaces;

namespace EmporioGege.Pages.Admin.Estoque
{
    [Authorize(Policy = "AdminOnly")]
    public class HistoricoModel(IEstoqueMovimentacaoService movimentacaoService, IRelatorioExportService exportService) : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public DateOnly? De { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateOnly? Ate { get; set; }

        public IReadOnlyList<MovimentacaoEstoqueDto> Movimentacoes { get; private set; } = [];

        public async Task OnGetAsync(CancellationToken ct)
        {
            Movimentacoes = await CarregarAsync(ct);
        }

        public async Task<IActionResult> OnGetExportarXlsxAsync(CancellationToken ct)
        {
            var relatorio = await MontarRelatorioAsync(ct);
            var bytes = exportService.ExportarXlsx(relatorio);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "historico-estoque.xlsx");
        }

        public async Task<IActionResult> OnGetExportarPdfAsync(CancellationToken ct)
        {
            var relatorio = await MontarRelatorioAsync(ct);
            var bytes = exportService.ExportarPdf(relatorio);
            return File(bytes, "application/pdf", "historico-estoque.pdf");
        }

        public async Task<IActionResult> OnGetExportarXmlAsync(CancellationToken ct)
        {
            var relatorio = await MontarRelatorioAsync(ct);
            var bytes = exportService.ExportarXml(relatorio);
            return File(bytes, "application/xml", "historico-estoque.xml");
        }

        public async Task<IActionResult> OnGetExportarCsvAsync(CancellationToken ct)
        {
            var relatorio = await MontarRelatorioAsync(ct);
            var bytes = exportService.ExportarCsv(relatorio);
            return File(bytes, "text/csv", "historico-estoque.csv");
        }

        private async Task<IReadOnlyList<MovimentacaoEstoqueDto>> CarregarAsync(CancellationToken ct)
        {
            var hoje = FusoHorarioBrasil.HojeLocal();
            De ??= new DateOnly(hoje.Year, hoje.Month, 1);
            Ate ??= hoje;

            var inicio = FusoHorarioBrasil.InicioDoDiaLocalEmUtc(De.Value);
            var fimExclusivo = FusoHorarioBrasil.InicioDoDiaLocalEmUtc(Ate.Value.AddDays(1));

            return await movimentacaoService.ListarHistoricoAsync(inicio, fimExclusivo, null, ct);
        }

        private async Task<RelatorioTabularDto> MontarRelatorioAsync(CancellationToken ct)
        {
            var movimentacoes = await CarregarAsync(ct);

            var linhas = movimentacoes
                .Select(m => (IReadOnlyList<string>)
                [
                    m.DataMovimento.ToString("dd/MM/yyyy HH:mm"),
                    m.ProdutoNome,
                    TraduzirTipo(m.TipoMovimentacao),
                    FormatarQuantidade(m.TipoMovimentacao, m.Quantidade),
                    m.Justificativa ?? "",
                    m.UsuarioNome ?? ""
                ])
                .ToList();

            return new RelatorioTabularDto(
                $"Histórico de Movimentação de Estoque ({De:dd/MM/yyyy} a {Ate:dd/MM/yyyy})",
                ["Data/Hora", "Produto", "Tipo", "Quantidade", "Justificativa", "Usuário"],
                linhas);
        }

        private static string TraduzirTipo(string tipo) => tipo switch
        {
            "ENTRADA_COMPRA" => "Entrada (Compra)",
            "SAIDA_VENDA" => "Saída (Venda)",
            "AJUSTE_MANUAL" => "Ajuste Manual",
            "SAIDA_QUEBRA" => "Saída (Quebra)",
            "SAIDA_DESCARTE" => "Saída (Descarte)",
            "SAIDA_USO_INSUMO" => "Saída (Uso como Insumo)",
            _ => tipo
        };

        // SAIDA_VENDA/SAIDA_QUEBRA/SAIDA_DESCARTE/SAIDA_USO_INSUMO gravam a quantidade retirada
        // como valor positivo, diferente de ENTRADA_COMPRA/AJUSTE_MANUAL onde o sinal já é o
        // delta real de estoque - sem isso, toda baixa aparecia como "+X" (aumento), igual uma entrada.
        internal static string FormatarQuantidade(string tipo, int quantidade)
        {
            if (tipo is "SAIDA_VENDA" or "SAIDA_QUEBRA" or "SAIDA_DESCARTE" or "SAIDA_USO_INSUMO")
                return $"-{quantidade}";

            return quantidade > 0 ? $"+{quantidade}" : quantidade.ToString();
        }
    }
}
