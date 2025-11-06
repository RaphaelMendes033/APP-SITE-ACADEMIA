using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using APP_SITE_ACADEMIA.Classes;
using System.Threading.Tasks;

namespace APP_SITE_ACADEMIA.Pages.Shared.Login
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public string CodigoEmpresa { get; set; } = "";

        [BindProperty]
        public string Senha { get; set; } = "";

        [TempData]
        public string Mensagem { get; set; } = "";

        public async Task<IActionResult> OnPostAsync()
        {
            var banco = new clsBancoNuvem();

            string nomePessoa = await banco.ValidarLoginPessoaAsync(CodigoEmpresa, Senha);

            if (!string.IsNullOrEmpty(nomePessoa))
            {
                // ✅ Guarda o nome para a próxima página
                TempData["NomeAluno"] = nomePessoa;

                // Redireciona para a home
                return RedirectToPage("/Shared/Home/Index");
            }
            else
            {
                Mensagem = "Código da empresa ou senha inválidos.";
                return Page();
            }
        }
    }
}
