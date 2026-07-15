using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EmporioGege.Application.DTOs;
using EmporioGege.Core.Interfaces;

namespace EmporioGege.Pages.SuperAdmin.Adegas
{
    [Authorize(Policy = "SuperAdminOnly")]
    public class IndexModel(ITenantService tenantService) : PageModel
    {
        public IReadOnlyList<TenantDto> Lojas { get; private set; } = [];

        [TempData]
        public string? MensagemSucesso { get; set; }

        [TempData]
        public string? MensagemErro { get; set; }

        public async Task OnGetAsync(CancellationToken ct)
        {
            Lojas = await tenantService.ListarAsync(ct);
        }

        // Grava ImpersonatedTenantId/Nome no cookie do próprio superadmin (não troca de
        // usuário nem afeta a sessão de ninguém mais) pra permitir usar as telas Admin/*
        // e Caixa/* de uma loja específica sem precisar de conta administrador nela.
        public async Task<IActionResult> OnPostEntrarAsync(Guid id, CancellationToken ct)
        {
            var loja = await tenantService.ObterAsync(id, ct);
            if (loja is null)
            {
                MensagemErro = "Loja não encontrada.";
                return RedirectToPage();
            }

            var claims = ClaimsSemImpersonacao()
                .Append(new Claim("ImpersonatedTenantId", loja.Id.ToString()))
                .Append(new Claim("ImpersonatedTenantNome", loja.NomeFantasia));

            await ReemitirCookieAsync(claims);
            return RedirectToPage("/Admin/Index");
        }

        // Chamado pelo banner "Modo Loja" em qualquer página do sistema (Pages/Shared/_Layout.cshtml).
        public async Task<IActionResult> OnPostSairAsync()
        {
            await ReemitirCookieAsync(ClaimsSemImpersonacao());
            return RedirectToPage();
        }

        private IEnumerable<Claim> ClaimsSemImpersonacao() =>
            User.Claims.Where(c => c.Type is not ("ImpersonatedTenantId" or "ImpersonatedTenantNome"));

        private async Task ReemitirCookieAsync(IEnumerable<Claim> claims)
        {
            var identidade = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identidade);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });
        }
    }
}
