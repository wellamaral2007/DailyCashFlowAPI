using System.Text;
using System.Text.Json;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;

using DailyCashFlow.Integration.Models;

namespace Tests.DailyCashFlow.Performance;

public static class CashFlowLoadScenario
{
    public static ScenarioProps Build(HttpClient httpClient, string apiUrl)
    {
        // 1. Definição do Payload usando records limpos do .NET 8
        var payload = new EntryRequest(150.50m);
        var jsonPayload = JsonSerializer.Serialize(payload);
        

        // 2. Definição do Passo HTTP (Step) nativo do NBomber
        // Usamos StringContent padrão com codificação UTF-8 explícita

        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            
        var request = NBomber.Http.CSharp.Http.CreateRequest("POST", apiUrl)
                              .WithHeader("Content-Type", "application/json")
                              .WithBody(content);
        //var step = Scenario.Create("create_entry", httpClient, async context =>
        //{


          //  return await NBomber.Http.CSharp.Http.Send(request, context);
        //});

        // 3. Configuração do Cenário (Injeção de 50 RPS constantes por 1 minuto)
        var scenario = Scenario.Create("gcp_faas_load_test", async context =>
            {
                var response = await Http.Send(httpClient, request);

                return response;
            }).WithWarmUpDuration(TimeSpan.FromSeconds(10)) // Período de aquecimento para evitar falso-positivo de Cold Start
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 50,                          // 50 requisições
                    interval: TimeSpan.FromSeconds(1), // Por segundo
                    during: TimeSpan.FromMinutes(1)    // Durante 1 minuto
                )
            );

        // 4. Critério de Aceitação / SLA (Máximo de 5% de falhas/perdas permitidas)
        scenario = scenario.WithThresholds(
            Threshold.Create(status => status.AllFailCount <= 0.05)
        );

        return scenario;
    }
}
