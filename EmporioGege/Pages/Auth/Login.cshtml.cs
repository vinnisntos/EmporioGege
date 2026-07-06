using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EmporioGege.Pages.Auth
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Senha { get; set; } = string.Empty;

        public void OnGet()
        {
            // Carrega a tela
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Mock de teste (depois a gente bota o Supabase aqui)
            if (Email == "admin@adega.com" && Senha == "123")
            {
                return RedirectToPage("/PDV/Turno");
            }

            ModelState.AddModelError(string.Empty, "E-mail ou senha incorretos.");
            return Page();
        }
    }
}