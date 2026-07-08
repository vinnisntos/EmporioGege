using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Supabase;
using System;
using System.Threading.Tasks;

namespace EmporioGege.Pages.Admin
{
    [Authorize(Policy = "AdminOnly")] // Camada de defesa 2: Proteção explícita no endpoint além da pasta
    public class IndexModel : PageModel
    {
        private readonly Client _supabaseClient;

        public IndexModel(Client supabaseClient)
        {
            _supabaseClient = supabaseClient;
        }

        // Propriedades fortemente tipadas para o painel de UX
        public decimal FaturamentoHoje { get; set; }
        public decimal FiadoPendenteTotal { get; set; }
        public int AlertasEstoqueCritico { get; set; }
        public int ComandasAtivasContador { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Engenharia de Software: Aqui faremos as chamadas agregadas ao Supabase.
                // Exemplo futuro: 
                // var response = await _supabaseClient.From<Venda>().Where(x => x.Data == DateTime.Today).Get();

                // Dados estáticos temporários para a interface não nascer vazia no primeiro build
                FaturamentoHoje = 1845.50m;
                FiadoPendenteTotal = 3420.00m;
                AlertasEstoqueCritico = 4;
                ComandasAtivasContador = 7;

                return Page();
            }
            catch (Exception)
            {
                // Cybersecurity: Nunca exponha a stack trace do banco de dados na tela do usuário.
                return RedirectToPage("/Error");
            }
        }
    }
}