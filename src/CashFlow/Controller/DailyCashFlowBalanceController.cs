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
using Microsoft.Extensions.Primitives;


namespace CashFlow.Controller;

/*******************************
Controller Microservice with CLEAN
architecture pattern and SOLID
for classes and objects.
*******************************/
public class DailyCashFlowBalanceController : IHttpFunction, ICloudEventFunction<>
{

    //Staless service, transient 
    DailyCashFlowBalanceMS DailyCashFlowBalanceMS = new();

    /****************************************
    Handle HTTP requests of API for 
    microservice
    ****************************************/
    public async Task HandleAsync(HttpContext context)
    {
        HttpResponse response = context.Response;
        switch (context.Request.Method)
        {
            case "GET":
                double BalanceValue = DailyCashFlowBalanceMS.ObtainDailyBalance();
                response.StatusCode = (int) HttpStatusCode.OK;
                await response.WriteAsync("{balanceValue: " + BalanceValue + "}", context.RequestAborted);
                break;
            default:
                response.StatusCode = (int) HttpStatusCode.MethodNotAllowed;
                await response.WriteAsync("No Response!", context.RequestAborted);
                break;
        }
    }
    
}