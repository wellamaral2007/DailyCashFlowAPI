using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Command; // Ingestion of the core abstract port contract
using DailyCashFlowBalanceMS.CashFlow.Domain.Entity; // Ingestion of the rich Domain Model Entity
using CloudNative.CloudEvents; // CNCF CloudEvents standard implementation v1.0
using CloudNative.CloudEvents.SystemTextJson;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using BaseFramework.Event.DailyCashFlow;


namespace DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.EDA.Controller;

/**
 * ============================================================================
 * TOGAF DIMENSION: Phase D - Technology Architecture (Outbound Middleware)
 * HEXAGONAL ROLE:  Outbound Driven Adapter - EDA Publisher Implementation
 * CONCEPTS:        Google Cloud Pub/Sub Integration / CNCF CloudEvents Envelope
 * ============================================================================
 * This class implements the abstract outbound port. It handles the physical 
 * infrastructure mechanics to publish rich domain entities onto Google Pub/Sub.
 */
public class GenerateDailyCashFlowController : IEventDailyCashFlowPublisherCommandPort
{
    private readonly string _projectId = "daily-cashflow-sol-17852";
    private readonly string _topicId = "DailyCashFlowEntry";

    /**
     * Implements the abstract port method to translate and broadcast the domain entity.
     */
    public async Task PublishDailyCashEventAsync(DailyCashFlowEntity entry)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));

        // 1. Instancia o contrato corporativo do BaseFramework preenchendo as propriedades fortemente tipadas
        var eventPayload = new DailyCashFlowEvent
        {
            Id = entry.Id,
            CreditDebitValue = entry.CreditDebitValue,
            ThowDate = entry.ThowDate,
            EventType = entry.EventType
        };

        // 2. Envelopa o payload tipado diretamente na propriedade Data do CloudEvent
        var cloudEvent = new CloudEvent
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri("https://bank.com"),
            Type = $"DailyCashFlow.Event.{entry.EventType}",
            DataContentType = "application/json",
            Data = eventPayload // 📌 FIXED: Injeção do objeto fortemente tipado aqui
        };

        // 3. Serializa a estrutura completa do CloudEvent em bytes planos
        var jsonFormatter = new JsonEventFormatter();
        var cloudEventData = jsonFormatter.EncodeStructuredModeMessage(cloudEvent, out _);

        // 4. Empacota o array de bytes resultante na mensagem do Pub/Sub
        var pubsubMessage = new PubsubMessage
        {
            Data = ByteString.CopyFrom(cloudEventData.ToArray())
        };

        TopicName topicName = TopicName.FromProjectTopic(_projectId, _topicId);
        PublisherClient publisher = await PublisherClient.CreateAsync(topicName);
        
        await publisher.PublishAsync(pubsubMessage);
    }

}
