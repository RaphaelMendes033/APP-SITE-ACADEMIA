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

        // 🔹 Evento chamado via AJAX (quando o usuário sai do campo NomeBanco)
        public async Task<JsonResult> OnGetBuscarEmpresaAsync(string nomeBanco)
        {
            var bancoNuvem = new clsBancoNuvem();
            var resultado = await bancoNuvem.BuscarEmpresaAsync(nomeBanco);

            if (resultado.Sucesso)
                return new JsonResult(new { sucesso = true, nome = resultado.NomeEmpresa });
            else
                return new JsonResult(new { sucesso = false, erro = resultado.Erro });
        }

        // 🔹 Login principal (botão "Entrar")
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

                // 🔹 (Etapa 1) Valida a existência do banco antes de tentar logar
                var resultadoEmpresa = await bancoNuvem.BuscarEmpresaAsync(NomeBanco);
                if (!resultadoEmpresa.Sucesso)
                {
                    Mensagem = resultadoEmpresa.Erro;
                    return Page();
                }

                // 🔹 (Etapa 2) Faz login no banco da empresa
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
                Mensagem = $"❌ Erro de login: {ex.Message}";
                return Page();
            }
        }
    }
}
