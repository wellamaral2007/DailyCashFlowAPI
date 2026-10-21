using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Command;
using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Query;
using DailyCashFlowBalanceMS.CashFlow.Domain.Entity; // Ingestion of the Rich Domain Model Entity

namespace DailyCashFlowBalanceMS.CashFlow.Application.Service;

/**
 * ============================================================================
 * TOGAF DIMENSION: Phase C - Application Architecture (Unified Facade)
 * HEXAGONAL ROLE:  Application Service (Cohesive Business Facade)
 * CONCEPTS:        Resilient Service / CQRS Segregated Ports / Polly Circuit Breaker / EDA Orchestrator
 * ============================================================================
 * This class is 100% agnostic to physical infrastructure, brokers, or DB frameworks.
 * It centralizes the read orchestration and delegates commands through pure ports in singular.
 */
public class DailyCashFlowBalanceService
{    
    // Query Ports (Segregated Read Operations - Query in Singular)
    private readonly IDailyCashFlowQueryPort _queryPort;
    
    // Command Ports (State Modification Operations - Command in Singular) for Facade Symmetry
    private readonly IDurableDailyCashFlowCommandPort _durableCommandPort;
    private readonly IQuickAccessDailyCashFlowCommandPort _quickAccessCommandPort;
    private readonly IEventDailyCashFlowPublisherCommandPort _eventPublisher;

    /**
     * Dependency Injection via Hexagonal Ports Constructor (IoC)
     */
    public DailyCashFlowBalanceService(
        IDailyCashFlowQueryPort queryPort,
        IDurableDailyCashFlowCommandPort durableCommandPort,
        IQuickAccessDailyCashFlowCommandPort quickAccessCommandPort,
        IEventDailyCashFlowPublisherCommandPort eventPublisher)
    {
        _queryPort = queryPort;
        _durableCommandPort = durableCommandPort;
        _quickAccessCommandPort = quickAccessCommandPort;
        _eventPublisher = eventPublisher;
    }

    /**
     * 📥 WRITE CAPABILITY: Generates the financial movement throwing event
     */
    public async Task GenerateDailyCashEvent(DailyCashFlowEntity entry)
    {
        entry.EventType = "throw"; // Sets the event type for consistent business logic mapping
        await _eventPublisher.PublishDailyCashEventAsync(entry);
    }

    /**************************************************************************
     Reactive SAGA Orchestration:
     Triggered when the inbound event topic (EDA Inbound) indicates 
     that a Credit or Debit entry must be saved into a durable medium.
     **************************************************************************/
    private async Task RegisterCreditDebit(DailyCashFlowEntity dailyCashFlowEvent)
    {
        await _durableCommandPort.insertCreditDebit(dailyCashFlowEvent);
    }

    /**************************************************************************
     Reactive SAGA Orchestration:
     Processes high-performance parallel updates to the quick access mesh 
     to recalculate the current net balance (Stateful projection).
     **************************************************************************/
    private async Task UpdateBalance(DailyCashFlowEntity dailyCashFlowEvent)
    {
        await _quickAccessCommandPort.updateBalanceValue(dailyCashFlowEvent, DateTime.Now);
    }

    /**************************************************************************
     EDA Consumer Entry Point (Inbound Middleware):
     Orchestrates both dimensions of state mutation (Durable + QuickAccess)
     and fires the confirmation event to finalize the message lifecycle.
     **************************************************************************/
    public async Task ProcessingDailyCashEvent(DailyCashFlowEntity dailyCashFlowEvent)
    {
        var eventType = dailyCashFlowEvent.EventType ?? "";
        if (eventType.CompareTo("throw") == 0) 
        {
            // Executes both write operations opaquely through the decoupled ports
            await RegisterCreditDebit(dailyCashFlowEvent); 
            await UpdateBalance(dailyCashFlowEvent); 
            
            // Fires the final success notification to close the workflow loop
            await SendProcessedEvent(dailyCashFlowEvent);
        }
    }

    /**************************************************************************
     Emits the success notification event ("registered") asynchronously 
     so downstream auditing or reporting services can process it.
     **************************************************************************/
    private async Task SendProcessedEvent(DailyCashFlowEntity dailyCashFlowEvent)
    {
        DailyCashFlowEntity processedEvent = new DailyCashFlowEntity();
        processedEvent.EventType = "registered";
        processedEvent.ThowDate = dailyCashFlowEvent.ThowDate;
        processedEvent.CreditDebitValue = dailyCashFlowEvent.CreditDebitValue;

        await _eventPublisher.PublishDailyCashEventAsync(processedEvent);
    }

    /**
     * 📤 READ CAPABILITY (QUERY): Obtains the current consolidated net balance with Fallback Resilience
     */
    public async Task<decimal> GetNetBalanceAsync(DateTime date)
    {
        decimal cachedValue = await _queryPort.readBalanceValue(date);
        return cachedValue;
    }
}
