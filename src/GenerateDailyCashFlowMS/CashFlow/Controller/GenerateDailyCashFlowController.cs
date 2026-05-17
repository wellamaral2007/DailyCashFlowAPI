using System;
using System.Net;
using GenerateDailyCashFlowMS.CashFlow.MicroService;
using Google.Cloud.Functions.Framework;
using Microsoft.Extensions.Primitives;


namespace GenerateDailyCashFlowMS.CashFlow.Controller;

/*******************************
Controller Microservice with CLEAN
architecture pattern and SOLID
for classes and objects.
*******************************/

public class GenerateDailyCashFlowController : IHttpFunction  /* Google Cloud Function HTTP trigger for handling incoming HTTP requests */
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
                await ThowCreditDebit(decimal.Parse(value));
                break;
            default:
                response.StatusCode = (int) HttpStatusCode.MethodNotAllowed;
                await response.WriteAsync("No Response!", context.RequestAborted);
                break;
        }
    }

    public async Task ThowCreditDebit(decimal BalanceValue)
    {
        GenerateDailyCashFlowMS.GenerateDailyCashEvent(BalanceValue);
    }
 
}