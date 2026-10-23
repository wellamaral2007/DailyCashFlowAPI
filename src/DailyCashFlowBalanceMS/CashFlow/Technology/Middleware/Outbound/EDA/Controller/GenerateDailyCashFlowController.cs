using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Command;
using DailyCashFlowBalanceMS.CashFlow.Domain.Entity;
using DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.DTO.Event.DailyCashFlow;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using System;
using System.Threading.Tasks;

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
    private readonly string _projectId;
    private readonly string _topicId;

    /**
     * Initializes a new instance resolving infrastructure parameters dynamically from environment variables.
     */
    public GenerateDailyCashFlowController()
    {
        _projectId = Environment.GetEnvironmentVariable("GCP_PROJECT") ?? "daily-cashflow-sol-17852";
        _topicId = Environment.GetEnvironmentVariable("GCP_TOPIC_ID") ?? "DailyCashFlowEntry";
    }

    /**
     * Implements the abstract port method to translate and broadcast the domain entity.
     */
    public async Task PublishDailyCashEventAsync(DailyCashFlowEntity entry)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));

        /**
         * Instantiates the core data contract mapping strongly-typed domain properties.
         */
        var eventPayload = new DailyCashFlowEvent
        {
            Id = entry.Id,
            CreditDebitValue = entry.CreditDebitValue,
            ThowDate = entry.ThowDate,
            EventType = entry.EventType
        };

        /**
         * Envelopes the typed payload instance inside the standard CNCF CloudEvent data boundary property.
         */
        var cloudEvent = new CloudEvent
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri("https://bank.com"),
            Type = $"DailyCashFlow.Event.{entry.EventType}",
            DataContentType = "application/json",
            Data = eventPayload
        };

        /**
         * Serializes the integrated CloudEvent object topology directly into raw text byte streams.
         */
        var jsonFormatter = new JsonEventFormatter();
        var cloudEventData = jsonFormatter.EncodeStructuredModeMessage(cloudEvent, out _);

        /**
         * Encapsulates the output byte array inside a structural Google Protobuf message envelope.
         */
        var pubsubMessage = new PubsubMessage
        {
            Data = ByteString.CopyFrom(cloudEventData.ToArray())
        };

        TopicName topicName = TopicName.FromProjectTopic(_projectId, _topicId);
        PublisherClient publisher = await PublisherClient.CreateAsync(topicName);
        
        await publisher.PublishAsync(pubsubMessage);
    }
}
