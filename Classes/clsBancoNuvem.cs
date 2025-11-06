using Newtonsoft.Json;
using System;
using System.Data;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace APP_SITE_ACADEMIA.Classes
{
    public class clsBancoNuvem
    {
        // --- ajuste esses valores se necessário ---
        private static readonly string apiUrl = "https://cflv2aczvz.g2.sqlite.cloud/v2/weblite/sql";
        private static readonly string apiKey = "B4xLGomzkME8p9AliBrdGJHKGiqt2KEGHPrjDZZ51Os"; // sua chave admin
        private static readonly string databaseName = "SGA"; // ou "SGA.sqlite" se seu painel exigir

        /// <summary>
        /// Retorna o nome da empresa cujo Documento = cnpj e Id = codigo.
        /// Retorna null se não encontrar.
        /// </summary>
        public async Task<string> ObterNomeEmpresaAsync(string cnpj, string codigo)
        {
            try
            {
                // sanitize simples
                cnpj = (cnpj ?? "").Trim();
                codigo = (codigo ?? "").Trim();

                if (string.IsNullOrEmpty(cnpj) || string.IsNullOrEmpty(codigo))
                    return null;

                // Monta o SELECT (LIMIT 1)
                string sql = $"SELECT Nome FROM Empresas WHERE Documento = '{EscapeSql(cnpj)}' AND Codigo = '{EscapeSql(codigo)}' LIMIT 1";

                // Executa via WebLite
                var tabela = await ExecutarConsultaAsync(sql);

                if (tabela != null && tabela.Rows.Count > 0 && tabela.Columns.Contains("Nome"))
                {
                    return tabela.Rows[0]["Nome"]?.ToString();
                }

                // Caso a API retorne com outro nome de coluna (ex: nome com case diferente)
                if (tabela != null && tabela.Rows.Count > 0 && tabela.Columns.Count > 0)
                {
                    return tabela.Rows[0][0]?.ToString();
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Executa um SELECT via WebLite e converte o resultado em DataTable.
        /// </summary>
        private async Task<DataTable> ExecutarConsultaAsync(string sql)
        {
            using (var client = new HttpClient())
            {
                // Headers
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Payload conforme WebLite (incluir database + sql)
                var payload = new
                {
                    database = databaseName,
                    sql = sql
                };

                string json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync(apiUrl, content);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    // opcional: lançar exceção com o body para debug
                    throw new Exception($"Erro na requisição WebLite: {(int)response.StatusCode}\n{responseBody}");
                }

                // Tenta interpretar o JSON retornado.
                // A API WebLite costuma retornar um JSON com um campo "data" contendo linhas (cada linha é um objeto com colunas).
                var tabela = new DataTable();

                try
                {
                    dynamic jsonResult = JsonConvert.DeserializeObject(responseBody);

                    // Forma 1: resposta com "data" (cada item é objeto com propriedades = colunas)
                    if (jsonResult != null && jsonResult.data != null && jsonResult.data.Count > 0)
                    {
                        // cria colunas a partir da primeira linha
                        var primeira = jsonResult.data[0];
                        foreach (var prop in primeira)
                        {
                            var jprop = (Newtonsoft.Json.Linq.JProperty)prop;
                            if (!tabela.Columns.Contains(jprop.Name))
                                tabela.Columns.Add(jprop.Name);
                        }

                        // preenche linhas
                        foreach (var linha in jsonResult.data)
                        {
                            var row = tabela.NewRow();
                            foreach (var prop in linha)
                            {
                                var jprop = (Newtonsoft.Json.Linq.JProperty)prop;
                                row[jprop.Name] = jprop.Value?.ToString() ?? string.Empty;
                            }
                            tabela.Rows.Add(row);
                        }

                        return tabela;
                    }

                    // Forma 2: algumas variantes devolvem um array de arrays ou outra estrutura.
                    // Tentamos um fallback: procurar por results/response/rows (formas que vimos em respostas anteriores).
                    try
                    {
                        // Exemplo de caminho alternativo: data in data[0].results[0].response[0].rows...
                        var possible = jsonResult[0]?.results?[0]?.response?[0]?.rows;
                        if (possible != null && possible.Count > 0)
                        {
                            // possible é array de arrays, sem nomes de coluna: best-effort - precisamos das colunas manualmente
                            // neste caso retornamos um DataTable com col1, col2...
                            int cols = possible[0].Count;
                            for (int c = 0; c < cols; c++)
                                tabela.Columns.Add("col" + (c + 1));

                            foreach (var rowArr in possible)
                            {
                                var row = tabela.NewRow();
                                for (int c = 0; c < cols; c++)
                                    row[c] = rowArr[c]?.ToString() ?? string.Empty;
                                tabela.Rows.Add(row);
                            }

                            return tabela;
                        }
                    }
                    catch
                    {
                        // não conseguiu interpretar fallback
                    }

                    // Se não reconheceu formato, retorna DataTable vazio
                    return tabela;
                }
                catch (Exception ex)
                {
                    throw new Exception("Erro ao interpretar JSON retornado da API WebLite: " + ex.Message + "\n\n" + responseBody);
                }
            }
        }

        // Pequena sanitação para evitar inserir aspas direto no SQL (não substitui parametrização completa,
        // porém é aceitável para teste rápido; em produção use procedimentos seguros)
        private string EscapeSql(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Replace("'", "''");
        }






        // faz o Select de autenticação do usuario   //Ativo é campo numerico 1 = ativo   /   0 = bloqueado
        public async Task<string> ValidarLoginPessoaAsync(string codigoEmpresa, string senha)
        {
            try
            {
                string sql = $"SELECT Nome FROM Pessoas WHERE fk_Empresa = '{EscapeSql(codigoEmpresa)}' AND Senha = '{EscapeSql(senha)}' AND Ativo = 1 LIMIT 1";
                var tabela = await ExecutarConsultaAsync(sql);

                if (tabela != null && tabela.Rows.Count > 0)
                    return tabela.Rows[0]["Nome"]?.ToString();

                return null;
            }
            catch
            {
                return null;
            }
        }





    }
}
