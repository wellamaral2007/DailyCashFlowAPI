using System;
using System.Net;
using System.Threading.Tasks;
using DailyCashFlowBalanceMS.CashFlow.MicroService;
using Google.Cloud.Functions.Framework;
using Google.Type;
using Microsoft.AspNetCore.Mvc;


namespace DailyCashFlowBalanceMS.CashFlow.Controller 
{

/*******************************
Controller Microservice with CLEAN
architecture pattern and SOLID
for classes and objects.
*******************************/
public class DailyCashFlowBalanceController : IHttpFunction
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

    /****************************************
    Handle HTTP requests of API for 
    microservice
    ****************************************/
    public async Task HandleAsync(HttpContext context) /* Google Cloud Function HTTP trigger for handling incoming HTTP requests */
    {
        HttpResponse response = context.Response;
        switch (context.Request.Method)
        {
            case "GET":
                decimal BalanceValue = await DailyCashFlowBalanceMS.ObtainDailyBalance(System.DateTime.Now);
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

}