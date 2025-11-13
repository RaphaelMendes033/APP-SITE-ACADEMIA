using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using APP_SITE_ACADEMIA.Classes;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace APP_SITE_ACADEMIA.Pages.Shared.Login
{
    public class IndexModel : PageModel
    {
        [BindProperty] public string Documento { get; set; }
        [BindProperty] public string Senha { get; set; }
        public string Mensagem { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // 🔹 Validação inicial
                if (string.IsNullOrWhiteSpace(Documento) || string.IsNullOrWhiteSpace(Senha))
                {
                    Mensagem = "❌ Informe o documento e a senha.";
                    return Page();
                }

                var bancoNuvem = new clsBancoNuvem();
                string resultado = await bancoNuvem.ObterApiKeyDaNuvemAsync(Documento, Senha);

                if (resultado.Contains("Usuário não encontrado"))
                {
                    Mensagem = "❌ Usuário não encontrado.";
                    return Page();
                }
                else if (resultado.Contains("Usuário bloqueado"))
                {
                    Mensagem = "❌ Usuário bloqueado.";
                    return Page();
                }
                else if (resultado.Contains("Erro ao consultar"))
                {
                    Mensagem = "❌ Erro ao consultar a nuvem.";
                    return Page();
                }

                // ✅ Login bem-sucedido → grava sessão
                HttpContext.Session.SetString("Logado", "true");
                HttpContext.Session.SetString("Documento", Documento);
                HttpContext.Session.SetString("ApiKey", resultado);

                // ✅ Redireciona corretamente para Home
                return RedirectToPage("/Shared/Home/Index");
            }
            catch (Exception ex)
            {
                Mensagem = $"❌ Erro ao tentar realizar login: {ex.Message}";
                return Page();
            }
        }
    }
}
