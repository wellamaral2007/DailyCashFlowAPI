using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NBomber.CSharp;
using Xunit;
using DailyCashFlow.Integration.Clients; 

namespace Tests.DailyCashFlow.Performance
{
    public class CashFlowLoadTestsRunner
    {
        [Fact] 
        public async Task Run_CashFlow_Load_Test()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            using var httpClient = new HttpClient(handler);
            //string apiUrl = "https://localhost:8443"; // 🚀 ssh tunnel para o APIGEE 
            
            // 🚀 Call directo to cloud function
            string apiUrl = "https://us-central1-daily-cashflow-sol-17852.cloudfunctions.net/daily-cashflow-balance";
            httpClient.BaseAddress = new Uri(apiUrl);

            var apiClient = new DailyCashFlowApiClient(httpClient);
            
            // 🚀 CAPTURA O TOKEN: O método agora retorna a string do token gerado pelo Apigee [1.3]
            // string tokenReal = await apiClient.AuthenticateAsync(username, password);

            // Opcional: Pode manter a autenticação se a sua API local exigir, 
            // ou passar um token fake como "local-token", já que o localhost não validará o OAuth
            string tokenReal = "local-bypass-token"; 

            // 🚀 INJETA NO CENÁRIO: Passa o token obtido diretamente para o escopo do NBomber [1.1]
            var scenario = CashFlowLoadScenario.Build(httpClient, apiUrl, tokenReal);

            var stats = NBomberRunner
                .RegisterScenarios(scenario)
                .Run();

            var scenarioStats = stats.ScenarioStats.FirstOrDefault(x => 
                x.ScenarioName == "gcp_faas_daily_cash_flow_load_test" || x.ScenarioName == "gcp_faas_load_test");
            
            Assert.NotNull(scenarioStats);

            int totalRequests = scenarioStats.AllRequestCount;
            int totalFailures = scenarioStats.AllFailCount;
            int totalOk = scenarioStats.AllOkCount;
            
            double failurePercentage = totalRequests > 0 ? ((double)totalFailures / totalRequests) * 100 : 0;

            Console.WriteLine($"\n[NBOMBER SLA] Total Geral: {totalRequests} | Sucesso: {totalOk} | Falhas: {totalFailures}");
            Console.WriteLine($"[NBOMBER SLA] Taxa de perda calculada: {failurePercentage:F2}%\n");

            Assert.True(failurePercentage <= 5.0, $"A taxa de falhas de {failurePercentage:F2}% ultrapassou o limite do SLA de 5%.");
        } 
    } 
}
