using APP_SITE_ACADEMIA.Classes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Threading.Tasks;
using static APP_SITE_ACADEMIA.Classes.clsBancoNuvem;

namespace APP_SITE_ACADEMIA.Pages.Shared.Login
{
    public class IndexModel : PageModel
    {
        [BindProperty] public string NomeBanco { get; set; }
        [BindProperty] public string Documento { get; set; }
        [BindProperty] public string Senha { get; set; }
        public string Mensagem { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NomeBanco) ||
                    string.IsNullOrWhiteSpace(Documento) ||
                    string.IsNullOrWhiteSpace(Senha))
                {
                    Mensagem = "❌ Preencha todos os campos.";
                    return Page();
                }

                var bancoNuvem = new clsBancoNuvem();
                string resultado = await bancoNuvem.FazerLoginAsync(NomeBanco, Documento, Senha);

                if (!bancoNuvem.Logado)
                {
                    Mensagem = "❌ Falha ao realizar login.";
                    return Page();
                }

                // ✅ Guarda sessão ASP.NET
                HttpContext.Session.SetString("Logado", "true");
                HttpContext.Session.SetString("Documento", Documento);
                HttpContext.Session.SetString("BancoEmpresa", NomeBanco);

                // ✅ Guarda também na classe estática global
                SessaoNuvem.BancoAtual = NomeBanco;
                SessaoNuvem.DocumentoUsuario = Documento;


                // ✅ Redireciona para Home
                return RedirectToPage("/Shared/Home/Index");
            }
            catch (Exception ex)
            {
                Mensagem = $"❌ Erro de login:  {ex.Message}";
                return Page();
            }
        }
    }
}
