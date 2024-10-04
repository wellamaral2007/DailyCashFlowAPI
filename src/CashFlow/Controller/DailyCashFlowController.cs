using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Google.Cloud.Functions.Framework;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Net;
using CloudNative.CloudEvents.Core;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using System;
using System.Threading.Tasks;
using CashFlow.MicroService;
using CashFlow.Event;


namespace CashFlow.Controller;

/*******************************
Controller Microservice with CLEAN
architecture pattern and SOLID
for classes and objects.
*******************************/
public class DailyCashFlowController : IHttpFunction, ICloudEventFunction<DailyCashFlowEvent>
{

    //Staless service, transient 
    DailyCashFlowMS DailyCashFlowMS = new();

    /****************************************
    Handle HTTP requests of API for 
    microservice
    ****************************************/
    public async Task HandleAsync(HttpContext context)
    {
        HttpResponse response = context.Response;
        switch (context.Request.Method)
        {
            case "PUT": //Throw Credit or Debit to MS
                response.StatusCode = (int) HttpStatusCode.OK;
                //context.Request.QueryString
                ThowCreditDebit(36.45);
                break;
            case "GET":
                double BalanceValue = DailyCashFlowMS.ObtainDailyBalance();
                response.StatusCode = (int) HttpStatusCode.OK;
                await response.WriteAsync("{balanceValue: " + BalanceValue + "}", context.RequestAborted);
                break;
            default:
                response.StatusCode = (int) HttpStatusCode.MethodNotAllowed;
                await response.WriteAsync("No Response!", context.RequestAborted);
                break;
        }
    }

    /****************************************
    Handle SAGA Choreography Events of 
    microservice
    ****************************************/
    public Task HandleAsync(CloudEvent cloudEvent, DailyCashFlowEvent data, CancellationToken cancellationToken)
    {
        //throw new NotImplementedException();
        return Task.CompletedTask;
    }

    
    public async Task ThowCreditDebit(Double BalanceValue)
    {
        /************************************************
         Thow throwEvent of credit or debit with signal increase or decrease 
         balance value for Pub/Sub Topic for other time processing
         for attempt high performance
        *************************************************/
        //throw new NotImplementedException();
        
        //DailyCashFlowEvent event;
        DailyCashFlowEvent dailyCashFlowEvent = new DailyCashFlowEvent();
        dailyCashFlowEvent.EventType = "Throw";
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