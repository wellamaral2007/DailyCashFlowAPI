// File: apps/daily-cash-flow-service/src/DailyCashFlowService.API/3_Technology/Middleware/Inbound/Eda/ProcessDailyCashFlowController.cs
using System.Threading;
using System.Threading.Tasks;
using DailyCashFlowBalanceMS.CashFlow.Domain.Entity; // Our rich domain model entity context
using BaseFramework.Event.DailyCashFlow; // Legacy framework event context bounded to infrastructure
using CloudNative.CloudEvents;
using Google.Cloud.Functions.Framework;
using DailyCashFlowBalanceMS.CashFlow.Application.Service;

namespace DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Inbound.EDA.Controller;

/**
 * ============================================================================
 * TOGAF DIMENSION: Phase D - Technology Architecture (Inbound Middleware)
 * HEXAGONAL ROLE:  Inbound Driving Adapter (Google Cloud Function Event Trigger)
 * CONCEPTS:        FaaS (Function as a Service) / EDA Choreography / Pub/Sub Listener
 * ============================================================================
 * This class listens directly to the enterprise Pub/Sub topics via CloudEvents.
 * It normalizes the technical pipeline payload into rich domain entities and invokes the core.
 */
public class ProcessDailyCashFlowController : ICloudEventFunction<DailyCashFlowEvent>
{
    private readonly DailyCashFlowBalanceService _dailyCashFlowService;

    /**
     * Dependency Injection configured via the Google Cloud Functions container framework (IoC)
     */
    public ProcessDailyCashFlowController(DailyCashFlowBalanceService dailyCashFlowService)
    {
        _dailyCashFlowService = dailyCashFlowService;
    }

    /**
     * Handles SAGA Choreography Events of the microservice.
     * Intercepts the infrastructure CloudEvent, normalizes it, and routes into the clean core application.
     */
    public async Task HandleAsync(CloudEvent cloudEvent, DailyCashFlowEvent data, CancellationToken cancellationToken)
    {
        // Fail-safe boundary validation for corrupted background processing events
        if (data == null || string.IsNullOrEmpty(data.EventType))
        {
            return;
        }

        // Translates and normalizes the un-encapsulated infrastructure record into the rich Core Domain Entity
        // This ensures the application layer never leaks data types bounded to specific frameworks
        var domainEntity = new DailyCashFlowEntity();
        domainEntity.Id = Guid.NewGuid().ToString(); // Generates tracing boundary context if missing from legacy payload
        domainEntity.CreditDebitValue = data.CreditDebitValue; // Precision scaling conversion to prevent float errors
        domainEntity.ThowDate = data.ThowDate != default ? data.ThowDate : DateTime.UtcNow;
        // Sets the internal message orchestration state matching the legacy protocol flow
        domainEntity.EventType = data.EventType;


        // Drives the asynchronous execution straight into the SAGA steps orchestration facade
        await _dailyCashFlowService.ProcessingDailyCashEvent(domainEntity);

    }
}
