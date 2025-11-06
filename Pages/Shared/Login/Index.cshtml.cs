using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using APP_SITE_ACADEMIA.Classes;
using System.Threading.Tasks;

namespace APP_SITE_ACADEMIA.Pages.Shared.Login
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public string CodigoEmpresa { get; set; } = ""; // Número do banco (fk_CodigoNuvem)

        [BindProperty]
        public string Documento { get; set; } = ""; // CPF da pessoa

        [BindProperty]
        public string Senha { get; set; } = "";

        [TempData]
        public string Mensagem { get; set; } = "";

        public async Task<IActionResult> OnPostAsync()
        {
            var banco = new clsBancoNuvem();

            // ✅ Valida login diretamente na tabela Pessoas
            string nomePessoa = await banco.ValidarLoginPessoaAsync(CodigoEmpresa, Documento, Senha);

            if (!string.IsNullOrEmpty(nomePessoa))
            {
                TempData["NomeAluno"] = nomePessoa;
                return RedirectToPage("/Shared/Home/Index");
            }
            else
            {
                Mensagem = "Número do banco, CPF ou senha inválidos, ou conta bloqueada.";
                return Page();
            }
        }
    }
}
