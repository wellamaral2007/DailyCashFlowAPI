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
public class GenerateDailyCashFlowController : IHttpFunction
{

    //Staless service, transient 
    GenerateDailyCashFlowMS GenerateDailyCashFlowMS = new();

    /****************************************
    Handle HTTP requests of API for 
    microservice
    ****************************************/
    public async Task HandleAsync(HttpContext context)
    {
        HttpResponse response = context.Response;
        switch (context.Request.Method)
        {
            case "POST": //Throw Credit or Debit to MS
                response.StatusCode = (int) HttpStatusCode.OK;
                //context.Request.QueryString
                var queryString = context.Request.Query;
                StringValues value;
                queryString.TryGetValue("CreditDebitValue", out value);
                ThowCreditDebit(double.Parse(value));
                break;
            default:
                response.StatusCode = (int) HttpStatusCode.MethodNotAllowed;
                await response.WriteAsync("No Response!", context.RequestAborted);
                break;
        }
    }

    public async Task ThowCreditDebit(Double BalanceValue)
    {
        GenerateDailyCashFlowMS.GenerateDailyCashEvent(BalanceValue);
    }
 
}