

using BaseFramework.DAL.DailyCashFlow;
using BaseFramework.Event.DailyCashFlow;
using BaseFramework.MicroService;
using BaseFramework.MicroService.DailyCashFlow;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;

namespace GenerateDailyCashFlowMS.CashFlow.MicroService;

/*******************************
Microservice logic with SAGA Choreography
with EDA for high performance and
rely of solution
*******************************/
public class GenerateDailyCashFlowMicroService : AbstractMicroService, IGenerateDailyCashFlowMS
{    

    private readonly ISQLDailyCashFlowDAL SQLDailyCashFlowDAL;

    public GenerateDailyCashFlowMicroService()
    {
        SQLDailyCashFlowDAL = new SQLDailyCashFlowDAL();
    }

    public async Task GenerateDailyCashEvent(decimal BalanceValue) 
    {
         /************************************************
         Thow throwEvent of credit or debit with signal increase or decrease 
         balance value for Pub/Sub Topic for other time processing
         for attempt high performance
        *************************************************/
        
        //DailyCashFlowEvent event;
        DailyCashFlowEvent dailyCashFlowEvent = new DailyCashFlowEvent();
        dailyCashFlowEvent.EventType = "throw";
        dailyCashFlowEvent.ThowDate = DateTime.Now;
        dailyCashFlowEvent.CreditDebitValue = BalanceValue;

        var cloudEvent = new CloudEvent
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri("//my-source"),
            Type = "CashFlow.Event.DailyCashFlowEvent",
            DataContentType = "application/json",
            Data = dailyCashFlowEvent
        };

        // Serialize the CloudEvent to JSON
        var jsonFormatter = new JsonEventFormatter();
        var cloudEventData = jsonFormatter.EncodeStructuredModeMessage(cloudEvent, out var contentType);

        // Create a Pub/Sub message
        var pubsubMessage = new PubsubMessage
        {
            Data = ByteString.CopyFrom(cloudEventData.ToArray())
        };

        // Publish the message to a Pub/Sub topic
        var publisher = await PublisherClient.CreateAsync(
            TopicName.FromProjectTopic("daily-cash-flow-api-project", "api-daily-cash-flow-event-topic")
            );
        await publisher.PublishAsync(pubsubMessage); 

        //Console.WriteLine("CloudEvent published to Pub/Sub.");
    
    }

}