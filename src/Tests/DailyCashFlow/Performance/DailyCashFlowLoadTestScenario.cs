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

public static class CashFlowLoadScenario
{
    // 🚀 ASSINATURA ATUALIZADA: Agora recebe a string do token de autenticação ativo [1.1]
    public static ScenarioProps Build(HttpClient httpClient, string apiUrl, string bearerToken)
    {
        var faker = new Faker();

        var scenario = Scenario.Create("gcp_faas_daily_cash_flow_load_test", async context =>
            {
                decimal randomAmount = Math.Round(faker.Random.Decimal(-1000.00m, 5000.00m), 2);
                var payload = new EntryRequest(randomAmount);
                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                string amountInBrazilianFormat = payload.Amount.ToString("F2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
                
                // Mapeia o caminho exato do proxy do Apigee que removemos o hífen
                // string dynamicUrlWithQuery = $"{apiUrl}/v1/cashflow/entry?CreditDebitValue={amountInBrazilianFormat}";
                // 🚀 direct to gcp cloud faas
                string dynamicUrlWithQuery = $"{apiUrl}/?CreditDebitValue={amountInBrazilianFormat}";

                // 🚀 INJEÇÃO BLINDADA: Injeta Host e Authorization diretamente em cada cópia do loop do NBomber 
                // para apigee exige o host e o token de autenticação 
                var request = NBomber.Http.CSharp.Http.CreateRequest("POST", dynamicUrlWithQuery)
                                      .WithHeader("Content-Type", "application/json")
                                      .WithHeader("Host", "daily-cashflow-sol-17852.dns-replaceme.example.com")
                                      .WithHeader("Authorization", $"Bearer {bearerToken}") // 👈 Garante a autenticação a 50 RPS!
                                      .WithBody(content);

                // To local or gcp cloud faas call
                //var request = NBomber.Http.CSharp.Http.CreateRequest("POST", dynamicUrlWithQuery)
                //      .WithHeader("Content-Type", "application/json")
                //      .WithBody(content);

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
