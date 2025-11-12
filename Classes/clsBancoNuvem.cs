using Newtonsoft.Json;
using System;
using System.Data;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace APP_SITE_ACADEMIA.Classes
{
    public class clsBancoNuvem
    {
        private readonly string apiUrl;
        private readonly string apiKey;
        private readonly string databaseName;

        // 🔹 Construtor padrão (usa seus valores fixos)
        public clsBancoNuvem()
        {
            apiUrl = "https://cflv2aczvz.g2.sqlite.cloud/v2/weblite/sql";
            apiKey = "B4xLGomzkME8p9AliBrdGJHKGiqt2KEGHPrjDZZ51Os"; // ADMIN
            databaseName = "SGA";
        }

        // 🔹 Construtor opcional (permite outros bancos, se precisar no futuro)
        public clsBancoNuvem(string url, string key, string db)
        {
            apiUrl = url;
            apiKey = key;
            databaseName = db;
        }

        // 🔹 Testa conexão com o banco
        public async Task<bool> TestarConexaoAsync()
        {
            try
            {
                string sql = "SELECT name FROM sqlite_master WHERE type='table' LIMIT 1;";

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", apiKey);

                    var json = JsonConvert.SerializeObject(new { database = databaseName, sql });
                    var response = await client.PostAsync(apiUrl, new StringContent(json, Encoding.UTF8, "application/json"));

                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        // 🔹 Obtém API Key da empresa (login)
        // 🔹 Obtém API Key da empresa (login)
        public async Task<string> ObterApiKeyDaNuvemAsync(string documento, string senha)
        {
            string sql = $@"
        SELECT ApiKey
        FROM Empresas
        WHERE Documento = '{documento}'
        AND Senha = '{senha}'
        LIMIT 1;
    ";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);

                // ✅ Novo formato aceito pelo endpoint /v2/weblite/sql
                var body = new
                {
                    data = new
                    {
                        database = databaseName,
                        sql
                    }
                };

                var json = JsonConvert.SerializeObject(body);
                var response = await client.PostAsync(apiUrl, new StringContent(json, Encoding.UTF8, "application/json"));
                string result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Erro ao consultar a nuvem. Status HTTP: {response.StatusCode}. Resposta: {result}");

                var data = JObject.Parse(result);

                // 🔍 Novo formato do retorno padronizado (v2)
                if (data["data"] != null &&
                    data["data"]["rows"] != null &&
                    data["data"]["rows"].HasValues)
                {
                    return data["data"]["rows"][0][0]?.ToString()
                        ?? throw new Exception("API Key não encontrada.");
                }
                else
                {
                    throw new Exception("❌ Usuário ou senha inválido, ou usuário inativo.");
                }
            }
        }



    }
}
