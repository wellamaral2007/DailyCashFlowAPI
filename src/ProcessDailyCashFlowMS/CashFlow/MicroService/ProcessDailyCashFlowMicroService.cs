using BaseFramework.DAL.DailyCashFlow;
using BaseFramework.Event.DailyCashFlow;
using BaseFramework.MicroService;
using BaseFramework.MicroService.DailyCashFlow;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;

namespace ProcessDailyCashFlowMS.CashFlow.MicroService;
/*******************************
Microservice logic with SAGA Choreography
with EDA for high performance and
rely of solution
*******************************/
public class ProcessDailyCashFlowMicroService : AbstractMicroService, IProcessDailyCashFlowMS
{    
    private readonly IRedisDailyCashFlowDAL RedisDailyCashFlowDAL;
    private readonly ISQLDailyCashFlowDAL SQLDailyCashFlowDAL;
    public ProcessDailyCashFlowMicroService()
    {
        RedisDailyCashFlowDAL = new RedisDailyCashFlowDAL();
        SQLDailyCashFlowDAL = new SQLDailyCashFlowDAL();
    }



    /*******************************
    Called When Event of Topic is 
    throwCreditDebit for register 
    in relational Table
    *******************************/
    private void RegisterCreditDebit(DailyCashFlowEvent DailyCashFlowEvent)
    {
        SQLDailyCashFlowDAL.insertCreditDebit(DailyCashFlowEvent);
    }

   /*******************************
    According Event of Topic processing
    Event with SAGA for update
    BalanceValue after write the throw
    of Credit or Debit
    *******************************/
    private void UpdateBalance(DailyCashFlowEvent DailyCashFlowEvent)
    {
        RedisDailyCashFlowDAL.updateBalanceValue(DailyCashFlowEvent, DateTime.Now);
    }

   /*******************************
    Called When Event of Topic is 
    throw for update Key-Value of
    BalanceValue State in Redis
    *******************************/
    public void ProcessingDailyCashEvent(DailyCashFlowEvent DailyCashFlowEvent)
    {
        if (DailyCashFlowEvent.EventType.CompareTo("throw") == 0) 
        {
            RegisterCreditDebit(DailyCashFlowEvent); // register throw of credit or debit in relational table in sql database
            UpdateBalance(DailyCashFlowEvent); 
        }
    }

    private async Task SendProcessedEvent(DailyCashFlowEvent DailyCashFlowEvent)
    {
        /************************************************
         Thow throwEvent of credit or debit with signal increase or decrease 
         balance value for Pub/Sub Topic for other time processing
         for attempt high performance, throw registred event
        *************************************************/
        //throw new NotImplementedException();
        
        //DailyCashFlowEvent event;
        DailyCashFlowEvent dailyCashFlowEvent = new DailyCashFlowEvent();
        dailyCashFlowEvent.EventType = "registred";
        dailyCashFlowEvent.ThowDate = DailyCashFlowEvent.ThowDate;
        dailyCashFlowEvent.CreditDebitValue = DailyCashFlowEvent.CreditDebitValue;

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