// File: apps/daily-cash-flow-service/src/DailyCashFlowService.API/3_Technology/Middleware/Inbound/Eda/ProcessDailyCashFlowController.cs
using System;
using System.Text; // 📌 ADICIONADO: Necessário para usar o Encoding.UTF8
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DailyCashFlowBalanceMS.CashFlow.Domain.Entity;
using BaseFramework.Event.DailyCashFlow;
using CloudNative.CloudEvents;
using Google.Cloud.Functions.Framework;
using Google.Cloud.Functions.Hosting;
using System.Runtime.CompilerServices;
using DailyCashFlowBalanceMS.CashFlow.Application.Service;
using DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.Command;

namespace DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Inbound.EDA.Controller;

public class ProcessDailyCashFlowController : ICloudEventFunction<object>
{
    private readonly DailyCashFlowBalanceService _dailyCashFlowService;

    public ProcessDailyCashFlowController(DailyCashFlowBalanceService dailyCashFlowService)
    {
        _dailyCashFlowService = dailyCashFlowService;
    }

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    public async Task HandleAsync(CloudEvent cloudEvent, object data, CancellationToken cancellationToken)
    {
        using (var context = new DailyCashFlowCommandContext(Environment.GetEnvironmentVariable("ConnectionStrings__db-write-connection")))
        {
            // Cria a tabela física 'CashFlowEntries' se ela estiver faltando na nuvem
            await context.Database.EnsureCreatedAsync(); 
        }

        if (data == null) return;

        DailyCashFlowEvent? eventData = null;
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        try
        {
            // 1. Extrai o texto plano do envelope injetado pelo Google
            string rawJson = data is JsonElement element ? element.GetRawText() : JsonSerializer.Serialize(data);

            using (JsonDocument doc = JsonDocument.Parse(rawJson))
            {
                JsonElement root = doc.RootElement;

                // 2. Localiza o nó de mensagem do padrão Push do Pub/Sub
                if (root.TryGetProperty("message", out JsonElement messageNode) && 
                    messageNode.TryGetProperty("data", out JsonElement dataNode))
                {
                    // 3. Extrai a string Base64 e decodifica para texto UTF-8 (Seu CloudEvent interno)
                    string base64Payload = dataNode.GetString() ?? string.Empty;
                    byte[] decodedBytes = Convert.FromBase64String(base64Payload);
                    string innerCloudEventJson = Encoding.UTF8.GetString(decodedBytes);

                    // 4. Efetua o parse do CloudEvent interno para extrair os dados de negócio (Id, CreditDebitValue, ThowDate, EventType)
                    using (JsonDocument innerDoc = JsonDocument.Parse(innerCloudEventJson))
                    {
                        JsonElement innerRoot = innerDoc.RootElement;
                        if (innerRoot.TryGetProperty("data", out JsonElement realDataFields))
                        {
                            eventData = JsonSerializer.Deserialize<DailyCashFlowEvent>(realDataFields.GetRawText(), options);
                        }
                    }
                }
                else
                {
                    // Fallback de contingência caso você faça um cURL enviando o JSON direto sem o envelope do Pub/Sub
                    eventData = JsonSerializer.Deserialize<DailyCashFlowEvent>(rawJson, options);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EDA DESSERIALIZAÇÃO ERRO]: {ex.Message}");
            return;
        }

        if (eventData == null || string.IsNullOrEmpty(eventData.EventType))
        {
            return;
        }

        // Traduz e normaliza o record de infraestrutura em uma Entidade de Domínio Rica (Hexagonal Core)
        var domainEntity = new DailyCashFlowEntity();
        domainEntity.Id = !string.IsNullOrEmpty(eventData.Id) ? eventData.Id : Guid.NewGuid().ToString(); 
        domainEntity.CreditDebitValue = eventData.CreditDebitValue; 
        domainEntity.ThowDate = eventData.ThowDate != default ? eventData.ThowDate : DateTime.UtcNow;
        domainEntity.EventType = eventData.EventType;

        // Envia a execução diretamente para o Facade da Aplicação Core (SAGA)
        await _dailyCashFlowService.ProcessingDailyCashEvent(domainEntity);
    }
}
