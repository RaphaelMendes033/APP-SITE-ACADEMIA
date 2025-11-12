using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using APP_SITE_ACADEMIA.Classes;
using System;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APP_Academia.Pages.Shared.Financeiro
{
    public class IndexModel : PageModel
    {
        // 🔹 Propriedades para exibição
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
                // 🔹 Recupera sessão do login
                string apiKey = HttpContext.Session.GetString("APIkeyEmpresa");
                string codigoPessoa = HttpContext.Session.GetString("CodigoPessoa");

                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(codigoPessoa))
                {
                    Mensagem = "Sessão expirada. Faça login novamente.";
                    return Redirect("~/Shared/Login/Index");
                }

                // 🔹 Faz conexão com o banco
                var banco = new clsBancoNuvem("https://cflv2aczvz.g2.sqlite.cloud/v2/weblite/sql", apiKey, "SGA");


                string sql = $@"
                    SELECT 
                        p.DataVencimento,
                        p.DataPagamento,
                        p.ValorDevido,
                        s.Nome AS NomeServico
                    FROM Pagamentos p
                    INNER JOIN ServicosCadastrados sc ON p.fk_ServicosCadastrado = sc.Codigo
                    INNER JOIN Servicos s ON sc.fk_Servico = s.Codigo
                    WHERE p.fk_CodigoPessoa = '{codigoPessoa}'
                    ORDER BY p.DataVencimento DESC
                ";

                DataTable tabela = null;

                try
                {
                    tabela = await banco.ExecutarConsultaPublicaAsync(sql);
                }
                catch
                {
                    // 🔹 Se o banco ou tabelas ainda não existem
                    Mensagem = "⚠️ Nenhum dado disponível no momento.";
                    Historico = new List<PagamentoInfo>
                    {
                        new PagamentoInfo
                        {
                            Data = DateTime.Now.ToString("dd/MM/yyyy"),
                            Servico = "Sem dados no banco",
                            Valor = "R$ 0,00",
                            Status = "Pendente"
                        }
                    };
                    return Page();
                }

                if (tabela == null || tabela.Rows.Count == 0)
                {
                    Mensagem = "⚠️ Nenhum pagamento encontrado.";
                    return Page();
                }

                // 🔹 Monta histórico normalmente
                decimal totalPago = 0;
                DateTime? ultimoPagData = null;
                decimal ultimoPagValor = 0;
                DateTime? proxPagData = null;
                decimal proxPagValor = 0;

                foreach (DataRow row in tabela.Rows)
                {
                    string dataVenc = row["DataVencimento"]?.ToString() ?? "";
                    string dataPag = row["DataPagamento"]?.ToString() ?? "";
                    string valorStr = row["ValorDevido"]?.ToString() ?? "0";
                    string servico = row["NomeServico"]?.ToString() ?? "-";

                    decimal.TryParse(valorStr, out decimal valor);
                    DateTime.TryParse(dataVenc, out DateTime dtVenc);
                    DateTime.TryParse(dataPag, out DateTime dtPag);

                    string status = string.IsNullOrEmpty(dataPag)
                        ? (DateTime.Now > dtVenc ? "Em Atraso" : "Pendente")
                        : "Pago";

                    Historico.Add(new PagamentoInfo
                    {
                        Data = dtVenc.ToString("dd/MM/yyyy"),
                        Servico = servico,
                        Valor = valor.ToString("C"),
                        Status = status
                    });

                    if (status == "Pago")
                    {
                        totalPago += valor;
                        if (ultimoPagData == null || dtPag > ultimoPagData)
                        {
                            ultimoPagData = dtPag;
                            ultimoPagValor = valor;
                        }
                    }
                    else if (status != "Pago" && proxPagData == null)
                    {
                        proxPagData = dtVenc;
                        proxPagValor = valor;
                    }
                }

                UltimoPagamentoValor = ultimoPagValor.ToString("C");
                UltimoPagamentoData = ultimoPagData?.ToString("dd/MM/yyyy") ?? "-";
                TotalPago = totalPago.ToString("C");
                ProximoPagamentoValor = proxPagValor.ToString("C");
                ProximoPagamentoData = proxPagData?.ToString("dd/MM/yyyy") ?? "-";
            }
            catch (Exception ex)
            {
                Mensagem = "❌ Erro ao carregar tela financeira: " + ex.Message;
            }

            return Page();
        }
    }

    public class PagamentoInfo
    {
        public string Data { get; set; }
        public string Servico { get; set; }
        public string Valor { get; set; }
        public string Status { get; set; }
    }

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
