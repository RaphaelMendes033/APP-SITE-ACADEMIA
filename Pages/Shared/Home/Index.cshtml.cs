using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;
using APP_SITE_ACADEMIA.Classes;

namespace APP_SITE_ACADEMIA.Pages.Shared.Home
{
    public class IndexModel : PageModel
    {
        public string NomeAluno { get; set; }
        public string Mensagem { get; set; }
        public string ApiKey { get; set; }
        public List<Treino> ListaTreinos { get; set; } = new List<Treino>();

        public async Task<IActionResult> OnGetAsync()
        {
            // 🔒 Verifica login
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Logado")))
                return RedirectToPage("/Shared/Login/Index");

            // Exemplo: recuperar nome do usuário logado
            var documento = HttpContext.Session.GetString("Documento");
            Mensagem = $"Bem-vindo! Documento: {documento}";

            return Page();
        }

       






    }





    public class Treino
    {
        public string DiaSemana { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public string ImagemBase64 { get; set; }
    }
}
