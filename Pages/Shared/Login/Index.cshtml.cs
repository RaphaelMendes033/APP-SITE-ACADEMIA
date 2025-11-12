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

        // 🔹 Evento de envio do formulário (LOGIN)
        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var bancoPrincipal = new clsBancoNuvem();

                // 🔹 Faz login e obtém o nome do banco da empresa
                string nomeBancoEmpresa = await bancoPrincipal.ObterApiKeyDaNuvemAsync(Documento, Senha);

                // 🔹 Armazena informações na sessão
                HttpContext.Session.SetString("BancoEmpresa", nomeBancoEmpresa);
                HttpContext.Session.SetString("DocumentoUsuario", Documento);
                HttpContext.Session.SetString("Logado", "true");

                // ✅ Redireciona para a Home
                return RedirectToPage("/Shared/Home/Index");
            }
            catch (Exception ex)
            {
                Mensagem = "❌ Erro ao tentar realizar login: " + ex.Message;
                return Page();
            }
        }

        // 🔹 Teste de conexão (quando a página de login é aberta)
        public async Task OnGetAsync()
        {
            try
            {
                var bancoTeste = new clsBancoNuvem();
                string resultado = await bancoTeste.TestarConexaoAsync();
                Mensagem = resultado;
            }
            catch (Exception ex)
            {
                Mensagem = "❌ Erro ao testar conexão: " + ex.Message;
            }
        }
    }
}
