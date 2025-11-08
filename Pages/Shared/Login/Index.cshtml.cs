using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using APP_SITE_ACADEMIA.Classes;
using System.Threading.Tasks;
using System.Data;

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

            // 🔹 Consulta login direto na tabela Pessoas
            string sql = $@"
                SELECT Codigo, Nome 
                FROM Pessoas 
                WHERE Documento = '{Documento}' 
                AND Senha = '{Senha}' 
                AND fk_CodigoNuvem = '{CodigoEmpresa}' 
                AND (Ativo = 1 OR Ativo IS NULL)
                LIMIT 1";

            var tabela = await banco.ExecutarConsultaPublicaAsync(sql);

            if (tabela == null || tabela.Rows.Count == 0)
            {
                Mensagem = "Usuário ou senha inválido.";
                return Page();
            }

            // 🔹 Captura os dados do aluno
            var row = tabela.Rows[0];
            string codigoPessoa = row["Codigo"].ToString();
            string nomePessoa = row["Nome"].ToString();

            // 🔹 Salva no TempData para o Home recuperar
            TempData["CodigoPessoa"] = codigoPessoa;
            TempData["NomeAluno"] = nomePessoa;

            // 🔹 Redireciona para a Home
            return RedirectToPage("/Shared/Home/Index");
        }
    }

    // Extensão auxiliar (mesmo que no Home)
    public static class BancoNuvemExtensions
    {
        public static async Task<DataTable> ExecutarConsultaPublicaAsync(this clsBancoNuvem banco, string sql)
        {
            var method = typeof(clsBancoNuvem).GetMethod("ExecutarConsultaAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var task = (Task<DataTable>)method.Invoke(banco, new object[] { sql });
            return await task;
        }
    }
}
