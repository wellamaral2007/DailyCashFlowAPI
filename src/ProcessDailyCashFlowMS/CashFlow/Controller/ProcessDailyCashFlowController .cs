using System.Threading; 
using BaseFramework.Event.DailyCashFlow;
using CloudNative.CloudEvents;
using Google.Cloud.Functions.Framework;
using ProcessDailyCashFlowMS.CashFlow.MicroService;


namespace CashFlow.Controller;

/****************************************
 Pub/Sub listen to process event of throw 
credit or debit for balance value
****************************************/
public class ProcessDailyCashFlowController: ICloudEventFunction<DailyCashFlowEvent> /* Google Cloud Function CloudEvent listen Pub/Sub for processing event of entry credit or debit for balance value */
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