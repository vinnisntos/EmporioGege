using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Interfaces;

namespace EmporioGege.Pages.Admin.ConfiguracaoFiscal
{
    [Authorize(Policy = "AdminOnly")]
    public class IndexModel(IConfiguracaoFiscalService configuracaoFiscalService, ILogger<IndexModel> logger) : PageModel
    {
        [BindProperty]
        public string RazaoSocial { get; set; } = "";

        [BindProperty]
        public short RegimeTributario { get; set; }

        [BindProperty]
        public string? InscricaoEstadual { get; set; }

        [BindProperty]
        public string Logradouro { get; set; } = "";

        [BindProperty]
        public string Numero { get; set; } = "";

        [BindProperty]
        public string Bairro { get; set; } = "";

        [BindProperty]
        public string Municipio { get; set; } = "";

        [BindProperty]
        public string Uf { get; set; } = "";

        [BindProperty]
        public string Cep { get; set; } = "";

        [BindProperty]
        public IFormFile? Certificado { get; set; }

        [BindProperty]
        public string? SenhaCertificado { get; set; }

        public bool NfceHabilitada { get; set; }
        public string? FocusNfeEmpresaId { get; set; }
        public string? MensagemSucesso { get; set; }
        public string? MensagemErro { get; set; }

        public async Task OnGetAsync(CancellationToken ct) => await PreencherCamposAsync(ct);

        // Só cuida dos dados cadastrais (razão social, regime, endereço, IE) - nunca toca
        // no certificado nem no token. Validação manual (não ModelState.IsValid) de propósito:
        // essa página tem outro handler (OnPostRegistrarAsync) com campos [BindProperty]
        // próprios, e um ModelState.IsValid genérico validaria TODAS as propriedades bound
        // da classe a cada POST, não só as do form/handler chamado (mesma classe de bug já
        // corrigida em Caixa/Turno - ver README.txt, bug #15).
        public async Task<IActionResult> OnPostSalvarAsync(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(RazaoSocial) || RegimeTributario is < 1 or > 4
                || string.IsNullOrWhiteSpace(Logradouro) || string.IsNullOrWhiteSpace(Numero)
                || string.IsNullOrWhiteSpace(Bairro) || string.IsNullOrWhiteSpace(Municipio)
                || string.IsNullOrWhiteSpace(Uf) || string.IsNullOrWhiteSpace(Cep))
            {
                MensagemErro = "Preencha todos os campos obrigatórios (razão social, regime tributário e endereço completo).";
                await CarregarStatusAsync(ct);
                return Page();
            }

            await configuracaoFiscalService.SalvarDadosCadastraisAsync(new SalvarConfiguracaoFiscalDto(
                RazaoSocial, RegimeTributario, InscricaoEstadual, Logradouro, Numero, Bairro, Municipio, Uf, Cep), ct);

            MensagemSucesso = "Dados cadastrais salvos.";
            await CarregarStatusAsync(ct);
            return Page();
        }

        // Registra a loja na Focus NFe usando os dados cadastrais JÁ SALVOS (não os campos
        // deste POST) + o certificado enviado agora. Certificado/senha só existem em memória
        // até essa chamada - nunca são gravados no banco nem em disco (ver
        // ConfiguracaoFiscalService.RegistrarNaFocusNfeAsync).
        public async Task<IActionResult> OnPostRegistrarAsync(CancellationToken ct)
        {
            await PreencherCamposAsync(ct);

            if (Certificado is null || Certificado.Length == 0)
            {
                MensagemErro = "Selecione o arquivo do certificado digital (.pfx).";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(SenhaCertificado))
            {
                MensagemErro = "Informe a senha do certificado.";
                return Page();
            }

            byte[] certificadoBytes;
            using (var buffer = new MemoryStream())
            {
                await Certificado.CopyToAsync(buffer, ct);
                certificadoBytes = buffer.ToArray();
            }

            try
            {
                var resultado = await configuracaoFiscalService.RegistrarNaFocusNfeAsync(certificadoBytes, SenhaCertificado, ct);

                if (!resultado.Sucesso)
                    MensagemErro = resultado.MensagemErro ?? "Não foi possível registrar a loja na Focus NFe.";
                else
                    MensagemSucesso = "Loja registrada na Focus NFe com sucesso. NFC-e habilitada.";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao registrar a loja na Focus NFe.");
                MensagemErro = "Erro inesperado ao registrar na Focus NFe. Verifique os dados e tente novamente.";
            }

            await CarregarStatusAsync(ct);
            return Page();
        }

        private async Task PreencherCamposAsync(CancellationToken ct)
        {
            var dados = await configuracaoFiscalService.ObterAsync(ct);
            if (dados is null)
                return;

            RazaoSocial = dados.RazaoSocial ?? "";
            RegimeTributario = dados.RegimeTributario ?? 0;
            InscricaoEstadual = dados.InscricaoEstadual;
            Logradouro = dados.Logradouro ?? "";
            Numero = dados.Numero ?? "";
            Bairro = dados.Bairro ?? "";
            Municipio = dados.Municipio ?? "";
            Uf = dados.Uf ?? "";
            Cep = dados.Cep ?? "";
            NfceHabilitada = dados.NfceHabilitada;
            FocusNfeEmpresaId = dados.FocusNfeEmpresaId;
        }

        private async Task CarregarStatusAsync(CancellationToken ct)
        {
            var dados = await configuracaoFiscalService.ObterAsync(ct);
            NfceHabilitada = dados?.NfceHabilitada ?? false;
            FocusNfeEmpresaId = dados?.FocusNfeEmpresaId;
        }
    }
}
