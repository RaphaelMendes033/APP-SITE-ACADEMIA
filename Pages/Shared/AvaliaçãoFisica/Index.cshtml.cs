//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;
//using APP_SITE_ACADEMIA.Classes;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Threading.Tasks;

//namespace APP_SITE_ACADEMIA.Pages.Shared.AvaliacaoFisica
//{
//    public class IndexModel : PageModel
//    {
//        [BindProperty] public double Peso { get; set; }
//        [BindProperty] public double Altura { get; set; }

//        public double IMC { get; set; }
//        public string Classificacao { get; set; } = "";

//        public List<IMCInfo> ListaIMC { get; set; } = new();
//        public List<AvaliacaoFisicaInfo> ListaAvaliacoes { get; set; } = new();

//        [TempData]
//        public string Mensagem { get; set; }

//        public async Task<IActionResult> OnGetAsync()
//        {
//            try
//            {
//                if (TempData["CodigoPessoa"] == null)
//                {
//                    Mensagem = "Sessão expirada. Faça login novamente.";
//                    return RedirectToPage("/Shared/Login/Index");
//                }

//                string codigoPessoa = TempData["CodigoPessoa"].ToString();
//                TempData.Keep("CodigoPessoa");
//                TempData.Keep("NomeAluno");

//                var banco = new clsBancoNuvem();

//                // ?? Consulta IMC filtrada pela pessoa logada
//                string sqlIMC = $@"
//                    SELECT Peso, Altura, Resultado, Data
//                    FROM IMC
//                    WHERE fk_Pessoa = '{codigoPessoa}'
//                    ORDER BY Data DESC";

//                var tabelaIMC = await banco.ExecutarConsultaPublicaAsync(sqlIMC);
//                ListaIMC.Clear();

//                if (tabelaIMC != null && tabelaIMC.Rows.Count > 0)
//                {
//                    foreach (DataRow row in tabelaIMC.Rows)
//                    {
//                        ListaIMC.Add(new IMCInfo
//                        {
//                            Peso = row["Peso"]?.ToString(),
//                            Altura = row["Altura"]?.ToString(),
//                            Resultado = row["Resultado"]?.ToString(),
//                            Data = row["Data"]?.ToString()
//                        });
//                    }
//                }

//                // ?? Consulta Avaliação Física filtrada pela pessoa logada
//                string sqlAvaliacao = $@"
//                    SELECT Data, BracoDireito, BracoEsquerdo, Cintura, Abdomen, 
//                           CoxaDireita, CoxaEsquerda, PanturrilhaDireita, PanturrilhaEsquerda, PressaoArterial
//                    FROM AvaliacaoFisica
//                    WHERE fk_Pessoa = '{codigoPessoa}'
//                    ORDER BY Data DESC";

//                var tabelaAvaliacao = await banco.ExecutarConsultaPublicaAsync(sqlAvaliacao);
//                ListaAvaliacoes.Clear();

//                if (tabelaAvaliacao != null && tabelaAvaliacao.Rows.Count > 0)
//                {
//                    foreach (DataRow row in tabelaAvaliacao.Rows)
//                    {
//                        ListaAvaliacoes.Add(new AvaliacaoFisicaInfo
//                        {
//                            Data = row["Data"]?.ToString(),
//                            BracoDireito = row["BracoDireito"]?.ToString(),
//                            BracoEsquerdo = row["BracoEsquerdo"]?.ToString(),
//                            Cintura = row["Cintura"]?.ToString(),
//                            Abdomen = row["Abdomen"]?.ToString(),
//                            CoxaDireita = row["CoxaDireita"]?.ToString(),
//                            CoxaEsquerda = row["CoxaEsquerda"]?.ToString(),
//                            PanturrilhaDireita = row["PanturrilhaDireita"]?.ToString(),
//                            PanturrilhaEsquerda = row["PanturrilhaEsquerda"]?.ToString(),
//                            PressaoArterial = row["PressaoArterial"]?.ToString()
//                        });
//                    }
//                }

//                return Page();
//            }
//            catch (Exception ex)
//            {
//                Mensagem = $"Erro ao carregar dados de avaliação: {ex.Message}";
//                return Page();
//            }
//        }

//        // ? Cálculo local de IMC
//        public void OnPost()
//        {
//            if (Altura > 0)
//            {
//                IMC = Math.Round(Peso / (Altura * Altura), 2);
//                Classificacao = ObterClassificacao(IMC);
//            }
//        }

//        private string ObterClassificacao(double imc)
//        {
//            if (imc < 18.5) return "Abaixo do peso";
//            if (imc < 25) return "Peso normal";
//            if (imc < 30) return "Sobrepeso";
//            if (imc < 35) return "Obesidade Grau I";
//            if (imc < 40) return "Obesidade Grau II";
//            return "Obesidade Grau III (mórbida)";
//        }
//    }

//    // ? Classe auxiliar para tabela IMC
//    public class IMCInfo
//    {
//        public string Peso { get; set; }
//        public string Altura { get; set; }
//        public string Resultado { get; set; }
//        public string Data { get; set; }
//    }

//    // ? Classe auxiliar para tabela Avaliação Física
//    public class AvaliacaoFisicaInfo
//    {
//        public string Data { get; set; }
//        public string BracoDireito { get; set; }
//        public string BracoEsquerdo { get; set; }
//        public string Cintura { get; set; }
//        public string Abdomen { get; set; }
//        public string CoxaDireita { get; set; }
//        public string CoxaEsquerda { get; set; }
//        public string PanturrilhaDireita { get; set; }
//        public string PanturrilhaEsquerda { get; set; }
//        public string PressaoArterial { get; set; }
//    }

//    // ? Extensão que permite acessar ExecutarConsultaAsync de clsBancoNuvem
//    public static class BancoNuvemExtensions
//    {
//        public static async Task<DataTable> ExecutarConsultaPublicaAsync(this clsBancoNuvem banco, string sql)
//        {
//            var method = typeof(clsBancoNuvem).GetMethod("ExecutarConsultaAsync",
//                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

//            if (method == null)
//                throw new Exception("Método 'ExecutarConsultaAsync' não encontrado dentro de clsBancoNuvem.");

//            var task = (Task<DataTable>)method.Invoke(banco, new object[] { sql });
//            return await task;
//        }
//    }
//}
