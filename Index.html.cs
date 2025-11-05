using APP_SITE_ACADEMIA.Classes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace APP_SITE_ACADEMIA.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public string Username { get; set; } = "";

        [BindProperty]
        public string Password { get; set; } = "";

        [TempData]
        public string Mensagem { get; set; } = "";

        public async Task<IActionResult> OnPostAsync()
        {
            var banco = new clsBancoNuvem();
            string nomeEmpresa = await banco.ObterNomeEmpresaAsync(Username, Password);

            if (!string.IsNullOrEmpty(nomeEmpresa))
                Mensagem = $"✅ Empresa encontrada: {nomeEmpresa}";
            else
                Mensagem = "⚠️ CNPJ ou código inválidos.";

            return RedirectToPage(); // redireciona, preservando TempData
        }
    }
}
