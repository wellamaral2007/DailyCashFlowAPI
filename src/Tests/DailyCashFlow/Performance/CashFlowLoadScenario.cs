using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Bogus;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using DailyCashFlow.Integration.Models;

namespace Tests.DailyCashFlow.Performance;

/**
 * ============================================================================
 * TOGAF DIMENSION: Phase D - Technology Architecture (Performance Verification)
 * HEXAGONAL ROLE:  Outbound Test Adapter - NBomber Scenario Profile Builder
 * CONCEPTS:        Load Injection Profiles / SLA Boundary Definition
 * ============================================================================
 * This component orchestrates concurrent transaction injection testing scenarios,
 * verifying that edge proxies and FaaS runtimes adhere to corporate performance gates.
 */
public static class CashFlowLoadScenario
{
    /**
     * Builds and configures the automated NBomber performance injection scenario.
     */
    public static ScenarioProps Build(HttpClient httpClient, string apiUrl, string bearerToken)
    {
        var faker = new Faker();

        /**
         * Initializes the infrastructure load test scenario definition with the core step logic.
         */
        var scenario = Scenario.Create("gcp_faas_daily_cash_flow_load_test", async context =>
            {
                decimal randomAmount = Math.Round(faker.Random.Decimal(-1000.00m, 5000.00m), 2);
                var payload = new EntryRequest(randomAmount);
                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                string bypassApigeeStr = Environment.GetEnvironmentVariable("BYPASS_APIGEE") ?? "true";
                bool bypassApigee = bool.Parse(bypassApigeeStr);

                string targetPathSuffix = bypassApigee 
                    ? "" 
                    : "v1/cashflow/entry";

                string amountInBrazilianFormat = payload.Amount.ToString("F2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
                
                string dynamicUrlWithQuery = string.IsNullOrEmpty(targetPathSuffix)
                    ? $"{apiUrl}/?CreditDebitValue={amountInBrazilianFormat}"
                    : $"{apiUrl}/{targetPathSuffix}?CreditDebitValue={amountInBrazilianFormat}";

                // 🚀 ADICIONE ESTA LINHA AQUI PARA PRINTAR A URL NO TERMINAL:
                // Console.WriteLine($"[NBOMBER DEBUG] URL GERADA: {dynamicUrlWithQuery}");

                /**
                 * Fluent request generation utilizing the NBomber HTTP extension factory channel.
                 * Injects target host routing metadata and the active authorization bearer token.
                 */
                var request = NBomber.Http.CSharp.Http.CreateRequest("POST", dynamicUrlWithQuery)
                                      .WithHeader("Content-Type", "application/json")
                                      .WithHeader("Host", "daily-cashflow-sol-17852.dns-replaceme.example.com")
                                      .WithHeader("Authorization", $"Bearer {bearerToken}") 
                                      .WithBody(content);

                var response = await Http.Send(httpClient, request);
                return response;
            })
            .WithWarmUpDuration(TimeSpan.FromSeconds(10))
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 50,                          
                    interval: TimeSpan.FromSeconds(1), 
                    during: TimeSpan.FromMinutes(1)    
                )
            );

        return scenario;
    }
}
