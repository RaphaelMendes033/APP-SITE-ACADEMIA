using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace APP_SITE_ACADEMIA.Classes
{
    public class clsBancoNuvem
    {
        // 🔹 Conexão fixa para o banco central SGA
        private readonly string apiUrlSGA = "https://cflv2aczvz.g2.sqlite.cloud/v2/weblite/sql";                                          
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
                    response = await client.PostAsync(apiUrlSGA, new StringContent(json, Encoding.UTF8, "application/json"));
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

                //string nomeEmpresa = arr[0]?[0]?.ToString();
                //string apiKeyEmpresa = arr[0]?[1]?.ToString();
                //bool bloqueado = arr[0]?[2]?.ToString() == "1";

                var row = arr[0] as JObject;

                string nomeEmpresa = row["Nome"]?.ToString();
                string apiKeyEmpresa = row["ApiKey"]?.ToString();
                bool bloqueado = row["Bloqueado"]?.ToObject<int>() == 1;

                if (string.IsNullOrWhiteSpace(apiKeyEmpresa))
                    throw new Exception("❌ Empresa encontrada, mas sem APIKEY cadastrada no SGA.");

                if (bloqueado)
                    throw new Exception($"🚫 A empresa '{nomeEmpresa}' está bloqueada. Contate o administrador.");

                // ✅ Retorna dados completos e confiáveis
                return (nomeEmpresa, apiKeyEmpresa, bloqueado);
            }
        }




        // 🔹 Testa se o banco SGA está acessível na nuvem
        public async Task<(bool Conectado, string Mensagem)> VerificarConexaoSGAAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", apiKeySGA);

                    // 🔹 Corpo correto da requisição (sem "data")
                    var body = new
                    {
                        database = bancoSGA,
                        sql = "SELECT 1;"
                    };

                    string json = JsonConvert.SerializeObject(body);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync(apiUrlSGA, content);
                    string result = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                        return (false, $"❌ Falha ao conectar ao banco SGA. Status HTTP: {response.StatusCode}\n{result}");

                    // ✅ Tenta ler o retorno do WebLite
                    try
                    {
                        var data = JObject.Parse(result);
                        if (data["data"] is JArray arr && arr.Count > 0)
                            return (true, "✅ Conexão com o banco SGA confirmada.");
                        else
                            return (false, "⚠️ Conexão estabelecida, mas resposta inesperada do servidor.");
                    }
                    catch
                    {
                        return (false, "⚠️ Resposta inválida recebida do servidor SGA.");
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"❌ Erro ao tentar conectar ao banco SGA: {ex.Message}");
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

                // 1️⃣ Buscar a ApiKey da empresa no banco SGA
                string sqlSGA = $@"
            SELECT 
                Nome AS NomeEmpresa,
                ApiKey,
                Ativo
            FROM Empresas
            WHERE NumeroBanco = '{numeroBanco}'";

                using (var clientSGA = new HttpClient())
                {
                    clientSGA.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", apiKeySGA);

                    //var bodySGA = new
                    //{
                    //    data = new
                    //    {
                    //        database = bancoSGA,
                    //        sql = sqlSGA
                    //    }
                    //};


                    var bodySGA = new
                    {
                        database = bancoSGA,
                        sql = sqlSGA
                    };

                    var jsonSGA = JsonConvert.SerializeObject(bodySGA);
                    var contentSGA = new StringContent(jsonSGA, Encoding.UTF8, "application/json");

                    var responseSGA = await clientSGA.PostAsync(apiUrlSGA, contentSGA);
                    var respostaSGA = await responseSGA.Content.ReadAsStringAsync();

                    if (!responseSGA.IsSuccessStatusCode)
                        return (false, null, null, $"❌ Erro ao consultar o SGA (HTTP {responseSGA.StatusCode}). {respostaSGA}");

              //      var resultadoSGA = JObject.Parse(respostaSGA);
                    //var rowsSGA = resultadoSGA["data"]?["rows"] as JArray;

                    //if (rowsSGA == null || rowsSGA.Count == 0)
                    //    return (false, null, null, "❌ Empresa não encontrada no SGA.");

                    //string nomeEmpresaSGA = rowsSGA[0]["NomeEmpresa"]?.ToString();
                    //string apiKeyEmpresa = rowsSGA[0]["ApiKey"]?.ToString();
                    //bool ativo = rowsSGA[0]["Ativo"]?.ToObject<int>() == 1;



                    //var rowsSGA = resultadoSGA["data"] as JArray;

                    //if (rowsSGA == null || rowsSGA.Count == 0)
                    //    return (false, null, null, "❌ Empresa não encontrada no SGA.");

                    //string nomeEmpresaSGA = rowsSGA[0][0]?.ToString();
                    //string apiKeyEmpresa = rowsSGA[0][1]?.ToString();
                    //bool ativo = rowsSGA[0][2]?.ToObject<int>() == 1;









                    //var rowsSGA = resultadoSGA["data"] as JArray;

                    //if (rowsSGA == null || rowsSGA.Count == 0)
                    //    return (false, null, null, "❌ Empresa não encontrada no SGA.");

                    //var row = rowsSGA[0] as JObject;
                    //if (row == null)
                    //    return (false, null, null, "❌ Formato inesperado retornado pelo WebLite.");

                    //string nomeEmpresaSGA = row["NomeEmpresa"]?.ToString();
                    //string apiKeyEmpresa = row["ApiKey"]?.ToString();
                    //bool ativo = row["Ativo"]?.ToObject<int>() == 1;


                    var resultadoSGA = JObject.Parse(respostaSGA);

                    // "data" é um array de objetos
                    var dataArray = resultadoSGA["data"] as JArray;

                    if (dataArray == null || dataArray.Count == 0)
                        return (false, null, null, "❌ Empresa não encontrada no SGA.");

                    // A primeira linha é um objeto
                    var row = dataArray[0] as JObject;

                    if (row == null)
                        return (false, null, null, "❌ Formato inesperado retornado pelo servidor.");

                    // Agora extraímos normalmente
                    string nomeEmpresaSGA = row["NomeEmpresa"]?.ToString();
                    string apiKeyEmpresa = row["ApiKey"]?.ToString();
                    bool ativo = row["Ativo"]?.ToObject<int>() == 1;

                    if (!ativo)
                        return (false, nomeEmpresaSGA, apiKeyEmpresa, "❌ Empresa inativa no SGA.");

                    if (string.IsNullOrEmpty(apiKeyEmpresa))
                        return (false, nomeEmpresaSGA, null, "❌ Empresa sem ApiKey configurada.");

                    return (true, nomeEmpresaSGA, apiKeyEmpresa, null);







                    //var rows = resultadoSGA["data"] as JArray;
                    //var cols = resultadoSGA["columns"] as JArray;

                    //if (rows == null || rows.Count == 0)
                    //    return (false, null, null, "❌ Empresa não encontrada no SGA.");

                    //var row = rows[0] as JArray;

                    //string nomeEmpresaSGA = row[cols.IndexOf("NomeEmpresa")]?.ToString();
                    //string apiKeyEmpresa = row[cols.IndexOf("ApiKey")]?.ToString();
                    //bool ativo = row[cols.IndexOf("Ativo")]?.ToObject<int>() == 1;











                    if (!ativo)
                        return (false, nomeEmpresaSGA, apiKeyEmpresa, "❌ Empresa inativa no SGA.");

                    if (string.IsNullOrEmpty(apiKeyEmpresa))
                        return (false, nomeEmpresaSGA, null, "❌ Empresa sem ApiKey configurada.");

                    // 2️⃣ Usar a ApiKey da empresa para acessar o banco dela e obter o nome real
                    //using (var clientEmpresa = new HttpClient())
                    //{
                    //    clientEmpresa.DefaultRequestHeaders.Authorization =
                    //        new AuthenticationHeaderValue("Bearer", apiKeyEmpresa);

                    //    string sqlEmpresa = "SELECT Nome AS NomeEmpresa FROM Empresas LIMIT 1;";

                    //    var bodyEmpresa = new
                    //    {
                    //        data = new
                    //        {
                    //            database = numeroBanco,
                    //            sql = sqlEmpresa
                    //        }
                    //    };

                    //    var jsonEmpresa = JsonConvert.SerializeObject(bodyEmpresa);
                    //    var contentEmpresa = new StringContent(jsonEmpresa, Encoding.UTF8, "application/json");

                    //    var responseEmpresa = await clientEmpresa.PostAsync(apiUrl, contentEmpresa);
                    //    var respostaEmpresa = await responseEmpresa.Content.ReadAsStringAsync();


                 //   dsfsdfsdf;
                    // pq aqui faz o select de novo ? se la em cima ja foi feito ?
                    //using (var clientEmpresa = new HttpClient())
                    //{
                    //    clientEmpresa.DefaultRequestHeaders.Authorization =
                    //        new AuthenticationHeaderValue("Bearer", apiKeyEmpresa);

                    //    string sqlEmpresa = "SELECT Nome AS NomeEmpresa FROM Empresas LIMIT 1;";

                    //    var bodyEmpresa = new
                    //    {
                    //        database = numeroBanco,
                    //        sql = sqlEmpresa
                    //    };

                    //    var jsonEmpresa = JsonConvert.SerializeObject(bodyEmpresa);
                    //    var contentEmpresa = new StringContent(jsonEmpresa, Encoding.UTF8, "application/json");

                    //    var responseEmpresa = await clientEmpresa.PostAsync(apiUrl, contentEmpresa);
                    //    var respostaEmpresa = await responseEmpresa.Content.ReadAsStringAsync();











                    //    if (!responseEmpresa.IsSuccessStatusCode)
                    //        return (false, nomeEmpresaSGA, apiKeyEmpresa, $"❌ Erro ao consultar banco da empresa (HTTP {responseEmpresa.StatusCode}). {respostaEmpresa}");

                    //    var resultadoEmpresa = JObject.Parse(respostaEmpresa);
                    //    var rowsEmpresa = resultadoEmpresa["data"]?["rows"] as JArray;

                    //    if (rowsEmpresa == null || rowsEmpresa.Count == 0)
                    //        return (false, nomeEmpresaSGA, apiKeyEmpresa, "⚠️ Empresa encontrada no SGA, mas não no banco da empresa.");

                    //     string nomeFinal = rowsEmpresa[0]["NomeEmpresa"]?.ToString() ?? nomeEmpresaSGA;
                    string nomeFinal = nomeEmpresaSGA;

                    return (true, nomeFinal, apiKeyEmpresa, null);
                    }
          //      }
            }
            catch (Exception ex)
            {
                return (false, null, null, $"❌ Erro ao buscar empresa: {ex.Message}");
            }
        }




        // 🔹 Etapa 2: faz login no banco da empresa usando ApiKey retornada
        //    public async Task<string> FazerLoginAsync(string nomeBanco, string documento, string senha)
        //    {
        //        if (string.IsNullOrWhiteSpace(nomeBanco))
        //            throw new Exception("❌ O nome do banco não foi informado.");

        //        // Busca Nome + ApiKey da empresa no banco central (SGA)
        //        var (nomeEmpresa, apiKeyEmpresa, bloqueado) = await ObterDadosEmpresaAsync(nomeBanco);

        //        if (bloqueado)
        //            throw new Exception($"🚫 A empresa '{nomeEmpresa}' está bloqueada.");

        //        ApiKeyEmpresa = apiKeyEmpresa;
        //        BancoEmpresa = nomeBanco;

        //        using (var client = CriarHttpClient(ApiKeyEmpresa))
        //        {






        //            string sqlCliente = $@"
        //SELECT Nome, Ativo
        //FROM Usuarios
        //WHERE Documento = '{documento}'
        //AND Senha = '{senha}'
        //;";

        //            using (var clientCliente = new HttpClient())
        //            {
        //                clientCliente.DefaultRequestHeaders.Authorization =
        //                    new AuthenticationHeaderValue("Bearer", apiKeyEmpresa);

        //                //var bodySGA = new
        //                //{
        //                //    data = new
        //                //    {
        //                //        database = bancoSGA,
        //                //        sql = sqlSGA
        //                //    }
        //                //};


        //                var bodyCliente = new
        //                {
        //                    database = BancoEmpresa,
        //                    sql = sqlCliente
        //                };

        //                var jsonCliente = JsonConvert.SerializeObject(bodyCliente);
        //                var contentCliente = new StringContent(jsonCliente, Encoding.UTF8, "application/json");

        //                var responseCliente = await clientCliente.PostAsync(apiUrl, contentCliente);
        //                var respostaCliente = await responseCliente.Content.ReadAsStringAsync();



















        //                string sql = $@"
        //SELECT Nome, Ativo
        //FROM Usuarios
        //WHERE Documento = '{documento}'
        //AND Senha = '{senha}'
        //;";

        //                //var body = new
        //                //{
        //                //    data = new
        //                //    {
        //                //        database = nomeBanco,
        //                //        sql = sql
        //                //    }
        //                //};


        //                var body = new
        //                {
        //                    database = nomeBanco,
        //                    sql = sql
        //                };
        //                //            var contentSGA = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");


        //                string json = JsonConvert.SerializeObject(body);
        //                var content = new StringContent(json, Encoding.UTF8, "application/json");

        //                var response = await client.PostAsync(apiUrl, content);
























        //                //          string sql = $@"
        //                //              SELECT Nome, Ativo
        //                //              FROM Usuarios
        //                //              WHERE Documento = '{documento}'
        //                //              AND Senha = '{senha}'
        //                //              LIMIT 1;";

        //                ////          var body = new { database = nomeBanco, sql };






        //                //          var body = new
        //                //          {
        //                //              data = new
        //                //              {
        //                //                  database = nomeBanco,
        //                //                  sql = sql
        //                //              }
        //                //          };










        //                //       string json = JsonConvert.SerializeObject(body);

        //                HttpResponseMessage responsee;
        //                try
        //                {
        //                    responsee = await client.PostAsync(apiUrl, new StringContent(json, Encoding.UTF8, "application/json"));
        //                }
        //                catch (HttpRequestException)
        //                {
        //                    throw new Exception("❌ Banco de dados não encontrado ou inacessível.");
        //                }

        //                string result = await responsee.Content.ReadAsStringAsync();

        //                if (!responsee.IsSuccessStatusCode)
        //                {
        //                    string erroLower = result.ToLower();
        //                    if (erroLower.Contains("database not found") ||
        //                        erroLower.Contains("no such database") ||
        //                        erroLower.Contains("not exist") ||
        //                        erroLower.Contains("unknown database") ||
        //                        erroLower.Contains("invalid database"))
        //                        throw new Exception("❌ Banco de dados não encontrado.");
        //                    else
        //                        throw new Exception($"❌ Erro ao consultar a nuvem. Status HTTP: {responsee.StatusCode}. Resposta: {result}");
        //                }

        //                var data = JObject.Parse(result);
        //                if (data["data"] is not JArray arr || arr.Count == 0)
        //                    throw new Exception("❌ Usuário ou senha incorretos.");

        //                string nomeUsuario = arr[0]?[0]?.ToString();
        //                string ativoStr = arr[0]?[1]?.ToString();

        //                if (ativoStr != "1")
        //                    throw new Exception("🚫 Usuário bloqueado.");

        //                // 🔹 Login concluído
        //                Logado = true;
        //                SessaoNuvem.BancoAtual = nomeBanco;
        //                SessaoNuvem.DocumentoUsuario = documento;

        //                return $"✅ Login realizado com sucesso. Usuário: {nomeUsuario} | Empresa: {nomeEmpresa}";
        //            }
        //        }
        //    }


        // 🔹 Executa SQL dentro do banco da empresa logada





        public async Task<string> FazerLoginAsync(string nomeBanco, string documento, string senha)
        {
            if (string.IsNullOrWhiteSpace(nomeBanco))
                throw new Exception("❌ O nome do banco não foi informado.");

            // 1️⃣ Busca Nome + ApiKey da empresa no SGA
            var (nomeEmpresa, apiKeyEmpresa, bloqueado) = await ObterDadosEmpresaAsync(nomeBanco);

            if (bloqueado)
                throw new Exception($"🚫 A empresa '{nomeEmpresa}' está bloqueada.");

            ApiKeyEmpresa = apiKeyEmpresa;
            BancoEmpresa = nomeBanco;

            // 2️⃣ SELECT no banco da empresa
            string sql = $@"
        SELECT Nome, Ativo
        FROM Usuarios
        WHERE Documento = '{documento}'
        AND Senha = '{senha}'
        LIMIT 1;
    ";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", ApiKeyEmpresa);

                var body = new
                {
                    database = nomeBanco,
                    sql = sql
                };

                string json = JsonConvert.SerializeObject(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync("https://coloa4kivz.g1.sqlite.cloud/v2/weblite/sql", content);
                string result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    string erroLower = result.ToLower();
                    if (erroLower.Contains("unauthorized"))
                        throw new Exception("❌ APIKEY inválida ou não autorizada.");
                    else
                        throw new Exception($"❌ Erro ao consultar a nuvem. Status HTTP: {response.StatusCode}. Resposta: {result}");
                }





                var data = JObject.Parse(result);
                var rows = data["data"] as JArray;

                if (rows == null || rows.Count == 0)
                    throw new Exception("❌ Usuário ou senha incorretos.");

                var row = (JObject)rows[0];
                //         string nomeUsuario = rows[0][0]?.ToString();
                //bool ativo = rows[0][1]?.ToObject<int>() == 1;

                string nomeUsuario = row["Nome"]?.ToString();
                int ativo = row["Ativo"]?.ToObject<int>() ?? 0;

                if (ativo != 1)
                    throw new Exception("🚫 Usuário bloqueado.");

                // Sessão
                Logado = true;
                SessaoNuvem.BancoAtual = nomeBanco;
                SessaoNuvem.DocumentoUsuario = documento;

                return $"✅ Login realizado com sucesso. Usuário: {nomeUsuario} | Empresa: {nomeEmpresa}";
            }
        }







        public async Task<string> ExecutarSqlEmpresaAsync(string sql)
        {
            string banco = SessaoNuvem.BancoAtual;

            if (string.IsNullOrEmpty(banco))
                throw new Exception("⚠️ Nenhum banco ativo. Faça login antes de continuar.");

            using (var client = CriarHttpClient(ApiKeyEmpresa))
            {
                var body = new { database = banco, sql };
                string json = JsonConvert.SerializeObject(body);

                var response = await client.PostAsync(apiUrlSGA, new StringContent(json, Encoding.UTF8, "application/json"));
                string result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"❌ Erro ao executar SQL: {response.StatusCode}. Resposta: {result}");

                return result;
            }
        }



        public async Task<string> ConsultarEmpresasAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // 🔹 Configurar o endpoint e autenticação
                    client.BaseAddress = new Uri("https://api.restdb.io/rest/");
                    client.DefaultRequestHeaders.Add("x-apikey", "AdB2Xn0vJvb26T5Xx1C8Dx");
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    // 🔹 Endpoint exato da sua coleção (tabela) no banco SGA
                    var url = "empresas?q={}&h=false&metafields=false";

                    // 🔹 Envia a requisição GET
                    HttpResponseMessage response = await client.GetAsync(url);

                    // 🔹 Trata o retorno
                    string result = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                        throw new Exception($"❌ Erro ao consultar a nuvem. Status HTTP: {response.StatusCode}\n\nResposta: {result}");

                    return result;
                }
            }
            catch (Exception ex)
            {
                return $"❌ Erro ao consultar a nuvem: {ex.Message}";
            }
        }




        public async Task<(bool Sucesso, string Retorno)> ConsultarEmpresasWebLiteAsync()
        {
            try
            {
                string apiUrl = "https://api.weblite.com.br/v2/weblite/sql/sql"; // ✅ confirme a URL base exata do seu servidor
                string apiKeySGA = "SUA_API_KEY_DO_BANCO_SGA"; // ✅ coloque a apikey do banco SGA aqui

                string sql = "SELECT * FROM Empresas";

                using (var client = new HttpClient())
                {
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

                    var response = await client.PostAsync(apiUrl, content);
                    var resposta = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                        return (false, $"❌ Erro ao consultar a nuvem (HTTP {response.StatusCode}). Resposta: {resposta}");

                    return (true, resposta);
                }
            }
            catch (Exception ex)
            {
                return (false, $"❌ Erro ao consultar a nuvem: {ex.Message}");
            }
        }


    }
}
