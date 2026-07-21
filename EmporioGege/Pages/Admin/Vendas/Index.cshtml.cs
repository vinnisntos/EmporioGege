using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Common;
using EmporioGege.Core.Interfaces;

namespace EmporioGege.Pages.Admin.Vendas
{
    [Authorize(Policy = "AdminOnly")]
    public class IndexModel(IVendaService vendaService, ITenantService tenantService, ITenantProvider tenantProvider, IImpressoraReciboService impressoraReciboService) : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public DateOnly? De { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateOnly? Ate { get; set; }

        public IReadOnlyList<VendaResumoDto> Vendas { get; private set; } = [];

        public decimal TotalPeriodo { get; private set; }

        public async Task OnGetAsync(CancellationToken ct)
        {
            var hoje = FusoHorarioBrasil.HojeLocal();
            De ??= hoje.AddDays(-6);
            Ate ??= hoje;

            // [inicio, fimExclusivo) - mesma convenção de intervalo usada no DashboardService.
            // De/Ate são datas escolhidas pelo usuário no fuso local (loja no Brasil) - convertê-las
            // pra UTC direto (DateTimeKind.Utc) sem ajustar o offset causava um descasamento de até
            // 3h com a exibição (que usa .ToLocalTime()): uma venda das 23h37 local (14/07) já
            // tinha virado dia 15 em UTC, e aparecia dentro de um filtro "De 15/07".
            var inicio = FusoHorarioBrasil.InicioDoDiaLocalEmUtc(De.Value);
            var fimExclusivo = FusoHorarioBrasil.InicioDoDiaLocalEmUtc(Ate.Value.AddDays(1));

            Vendas = await vendaService.ListarAsync(inicio, fimExclusivo, ct);
            TotalPeriodo = Vendas.Sum(v => v.TotalVenda);
        }

        public async Task<JsonResult> OnGetDetalheAsync(Guid id, CancellationToken ct)
        {
            var detalhe = await vendaService.ObterDetalheAsync(id, ct);
            if (detalhe is null)
            {
                return new JsonResult(new { sucesso = false, mensagem = "Venda não encontrada." })
                { StatusCode = StatusCodes.Status404NotFound };
            }

            return new JsonResult(new { sucesso = true, venda = detalhe });
        }

        // Reimpressão manual: lê a venda já persistida (diferente da impressão automática no
        // fechamento, que usa os dados já em mãos da transação recém-concluída) e manda pra
        // mesma impressora térmica configurada, com o mesmo formato ESC/POS.
        public async Task<JsonResult> OnPostReimprimirAsync(Guid id, CancellationToken ct)
        {
            var detalhe = await vendaService.ObterDetalheAsync(id, ct);
            if (detalhe is null)
            {
                return new JsonResult(new { sucesso = false, mensagem = "Venda não encontrada." })
                { StatusCode = StatusCodes.Status404NotFound };
            }

            var loja = await tenantService.ObterAsync(tenantProvider.RequireTenantId(), ct);
            var itensRecibo = detalhe.Itens
                .Select(i => new ItemReciboDto(i.ProdutoNome, i.Quantidade, i.PrecoUnitarioAplicado, i.Subtotal))
                .ToList();

            var recibo = new ReciboVendaDto(
                loja?.NomeFantasia ?? "Empório Gege", detalhe.Id, detalhe.DataVenda, itensRecibo,
                detalhe.TotalVenda, detalhe.MetodoPagamento ?? "-", detalhe.NumeroComanda);

            var reciboImpresso = await impressoraReciboService.ImprimirAsync(recibo, ct);
            return new JsonResult(new { sucesso = true, reciboImpresso });
        }
    }
}
