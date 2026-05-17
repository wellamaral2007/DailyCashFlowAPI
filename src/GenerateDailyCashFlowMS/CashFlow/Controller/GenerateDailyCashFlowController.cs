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
using GenerateDailyCashFlowMS.CashFlow.MicroService;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Mvc;


namespace GenerateDailyCashFlowMS.CashFlow.Controller;

/*******************************
Controller Microservice with CLEAN
architecture pattern and SOLID
for classes and objects.
*******************************/
[ApiController] // Enables automatic model validation and other API-specific behaviors
[Route("[controller]")] // Sets the route to /GenerateDailyCashFlow
public class GenerateDailyCashFlowController : ControllerBase
{

    private readonly GenerateDailyCashFlowMicroService GenerateDailyCashFlowMS;
    public GenerateDailyCashFlowController()
    {
        //Staless service, transient 
        GenerateDailyCashFlowMS = new GenerateDailyCashFlowMicroService();
    }

    /****************************************
    Handle HTTP requests of API for 
    microservice
    ****************************************/
    
    [HttpPost]
    public async Task<ActionResult> PostAsync([FromBody] string thowValue)
    {
        await ThowCreditDebit(decimal.Parse(thowValue));
        return Ok();
    }

    public async Task ThowCreditDebit(decimal BalanceValue)
    {
        GenerateDailyCashFlowMS.GenerateDailyCashEvent(BalanceValue);
    }
 
}