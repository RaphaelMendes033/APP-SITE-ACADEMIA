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
        // 🔹 Conexão fixa para o banco central SGA
        private readonly string apiUrl = "https://cflv2aczvz.g2.sqlite.cloud/v2/weblite/sql";
        private readonly string apiKeySGA = "B4xLGomzkME8p9AliBrdGJHKGiqt2KEGHPrjDZZ51Os";
        private readonly string bancoSGA = "SGA";

        // 🔹 Dados dinâmicos da empresa logada
        public string BancoEmpresa { get; private set; } = "";
        public string ApiKeyEmpresa { get; private set; } = "";
        public bool Logado { get; private set; } = false;

        // 🔹 Sessão estática (armazenamento simples)
        public static class SessaoNuvem
        {
            public static string BancoAtual { get; set; } = "";
            public static string DocumentoUsuario { get; set; } = "";
            public static bool Logado => !string.IsNullOrEmpty(BancoAtual);
        }

        // 🔹 Cria o cliente HTTP com autorização
        private HttpClient CriarHttpClient(string apiKey)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);
            return client;
        }

     
        // 🔹 Etapa 1: busca ApiKey da empresa no banco SGA (BANCO CENTRAL)
        private async Task<(string NomeEmpresa, string ApiKeyEmpresa, bool Bloqueado)> ObterDadosEmpresaAsync(string numeroBanco)
        {
            // ✅ Validação reforçada
            numeroBanco = numeroBanco?.Trim();
            if (string.IsNullOrWhiteSpace(numeroBanco))
                throw new Exception("❌ Número do banco não informado.");

            using (var client = CriarHttpClient(apiKeySGA))
            {
                // ✅ Consulta completa: Nome, ApiKey e Bloqueado (Ativo <> 1)
                string sql = $@"
            SELECT 
                Nome, 
                ApiKey, 
                (CASE WHEN Ativo <> 1 THEN 1 ELSE 0 END) AS Bloqueado
            FROM Empresas
            WHERE NumeroBanco = '{numeroBanco}'
            LIMIT 1;
        ";

                var body = new { database = bancoSGA, sql };
                string json = JsonConvert.SerializeObject(body);

                HttpResponseMessage response;
                try
                {
                    response = await client.PostAsync(apiUrl, new StringContent(json, Encoding.UTF8, "application/json"));
                }
                catch (HttpRequestException ex)
                {
                    throw new Exception("❌ Falha de comunicação com o banco SGA: " + ex.Message);
                }

                string result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"❌ Erro ao consultar banco SGA. Status: {response.StatusCode}. Resposta: {result}");

                var data = JObject.Parse(result);
                if (data["data"] is not JArray arr || arr.Count == 0)
                    throw new Exception($"❌ Empresa com número '{numeroBanco}' não encontrada no banco SGA.");

                string nomeEmpresa = arr[0]?[0]?.ToString();
                string apiKeyEmpresa = arr[0]?[1]?.ToString();
                bool bloqueado = arr[0]?[2]?.ToString() == "1";

                if (string.IsNullOrWhiteSpace(apiKeyEmpresa))
                    throw new Exception("❌ Empresa encontrada, mas sem APIKEY cadastrada no SGA.");

                if (bloqueado)
                    throw new Exception($"🚫 A empresa '{nomeEmpresa}' está bloqueada. Contate o administrador.");

                // ✅ Retorna dados completos e confiáveis
                return (nomeEmpresa, apiKeyEmpresa, bloqueado);
            }
        }







        // 🔹 Método público para buscar empresa (usado pelo AJAX e pelo login)
        // 🔹 Método público para buscar empresa (substitui OnGetBuscarEmpresaAsync)
        public async Task<(bool Sucesso, string NomeEmpresa, string ApiKey, string Erro)> BuscarEmpresaAsync(string numeroBanco)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(numeroBanco))
                    return (false, null, null, "❌ Número do banco não informado.");

                // 🔹 SQL que roda no banco SGA fixo
                string sql = $@"
            SELECT Nome AS NomeEmpresa, ApiKey, Bloqueado 
            FROM Empresas 
            WHERE NumeroBanco = '{numeroBanco}'";

                using (var client = new HttpClient())
                {
                    // 🔹 Usa o APIKEY e o banco SGA fixos (já definidos na classe)
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", apiKeySGA);

                    var body = new
                    {
                        data = new
                        {
                            database = "SGA",
                            sql
                        }
                    };

                    var json = JsonConvert.SerializeObject(body);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // 🔹 Envia a requisição
                    var response = await client.PostAsync($"{apiUrl}/sql", content);
                    var resposta = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                        return (false, null, null, $"❌ Erro ao consultar a nuvem. Status HTTP: {response.StatusCode}");

                    // 🔹 Converte o JSON de resposta
                    var resultado = JObject.Parse(resposta);

                    // 🔸 Garante que há dados
                    var rows = resultado["data"]?["rows"];
                    if (rows == null || !rows.HasValues)
                        return (false, null, null, "❌ Empresa não encontrada no banco SGA.");

                    // 🔹 Lê os dados da primeira linha retornada
                    string nomeEmpresa = rows[0]["NomeEmpresa"]?.ToString();
                    string apiKeyEmpresa = rows[0]["ApiKey"]?.ToString();
                    bool bloqueado = rows[0]["Bloqueado"]?.ToObject<bool>() ?? false;

                    if (bloqueado)
                        return (false, nomeEmpresa, apiKeyEmpresa, "❌ Empresa bloqueada.");

                    if (string.IsNullOrEmpty(apiKeyEmpresa))
                        return (false, nomeEmpresa, null, "❌ Empresa sem ApiKey configurada.");

                    return (true, nomeEmpresa, apiKeyEmpresa, null);
                }
            }
            catch (Exception ex)
            {
                return (false, null, null, $"❌ Erro ao buscar empresa: {ex.Message}");
            }
        }





        // 🔹 Etapa 2: faz login no banco da empresa usando ApiKey retornada
        public async Task<string> FazerLoginAsync(string nomeBanco, string documento, string senha)
        {
            if (string.IsNullOrWhiteSpace(nomeBanco))
                throw new Exception("❌ O nome do banco não foi informado.");

            // Busca Nome + ApiKey da empresa no banco central (SGA)
            var (nomeEmpresa, apiKeyEmpresa, bloqueado) = await ObterDadosEmpresaAsync(nomeBanco);

            if (bloqueado)
                throw new Exception($"🚫 A empresa '{nomeEmpresa}' está bloqueada.");

            ApiKeyEmpresa = apiKeyEmpresa;
            BancoEmpresa = nomeBanco;

            using (var client = CriarHttpClient(ApiKeyEmpresa))
            {
                string sql = $@"
                    SELECT Nome, Ativo
                    FROM Usuarios
                    WHERE Documento = '{documento}'
                    AND Senha = '{senha}'
                    LIMIT 1;";

                var body = new { database = nomeBanco, sql };
                string json = JsonConvert.SerializeObject(body);

                HttpResponseMessage response;
                try
                {
                    response = await client.PostAsync(apiUrl, new StringContent(json, Encoding.UTF8, "application/json"));
                }
                catch (HttpRequestException)
                {
                    throw new Exception("❌ Banco de dados não encontrado ou inacessível.");
                }

                string result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    string erroLower = result.ToLower();
                    if (erroLower.Contains("database not found") ||
                        erroLower.Contains("no such database") ||
                        erroLower.Contains("not exist") ||
                        erroLower.Contains("unknown database") ||
                        erroLower.Contains("invalid database"))
                        throw new Exception("❌ Banco de dados não encontrado.");
                    else
                        throw new Exception($"❌ Erro ao consultar a nuvem. Status HTTP: {response.StatusCode}. Resposta: {result}");
                }

                var data = JObject.Parse(result);
                if (data["data"] is not JArray arr || arr.Count == 0)
                    throw new Exception("❌ Usuário ou senha incorretos.");

                string nomeUsuario = arr[0]?[0]?.ToString();
                string ativoStr = arr[0]?[1]?.ToString();

                if (ativoStr != "1")
                    throw new Exception("🚫 Usuário bloqueado.");

                // 🔹 Login concluído
                Logado = true;
                SessaoNuvem.BancoAtual = nomeBanco;
                SessaoNuvem.DocumentoUsuario = documento;

                return $"✅ Login realizado com sucesso. Usuário: {nomeUsuario} | Empresa: {nomeEmpresa}";
            }
        }

        // 🔹 Executa SQL dentro do banco da empresa logada
        public async Task<string> ExecutarSqlEmpresaAsync(string sql)
        {
            string banco = SessaoNuvem.BancoAtual;

            if (string.IsNullOrEmpty(banco))
                throw new Exception("⚠️ Nenhum banco ativo. Faça login antes de continuar.");

            using (var client = CriarHttpClient(ApiKeyEmpresa))
            {
                var body = new { database = banco, sql };
                string json = JsonConvert.SerializeObject(body);

                var response = await client.PostAsync(apiUrl, new StringContent(json, Encoding.UTF8, "application/json"));
                string result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"❌ Erro ao executar SQL: {response.StatusCode}. Resposta: {result}");

                return result;
            }
        }
    }
}
