using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace EmporioGege.Pages
{
    [AllowAnonymous] // Garante explicitamente que qualquer pessoa pode ver a página inicial
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Arquitetura de Segurança: Se o usuário já possui um cookie de sessão válido,
            // evita dupla exposição e o joga direto para a área segura correspondente à sua Role.
            if (User.Identity?.IsAuthenticated == true)
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                return role switch
                {
                    "superadmin" => RedirectToPage("/SuperAdmin/Index"),
                    "administrador" => RedirectToPage("/Admin/Index"),
                    "vendedor" => RedirectToPage("/Caixa/Index"),
                    _ => RedirectToPage("/Auth/AcessoNegado") // Fallback de segurança se a role for inválida
                };
            }

            return Page();
        }
    }
}