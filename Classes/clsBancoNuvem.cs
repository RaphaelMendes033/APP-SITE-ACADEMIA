using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace APP_SITE_ACADEMIA.Classes
{
    public class clsBancoNuvem
    {
        // 🔹 Configurações fixas
        private readonly string apiUrl = "https://cflv2aczvz.g2.sqlite.cloud/v2/weblite/sql";
        private readonly string apiKey = "B4xLGomzkME8p9AliBrdGJHKGiqt2KEGHPrjDZZ51Os";

        public string BancoEmpresa { get; private set; } = "";
        public bool Logado { get; private set; } = false;

        // 🔹 Testa conexão com o banco informado




        public static class SessaoNuvem
        {
            public static string BancoAtual { get; set; } = "";
            public static string DocumentoUsuario { get; set; } = "";
            public static bool Logado => !string.IsNullOrEmpty(BancoAtual);
        }









        public async Task<string> TestarConexaoBancoAsync(string nomeBanco)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", apiKey);

                    var body = new
                    {
                        database = nomeBanco,
                        sql = "SELECT 1;"
                    };

                    var json = JsonConvert.SerializeObject(body);
                    var response = await client.PostAsync(apiUrl, new StringContent(json, Encoding.UTF8, "application/json"));
                    string result = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                        return "✅ Conectado com sucesso ao banco informado.";
                    else
                        return $"❌ Falha na conexão. Status: {response.StatusCode}. Resposta: {result}";
                }
            }
            catch (Exception ex)
            {
                return $"❌ Erro ao testar conexão com o banco {nomeBanco}: {ex.Message}";
            }
        }

        // 🔹 Faz login dentro do banco informado
        public async Task<string> FazerLoginAsync(string nomeBanco, string documento, string senha)
        {
            if (string.IsNullOrWhiteSpace(nomeBanco))
                throw new Exception("O nome do banco não foi informado.");

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);

                string sql = $@"
            SELECT Nome, Ativo
            FROM Usuarios
            WHERE Documento = '{documento}'
            AND Senha = '{senha}'
            LIMIT 1;";

                var body = new
                {
                    database = nomeBanco,
                    sql
                };

                var json = JsonConvert.SerializeObject(body);
                HttpResponseMessage response;

                try
                {
                    response = await client.PostAsync(apiUrl, new StringContent(json, Encoding.UTF8, "application/json"));
                }
                catch (HttpRequestException)
                {
                    // 🔹 Erro de rede ou banco inexistente
                    throw new Exception("❌ Banco de dados não encontrado ou inacessível.");
                }

                string result = await response.Content.ReadAsStringAsync();

                // 🔹 Caso o banco realmente não exista ou a requisição seja rejeitada
                if (!response.IsSuccessStatusCode)
                {
                    // 🔍 Tenta interpretar o retorno de erro da API
                    string erroLower = result.ToLower();

                    if (erroLower.Contains("database not found") ||
                        erroLower.Contains("no such database") ||
                        erroLower.Contains("not exist") ||
                        erroLower.Contains("unknown database") ||
                        erroLower.Contains("invalid database"))
                    {
                        throw new Exception("❌ Banco de dados não encontrado.");
                    }
                    else
                    {
                        throw new Exception($"❌ Erro ao consultar a nuvem. Status HTTP: {response.StatusCode}. Resposta: {result}");
                    }
                }


                var data = JObject.Parse(result);

                // 🔹 Nenhum usuário encontrado
                if (data["data"] is not JArray arr || arr.Count == 0)
                    throw new Exception(" Usuário ou senha incorretos.");

                string nomeUsuario = arr[0]?["Nome"]?.ToString();
                string ativoStr = arr[0]?["Ativo"]?.ToString();

                // 🔹 Verifica se o usuário está ativo
                if (ativoStr != "1")
                    throw new Exception(" Usuário bloqueado.");

                // 🔹 Tudo certo
                BancoEmpresa = nomeBanco;
                Logado = true;

                return $"✅ Login realizado com sucesso. Usuário: {nomeUsuario}";
            }
        }


        // 🔹 Executa SQL dentro do banco da empresa logada
        public async Task<string> ExecutarSqlEmpresaAsync(string sql)
        {
            string banco = SessaoNuvem.BancoAtual;

            if (string.IsNullOrEmpty(banco))
                throw new Exception("Nenhum banco ativo. Faça login antes de continuar.");

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);

                var body = new
                {
                    database = banco,
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
