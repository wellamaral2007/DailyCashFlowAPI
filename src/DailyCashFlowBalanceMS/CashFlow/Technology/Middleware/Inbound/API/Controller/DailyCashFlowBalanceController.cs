using System.Net;
using DailyCashFlowBalanceMS.CashFlow.Application.Service;
using DailyCashFlowBalanceMS.CashFlow.Domain.Entity;
using Google.Cloud.Functions.Framework;

namespace DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Inbound.API.Controller;

/**
 * ============================================================================
 * TOGAF DIMENSION: Phase D - Technology Architecture (Inbound Middleware)
 * HEXAGONAL ROLE:  Inbound Driving Adapter (Google Cloud Function Trigger)
 * CONCEPTS:        FaaS (Function as a Service) / HTTP Web Delivery Mechanism
 * ============================================================================
 * This class handles incoming HTTP traffic at the cluster edge. It intercepts 
 * requests, validates headers through AOP, and triggers the decoupled application core.
 */
public class DailyCashFlowBalanceController : IHttpFunction
{
    private readonly DailyCashFlowBalanceService _dailyCashFlowService;

    /**
     * Dependency Injection enabled natively by the Google Cloud Functions Framework (IoC)
     */
    public DailyCashFlowBalanceController(DailyCashFlowBalanceService dailyCashFlowService)
    {
        _dailyCashFlowService = dailyCashFlowService;
    }

    /**
     * Entry point for handling serverless HTTP web requests safely inside the cluster topology.
     * Supports POST for throwing credit/debit entries and GET for fetching consolidated balance.
     */
    //[SecureGateway] [Observed] // AOP: Injected automatically from Shared Kernel NuGet for security & telemetry
    public async Task HandleAsync(HttpContext context)
    {
        HttpRequest request = context.Request;
        HttpResponse response = context.Response;

        // Ensuring standard enterprise JSON responses across the platform
        response.ContentType = "application/json";

        switch (request.Method)
        {
            case "POST": // Throw Credit or Debit financial movement to the Microservice
                var queryString = request.Query;
                
                // Safe extraction of query parameters from the HTTP request context
                if (queryString.TryGetValue("CreditDebitValue", out var creditDebitValueStr) && 
                    decimal.TryParse(creditDebitValueStr, out decimal parsedValue))
                {
                    // Generates unique identifiers and metadata for the new Domain Entity context
                    string transactionId = Guid.NewGuid().ToString();
                    DateTime currentTimestamp = DateTime.UtcNow;
                    
                    // Creates the Rich Domain Entity through the unified application service facade
                    var domainEntity = new DailyCashFlowEntity();
                    domainEntity.Id = transactionId;
                    domainEntity.CreditDebitValue = parsedValue;
                    domainEntity.ThowDate = currentTimestamp;
                    domainEntity.EventType = "throw";
                    // Drives the execution straight into the asynchronous application core (SAGA workflow trigger)
                    await _dailyCashFlowService.GenerateDailyCashEvent(domainEntity);

                    response.StatusCode = (int)HttpStatusCode.Created;
                    await response.WriteAsync($"{{\"message\": \"Financial event successfully triggered.\", \"transactionId\": \"{transactionId}\"}}", context.RequestAborted);
                }
                else
                {
                    // Validation policy defense at the architecture boundary for missing/corrupted arguments
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await response.WriteAsync("{\"error\": \"Missing or invalid mandatory query parameter 'CreditDebitValue'.\"}", context.RequestAborted);
                }
                break;

            case "GET": // Fetch consolidated net balance value from the resilient architecture
                decimal balanceValue = await _dailyCashFlowService.GetNetBalanceAsync(DateTime.UtcNow);
                
                response.StatusCode = (int)HttpStatusCode.OK;
                await response.WriteAsync($"{{\"balanceValue\": {balanceValue}}}", context.RequestAborted);
                break;

            default:
                // Rejection policy for unmapped HTTP Verbs at the architecture boundary
                response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                await response.WriteAsync("{\"error\": \"HTTP Method Not Allowed on this corporate resource.\"}", context.RequestAborted);
                break;
        }
    }

}
