using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EmporioGege.Models;

namespace EmporioGege.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly Supabase.Client _supabase;

        public LoginModel(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        [BindProperty]
        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        public string Email { get; set; } = default!;

        [BindProperty]
        [Required(ErrorMessage = "A senha é obrigatória.")]
        [DataType(DataType.Password)]
        public string Senha { get; set; } = default!;

        public string? MensagemErro { get; set; }

        public void OnGet()
        {
            // Se o cara já estiver logado e tentar entrar no login, joga ele para fora
            if (User.Identity?.IsAuthenticated == true)
            {
                RedirecionarPorRole(User.FindFirst(ClaimTypes.Role)?.Value);
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            try
            {
                // 1. Tenta autenticar o usuário no Supabase Auth
                var sessao = await _supabase.Auth.SignIn(Email, Senha);

                if (sessao?.User == null)
                {
                    MensagemErro = "Usuário ou senha inválidos.";
                    return Page();
                }

                // 2. Busca o perfil do usuário na tabela pública para saber a Role e o TenantId
                var resultadoPerfil = await _supabase
                    .From<Profile>()
                    .Where(x => x.Id == sessao.User.Id)
                    .Single();

                if (resultadoPerfil == null)
                {
                    MensagemErro = "Perfil não encontrado no sistema.";
                    return Page();
                }

                // 3. Cria as credenciais (Claims) para salvar no Cookie do navegador de forma segura
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, resultadoPerfil.Id),
                    new Claim(ClaimTypes.Name, resultadoPerfil.Nome),
                    new Claim(ClaimTypes.Role, resultadoPerfil.Role),
                    new Claim("TenantId", resultadoPerfil.TenantId ?? "") // Se for null (SuperAdmin), fica vazio
                };

                var identidade = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identidade);

                // 4. Salva o cookie HttpOnly trancado no navegador do usuário
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
                {
                    IsPersistent = true, // Mantém logado mesmo se fechar o navegador (lembrar-me)
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) // Expira em 8 horas (um turno de trabalho)
                });

                // 5. Redireciona o cara para o painel correto dele
                return RedirecionarPorRole(resultadoPerfil.Role);
            }
            catch (Exception)
            {
                MensagemErro = "Erro ao tentar realizar o login. Verifique os dados ou a conexão.";
                return Page();
            }
        }

        private IActionResult RedirecionarPorRole(string? role)
        {
            return role switch
            {
                "superadmin" => RedirectToPage("/SuperAdmin/Index"),
                "administrador" => RedirectToPage("/Admin/Index"),
                _ => RedirectToPage("/Caixa/Index") // Vendedor / Caixa cai aqui
            };
        }
    }
}