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
using BaseFramework.MicroService.DailyCashFlow;
using DailyCashFlowBalanceMS.CashFlow.MicroService;
using Microsoft.AspNetCore.Mvc;


namespace DailyCashFlowBalanceMS.CashFlow.Controller;


/*******************************
Controller Microservice with CLEAN
architecture pattern and SOLID
for classes and objects.
*******************************/
[ApiController] // Enables automatic model validation and other API-specific behaviors
[Route("[controller]")] // Sets the route to /DailyCashFlowBalance
public class DailyCashFlowBalanceController : ControllerBase
{
    private readonly DailyCashFlowBalanceMicroService DailyCashFlowBalanceMS;
    public DailyCashFlowBalanceController()
    {
        //Staless service, transient 
        DailyCashFlowBalanceMS = new DailyCashFlowBalanceMicroService();
    }

    /****************************************
    Handle HTTP requests of API for 
    microservice
    ****************************************/
    [HttpGet]
    public async Task<ActionResult<decimal>> GetAsync()
    {
        decimal BalanceValue = await DailyCashFlowBalanceMS.ObtainDailyBalance(DateTime.Now);
        return Ok(BalanceValue);
    }
    
}