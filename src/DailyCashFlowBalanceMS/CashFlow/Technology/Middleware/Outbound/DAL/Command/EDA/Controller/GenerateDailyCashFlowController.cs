// File: src/GenerateDailyCashFlowMS/CashFlow/Technology/Middleware/Outbound/Eda/Generate.cs
using System;
using System.Threading.Tasks;
using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Command; // Ingestion of the core abstract port contract
using DailyCashFlowBalanceMS.CashFlow.Domain.Entity; // Ingestion of the rich Domain Model Entity
using Google.Cloud.PubSub.V1; // Google Cloud Pub/Sub official production SDK
using Google.Protobuf;
using CloudNative.CloudEvents; // CNCF CloudEvents standard implementation v1.0
using CloudNative.CloudEvents.SystemTextJson;


namespace GenerateDailyCashFlowMS.CashFlow.Technology.Middleware.Outbound.Eda;

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

        // 1. Enveloping the rich domain data fields into the standardized CNCF CloudEvent envelope layout
        var cloudEvent = new CloudEvent
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri("https://bank.com"),
            Type = $"DailyCashFlow.Event.{entry.EventType}",
            DataContentType = "application/json",
            Data = new
            {
                Id = entry.Id,
                Value = entry.CreditDebitValue, // Precision safe handling serialization boundary type mapping
                Date = entry.ThowDate
            }
        };

        // 2. Encoding and serializing the CloudEvent object structure into standard raw JSON bytes
        var jsonFormatter = new JsonEventFormatter();
        var cloudEventData = jsonFormatter.EncodeStructuredModeMessage(cloudEvent, out _);

        // 3. Packing the serialized byte array payload into the physical Google PubsubMessage format
        var pubsubMessage = new PubsubMessage
        {
            Data = ByteString.CopyFrom(cloudEventData.ToArray())
        };

        // 4. Instantiating the Google production publisher pipeline client engine to ship the message onto the topic
        TopicName topicName = TopicName.FromProjectTopic(_projectId, _topicId);
        PublisherClient publisher = await PublisherClient.CreateAsync(topicName);
        
        // 5. Asynchronously pushing the record out into the managed cloud messaging infrastructure
        await publisher.PublishAsync(pubsubMessage);
    }
}
