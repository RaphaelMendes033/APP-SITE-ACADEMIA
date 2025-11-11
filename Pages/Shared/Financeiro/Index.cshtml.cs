using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using APP_SITE_ACADEMIA.Classes;
using System;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace APP_Academia.Pages.Shared.Financeiro
{
    public class IndexModel : PageModel
    {
        // 🔹 Propriedades para exibição no HTML
        public string UltimoPagamentoValor { get; set; } = "R$ 0,00";
        public string UltimoPagamentoData { get; set; } = "-";
        public string TotalPago { get; set; } = "R$ 0,00";
        public string ProximoPagamentoValor { get; set; } = "R$ 0,00";
        public string ProximoPagamentoData { get; set; } = "-";

        public List<PagamentoInfo> Historico { get; set; } = new();

        [TempData]
        public string Mensagem { get; set; } = "";

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // 🔹 Recupera o código da pessoa logada
                if (TempData["CodigoPessoa"] == null)
                {
                    Mensagem = "Sessão expirada. Faça login novamente.";
                    return RedirectToPage("/Shared/Login/Index");
                }

                string codigoPessoa = TempData["CodigoPessoa"].ToString();
                TempData.Keep("CodigoPessoa");
                TempData.Keep("NomeAluno");

                var banco = new clsBancoNuvem();

                // 🔹 Consulta com INNER JOIN trazendo o nome do serviço
                string sql = $@"
    SELECT 
        p.DataVencimento,
        p.DataPagamento,
        p.ValorDevido,
        s.Nome AS NomeServico,
        p.FormaDePagamento
    FROM Pagamentos p
    INNER JOIN ServicosCadastrados sc 
        ON p.fk_ServicosCadastrado = sc.Codigo
    INNER JOIN Servicos s 
        ON sc.fk_Servico = s.Codigo
    WHERE p.fk_CodigoPessoa = '{codigoPessoa}'
    ORDER BY p.DataVencimento DESC";


                var tabela = await banco.ExecutarConsultaPublicaAsync(sql);

                if (tabela == null || tabela.Rows.Count == 0)
                {
                    Mensagem = "Nenhum pagamento encontrado.";
                    return Page();
                }

                decimal totalPago = 0;
                DateTime? ultimoPagamentoData = null;
                decimal ultimoPagamentoValor = 0;
                DateTime? proximoPagamentoData = null;
                decimal proximoPagamentoValor = 0;

                Historico.Clear();

                foreach (DataRow row in tabela.Rows)
                {
                    string dataVenc = row["DataVencimento"]?.ToString() ?? "";
                    string dataPag = row["DataPagamento"]?.ToString() ?? "";
                    string valorStr = row["ValorDevido"]?.ToString() ?? "0";
                    string servico = row["NomeServico"]?.ToString() ?? "-";

                    decimal valor = decimal.TryParse(valorStr, out var v) ? v : 0;
                    DateTime.TryParse(dataVenc, out DateTime dtVenc);
                    DateTime.TryParse(dataPag, out DateTime dtPag);

                    string status = string.IsNullOrEmpty(dataPag)
                        ? (DateTime.Now > dtVenc ? "Em Atraso" : "Pendente")
                        : "Pago";

                    // Histórico de pagamentos
                    Historico.Add(new PagamentoInfo
                    {
                        Data = dtVenc.ToString("dd/MM/yyyy"),
                        Servico = servico,
                        Valor = valor.ToString("C"),
                        Status = status
                    });

                    // Soma total pago
                    if (status == "Pago")
                    {
                        totalPago += valor;

                        // Atualiza último pagamento
                        if (ultimoPagamentoData == null || dtPag > ultimoPagamentoData)
                        {
                            ultimoPagamentoData = dtPag;
                            ultimoPagamentoValor = valor;
                        }
                    }
                    else if (status != "Pago" && proximoPagamentoData == null)
                    {
                        proximoPagamentoData = dtVenc;
                        proximoPagamentoValor = valor;
                    }
                }

                // 🔹 Preenche dados resumidos
                UltimoPagamentoValor = ultimoPagamentoValor.ToString("C");
                UltimoPagamentoData = ultimoPagamentoData?.ToString("dd/MM/yyyy") ?? "-";
                TotalPago = totalPago.ToString("C");
                ProximoPagamentoValor = proximoPagamentoValor.ToString("C");
                ProximoPagamentoData = proximoPagamentoData?.ToString("dd/MM/yyyy") ?? "-";

                return Page();
            }
            catch (Exception ex)
            {
                Mensagem = $"Erro ao carregar dados financeiros: {ex.Message}";
                return Page();
            }
        }
    }

    // 🔹 Classe auxiliar para exibir o histórico na View
    public class PagamentoInfo
    {
        public string Data { get; set; } = "";
        public string Servico { get; set; } = "";
        public string Valor { get; set; } = "";
        public string Status { get; set; } = "";
    }

    // 🔹 Extensão para acessar o método interno de consulta do clsBancoNuvem
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
