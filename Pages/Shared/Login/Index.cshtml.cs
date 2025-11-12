using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using APP_SITE_ACADEMIA.Classes;
using System;
using System.Threading.Tasks;

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

                // 🔹 Valida login e obtém API Key da empresa
                string apiKeyEmpresa = await bancoPrincipal.ObterApiKeyDaNuvemAsync(Documento, Senha);

                // 🔹 Armazena informações temporárias para a Home
                TempData["APIkeyEmpresa"] = apiKeyEmpresa;
                TempData["NomeAluno"] = Documento;
                TempData["CodigoPessoa"] = "1";

                // ✅ Redireciona e envia também via querystring
                return RedirectToPage("/Shared/Home/Index", new { api = apiKeyEmpresa, nome = Documento });
            }
            catch (Exception ex)
            {
                Mensagem = "❌ Erro ao tentar realizar login: " + ex.Message;
                return Page();
            }
        }

        // 🔹 Teste de conexão (chamado quando a página de login é aberta)
        public async Task OnGetAsync()
        {
            try
            {
                var bancoTeste = new clsBancoNuvem();
                bool conectado = await bancoTeste.TestarConexaoAsync();

                Mensagem = conectado
                    ? "✅ Conexão com o banco principal estabelecida com sucesso!"
                    : "❌ Falha ao conectar com o banco principal.";
            }
            catch (Exception ex)
            {
                Mensagem = "❌ Erro ao testar conexão: " + ex.Message;
            }
        }
    }
}
