using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Threading.Tasks;
using CashFlow.MicroService;

namespace CashFlow.Controller;

public class DailyCashFlowController : IHttpFunction
{

    //Staless service, transient 
    DailyCashFlowMS DailyCashFlowMS = new();

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

    public void ThowCreditDebit(Double BalanceValue)
    {
        /************************************************
         Thow throwEvent of credit or debit with signal increase or decrease 
         balance value for Pub/Sub Topic for other time processing
         for attempt high performance
        *************************************************/
        //throw new NotImplementedException();
    }
}