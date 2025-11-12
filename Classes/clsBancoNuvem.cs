using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace APP_SITE_ACADEMIA.Classes
{
    public class clsBancoNuvem
    {
        private readonly string apiUrl = "https://cflv2aczvz.g2.sqlite.cloud/v2/weblite/sql"; // 🔹 Fixo
        private readonly string apiKey = "B4xLGomzkME8p9AliBrdGJHKGiqt2KEGHPrjDZZ51Os";     // 🔹 Sempre o mesmo
        private readonly string bancoPrincipal = "SGA"; // 🔹 Banco principal com Empresas e Usuarios

        public string BancoEmpresa { get; private set; } = "";
        public string NomeEmpresa { get; private set; } = "";
        public string DocumentoEmpresa { get; private set; } = "";
        public bool Logado { get; private set; } = false;

        // 🔹 Testa a conexão básica com o banco principal
        public async Task<string> TestarConexaoAsync()
        {
            try
            {
                // 🔸 Ignora validação de certificado SSL (caso o servidor SQLite Cloud esteja com certificado intermediário)
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                };

                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", apiKey);

                    string sql = "SELECT 1;";

                    var body = new
                    {
                        database = bancoPrincipal,
                        sql
                    };

                    var json = JsonConvert.SerializeObject(body);

                    var response = await client.PostAsync(apiUrl,
                        new StringContent(json, Encoding.UTF8, "application/json"));

                    string result = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                        return "✅ Conectado com sucesso ao banco principal.";
                    else
                        return $"❌ Falha na conexão. Status: {response.StatusCode}. Resposta: {result}";
                }
            }
            catch (HttpRequestException ex)
            {
                return $"❌ Erro de conexão HTTP: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"❌ Erro ao testar conexão: {ex.Message}";
            }
        }

        // 🔹 Faz login do usuário (documento + senha)
        public async Task<string> ObterApiKeyDaNuvemAsync(string documento, string senha)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);

                string sql = $@"
                    SELECT e.NomeBanco
                    FROM Usuarios u
                    INNER JOIN Empresas e ON e.Codigo = u.fk_empresa
                    WHERE u.Documento = '{documento}'
                    AND u.Senha = '{senha}'
                    AND u.Ativo = 1
                    LIMIT 1;";

                var body = new
                {
                    database = bancoPrincipal,
                    sql
                };

                var json = JsonConvert.SerializeObject(body);
                var response = await client.PostAsync(apiUrl,
                    new StringContent(json, Encoding.UTF8, "application/json"));

                string result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Erro ao consultar a nuvem. Status HTTP: {response.StatusCode}. Resposta: {result}");

                try
                {
                    var data = JObject.Parse(result);

                    // ✅ Novo formato: data = [ { "NomeBanco": "teste-cliente-academia" } ]
                    if (data["data"] is JArray arr && arr.Count > 0)
                    {
                        BancoEmpresa = arr[0]?["NomeBanco"]?.ToString();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Erro ao interpretar resposta da nuvem: {ex.Message}\nRetorno: {result}");
                }

                if (string.IsNullOrEmpty(BancoEmpresa))
                    throw new Exception("❌ Usuário ou senha inválido, ou usuário inativo.");

                Logado = true;
                return BancoEmpresa;
            }
        }

        // 🔹 Executa SQL dentro do banco da empresa logada
        public async Task<string> ExecutarSqlEmpresaAsync(string sql)
        {
            if (!Logado || string.IsNullOrEmpty(BancoEmpresa))
                throw new Exception("É necessário fazer login antes de acessar o banco da empresa.");

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);

                var body = new
                {
                    database = BancoEmpresa,
                    sql
                };

                var json = JsonConvert.SerializeObject(body);
                var response = await client.PostAsync(apiUrl, new StringContent(json, Encoding.UTF8, "application/json"));
                string result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Erro ao executar SQL: {response.StatusCode}. Resposta: {result}");

                return result;
            }
        }
    }
}
