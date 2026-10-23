using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NBomber.CSharp;
using Xunit;
using DailyCashFlow.Integration.Clients; 

namespace Tests.DailyCashFlow.Performance;

/**
 * ============================================================================
 * TOGAF DIMENSION: Phase D - Technology Architecture (Performance Verification)
 * HEXAGONAL ROLE:  Outbound Test Adapter - NBomber Concurrency Load Runner
 * CONCEPTS:        Stress Injection Loops / Service Level Agreement (SLA) Assertions
 * ============================================================================
 * This component orchestrates concurrent transaction injection testing scenarios,
 * verifying that edge proxies and FaaS runtimes adhere to corporate performance gates.
 */
public class CashFlowLoadTestsRunner
{
    /**
     * Executes high-concurrency performance verification against the infrastructure API boundary.
     */
    [Fact] 
    public async Task Run_CashFlow_Load_Test()
    {
         // System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        
        using var httpClient = new HttpClient(handler);

        // 🚀 ADICIONE ESTA LINHA AQUI: Garante o roteamento correto no Load Balancer da Apigee
        httpClient.DefaultRequestHeaders.Host = "daily-cashflow-sol-17852.dns-replaceme.example.com";
        
          /**
         * Resolves the gateway execution strategy dynamically from environment parameters.
         * If BYPASS_APIGEE is set to true, it utilizes the direct local FaaS endpoint with a bypass token.
         * BY LIMITING THE APIGEE GATEWAY EVAL ENVIRONMENT, THIS IS THE MOST SIMILAR WAY TO EXECUTE THE LOAD TESTS.
         */
        string bypassApigeeStr = Environment.GetEnvironmentVariable("BYPASS_APIGEE") ?? "true";
        bool bypassApigee = bool.Parse(bypassApigeeStr);

        string apiUrl;
        string tokenReal;

        if (bypassApigee)
        {
            apiUrl = Environment.GetEnvironmentVariable("FAAS_URL") ?? "http://localhost:8080";
            tokenReal = "local-bypass-token";
            //httpClient.BaseAddress = new Uri(apiUrl);
        }
        else
        {
            apiUrl = Environment.GetEnvironmentVariable("APIGEE_GATEWAY_URL") ?? "https://localhost:8443";
            
            var apiClient = new DailyCashFlowApiClient(httpClient);
            string key = Environment.GetEnvironmentVariable("APIGEE_CLIENT_ID") ?? "local-bypass-token";
            string secret = Environment.GetEnvironmentVariable("APIGEE_CLIENT_SECRET") ?? "local-bypass-token";

            /**
             * Executes the live authentication handshake and extracts the real bearer token from the security authority.
             */
            tokenReal = await apiClient.AuthenticateAsync(key, secret);
        }

         

        /**
         * Binds the fetched security authorization token directly into the load test scenario execution block.
         */
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

        Console.WriteLine($"\n[NBOMBER SLA] Total Requests: {totalRequests} | OK: {totalOk} | Failures: {totalFailures}");
        Console.WriteLine($"[NBOMBER SLA] Calculated error rate threshold: {failurePercentage:F2}%\n");

        Assert.True(failurePercentage <= 5.0, $"The calculated failure rate of {failurePercentage:F2}% exceeded the corporate SLA limit constraint of 5%.");
    } 
}
