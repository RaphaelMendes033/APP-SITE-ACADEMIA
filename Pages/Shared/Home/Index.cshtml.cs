using Microsoft.AspNetCore.Mvc.RazorPages;
using APP_SITE_ACADEMIA.Classes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APP_SITE_ACADEMIA.Pages.Shared.Home
{
    public class IndexModel : PageModel
    {
        public string NomeAluno { get; set; } = "Aluno";
        public List<TreinoInfo> ListaTreinos { get; set; } = new();

        public async Task OnGetAsync()
        {
            // ✅ Recupera dados do aluno logado
            NomeAluno = TempData["NomeAluno"]?.ToString() ?? "Aluno";
            var codigoPessoa = TempData["CodigoPessoa"]?.ToString();

            if (string.IsNullOrEmpty(codigoPessoa))
            {
                // Se não estiver logado, redireciona para o login
                Response.Redirect("/Shared/Login/Index");
                return;
            }

            var banco = new clsBancoNuvem();

            // 🔹 Consulta combinando as tabelas TreinosCadastrados e Treinos
            string sql = $@"
                SELECT 
                    T.Nome, 
                    T.Descricao, 
                    T.Imagem, 
                    TC.Dia
                FROM TreinosCadastrados TC
                INNER JOIN Treinos T ON T.Codigo = TC.fk_Treino
                WHERE TC.fk_Pessoa = '{codigoPessoa}'
                ORDER BY 
                    CASE 
                        WHEN TC.Dia = 'Segunda' THEN 1
                        WHEN TC.Dia = 'Terça' THEN 2
                        WHEN TC.Dia = 'Quarta' THEN 3
                        WHEN TC.Dia = 'Quinta' THEN 4
                        WHEN TC.Dia = 'Sexta' THEN 5
                        WHEN TC.Dia = 'Sábado' THEN 6
                        WHEN TC.Dia = 'Domingo' THEN 7
                    END";

            var tabela = await banco.ExecutarConsultaPublicaAsync(sql);

            if (tabela != null && tabela.Rows.Count > 0)
            {
                foreach (System.Data.DataRow row in tabela.Rows)
                {
                    byte[] imagemBytes = null;

                    if (row["Imagem"] != DBNull.Value)
                    {
                        try
                        {
                            // Se o retorno for texto base64 ou blob, converte corretamente
                            if (row["Imagem"] is byte[] bytes)
                                imagemBytes = bytes;
                            else
                            {
                                var valor = row["Imagem"].ToString();
                                if (!string.IsNullOrEmpty(valor))
                                    imagemBytes = Convert.FromBase64String(valor);
                            }
                        }
                        catch
                        {
                            imagemBytes = null;
                        }
                    }

                    ListaTreinos.Add(new TreinoInfo
                    {
                        Nome = row["Nome"]?.ToString(),
                        Descricao = row["Descricao"]?.ToString(),
                        ImagemBase64 = imagemBytes != null ? Convert.ToBase64String(imagemBytes) : null,
                        DiaSemana = row["Dia"]?.ToString()
                    });
                }
            }

            // 🔹 Mantém TempData disponível após OnGet
            TempData.Keep();
        }

        public class TreinoInfo
        {
            public string Nome { get; set; }
            public string Descricao { get; set; }
            public string DiaSemana { get; set; }
            public string ImagemBase64 { get; set; }
        }
    }

    // 🔹 Extensão para reaproveitar o método privado ExecutarConsultaAsync da clsBancoNuvem
    public static class BancoNuvemExtensions
    {
        public static async Task<System.Data.DataTable> ExecutarConsultaPublicaAsync(this clsBancoNuvem banco, string sql)
        {
            var method = typeof(clsBancoNuvem).GetMethod("ExecutarConsultaAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var task = (Task<System.Data.DataTable>)method.Invoke(banco, new object[] { sql });
            return await task;
        }
    }
}
