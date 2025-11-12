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

        public async Task<IActionResult> OnGetAsync(string api, string nome)
        {
            // ✅ Recupera dados do login
            ApiKey = api ?? TempData["APIkeyEmpresa"]?.ToString();
            NomeAluno = nome ?? TempData["NomeAluno"]?.ToString();

            if (string.IsNullOrEmpty(ApiKey) || string.IsNullOrEmpty(NomeAluno))
            {
                // ⚠️ Caso falte algo, volta pro login
                return Redirect("~/Shared/Login/Index");
            }

            try
            {
                // 🔹 Aqui você poderá buscar os treinos do aluno usando ApiKey
                // var banco = new clsBancoNuvem();
                // ListaTreinos = await banco.ObterTreinosDoAlunoAsync(ApiKey, NomeAluno);

                // if (ListaTreinos == null || ListaTreinos.Count == 0)
                //     Mensagem = "⚠️ Nenhum treino encontrado para este aluno.";

                Mensagem = "✅ Login realizado com sucesso! API conectada.";
            }
            catch (System.Exception ex)
            {
                Mensagem = "❌ Erro ao carregar treinos: " + ex.Message;
            }

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
