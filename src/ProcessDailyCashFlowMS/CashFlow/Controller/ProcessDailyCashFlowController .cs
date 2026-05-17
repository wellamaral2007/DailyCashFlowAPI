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
using Microsoft.Extensions.Primitives;
using BaseFramework.Event.DailyCashFlow;
using ProcessDailyCashFlowMS.CashFlow.MicroService;


namespace CashFlow.Controller;

/****************************************
 Pub/Sub listen to process event of throw 
credit or debit for balance value
****************************************/
public class ProcessDailyCashFlowController: ICloudEventFunction<DailyCashFlowEvent>
{

    //Staless service, transient 
    ProcessDailyCashFlowMicroService ProcessDailyCashFlowMS;

    public ProcessDailyCashFlowController()
    {
        ProcessDailyCashFlowMS = new ProcessDailyCashFlowMicroService();
    }

    /****************************************
    Handle SAGA Choreography Events of 
    microservice, process event of throw 
    credit or debit for balance value
    ****************************************/
    public Task HandleAsync(CloudEvent cloudEvent, DailyCashFlowEvent data, CancellationToken cancellationToken)
    {
        //throw new NotImplementedException();
        
        if (data.EventType.Equals("throw"))
            ProcessDailyCashFlowMS.ProcessingDailyCashEvent(data);
            //SendProcessedEvent(data); can notify in pub/sub topic with processed event
        return Task.CompletedTask;
    }

}