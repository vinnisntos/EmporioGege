using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Common;
using EmporioGege.Core.Interfaces;

namespace EmporioGege.Pages.Admin
{
    [Authorize(Policy = "AdminOnly")] // Camada de defesa 2: Proteção explícita no endpoint além da pasta
    public class IndexModel(
        IDashboardService dashboardService, IFaturamentoService faturamentoService, ITenantProvider tenantProvider, ILogger<IndexModel> logger)
        : PageModel
    {
        // Propriedades fortemente tipadas para o painel de UX
        public decimal FaturamentoHoje { get; set; }
        public decimal FaturamentoSemana { get; set; }
        public decimal FiadoPendenteTotal { get; set; }
        public int AlertasEstoqueCritico { get; set; }
        public int ComandasAtivasContador { get; set; }

        public DashboardResumoDto ResumoMes { get; set; } = new(0, 0, 0, null, 0);
        public IReadOnlyList<ProdutoValidadeDto> ProdutosProximosValidade { get; set; } = [];

        // Widget de status de assinatura (trial/pendente/ativo) - null quando o tenant não tem
        // nenhuma assinatura/trial registrado (não deveria acontecer no fluxo normal, mas o
        // card só some da tela em vez de quebrar).
        public AssinaturaTenantDto? Assinatura { get; set; }
        public int DiasRestantesTrial { get; set; }

        [TempData]
        public string? MensagemErroAssinatura { get; set; }

        [TempData]
        public string? MensagemSucessoAssinatura { get; set; }

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            try
            {
                var hojeLocal = FusoHorarioBrasil.HojeLocal();
                var inicioHojeUtc = FusoHorarioBrasil.InicioDoDiaLocalEmUtc(hojeLocal);
                var inicioMes = FusoHorarioBrasil.InicioDoDiaLocalEmUtc(new DateOnly(hojeLocal.Year, hojeLocal.Month, 1));
                var proximoMes = inicioMes.AddMonths(1);

                var resumoHoje = await dashboardService.ObterResumoAsync(inicioHojeUtc, inicioHojeUtc.AddDays(1), ct);
                FaturamentoHoje = resumoHoje.Faturamento;

                var inicioSemana = FusoHorarioBrasil.InicioDoDiaLocalEmUtc(FusoHorarioBrasil.InicioDaSemanaLocal(hojeLocal));
                var resumoSemana = await dashboardService.ObterResumoAsync(inicioSemana, inicioHojeUtc.AddDays(1), ct);
                FaturamentoSemana = resumoSemana.Faturamento;

                ResumoMes = await dashboardService.ObterResumoAsync(inicioMes, proximoMes, ct);
                FiadoPendenteTotal = await dashboardService.ObterFiadoPendenteTotalAsync(ct);
                AlertasEstoqueCritico = await dashboardService.ContarProdutosEstoqueCriticoAsync(ct);
                ComandasAtivasContador = await dashboardService.ContarComandasAtivasAsync(ct);
                ProdutosProximosValidade = await dashboardService.ListarProdutosProximosValidadeAsync(30, ct);

                Assinatura = await faturamentoService.ObterAsync(tenantProvider.RequireTenantId(), ct);
                if (Assinatura is not null)
                    DiasRestantesTrial = (int)Math.Ceiling((Assinatura.DataExpiracao - DateTime.UtcNow).TotalDays);

                return Page();
            }
            catch (Exception ex)
            {
                // Cybersecurity: Nunca exponha a stack trace do banco de dados na tela do usuário
                // — mas logar o erro de verdade, senão qualquer falha aqui vira um /Error mudo
                // sem nenhum rastro pra investigar depois.
                logger.LogError(ex, "Falha ao carregar o dashboard administrativo.");
                return RedirectToPage("/Error");
            }
        }

        // Botão "Assinar agora" do card de trial - cria a assinatura Asaas de verdade (mesmo
        // caminho usado pelo job de trial vencido, ver TrialExpiradoProcessor) e já mostra o
        // link de pagamento assim que a página recarregar.
        public async Task<IActionResult> OnPostAssinarAgoraAsync(CancellationToken ct)
        {
            var resultado = await faturamentoService.IniciarCobrancaTrialAsync(tenantProvider.RequireTenantId(), ct);

            if (!resultado.Sucesso)
                MensagemErroAssinatura = resultado.MensagemErro ?? "Não foi possível iniciar sua assinatura. Tente novamente em instantes.";
            else
                MensagemSucessoAssinatura = "Assinatura criada! Use o link de pagamento abaixo pra concluir.";

            return RedirectToPage();
        }
    }
}
