using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Supabase;

namespace EmporioGege.Pages.SuperAdmin
{
    // Segurança máxima: Só entra quem tem a role superadmin definida na policy do Program.cs
    [Authorize(Policy = "SuperAdminOnly")]
    public class IndexModel : PageModel
    {
        private readonly Client _supabase;

        public IndexModel(Client supabase)
        {
            _supabase = supabase;
        }

        // Propriedades mockadas para renderizar o painel inicial
        public int TotalLojas { get; set; } = 2;
        public int TotalUsuarios { get; set; } = 8;
        public int LicencasAtivas { get; set; } = 2;
        public double FaturamentoGlobal { get; set; } = 12450.00;

        public async Task OnGetAsync()
        {
            try
            {
                // Engenharia de Software: Exemplo futuro de contagem dinâmica do Supabase
                // var response = await _supabase.From<Tenant>().Get();
                // TotalLojas = response.Models.Count;
            }
            catch (Exception)
            {
                // Silencioso para não crashar a página em modo dev caso o banco esteja instável
            }
        }
    }
}