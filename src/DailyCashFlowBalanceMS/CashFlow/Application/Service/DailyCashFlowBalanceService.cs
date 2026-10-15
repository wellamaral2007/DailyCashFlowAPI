using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Command;
using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Query;
using DailyCashFlowBalanceMS.CashFlow.Domain.Entity; // Ingestion of the Rich Domain Model Entity
using Polly;
using Polly.CircuitBreaker;

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
    private readonly IQuickAccessDailyCashFlowQueryPort _quickAccessQueryPort;
    private readonly IDurableDailyCashFlowQueryPort _durableQueryPort;
    
    // Command Ports (State Modification Operations - Command in Singular) for Facade Symmetry
    private readonly IDurableDailyCashFlowCommandPort _durableCommandPort;
    private readonly IQuickAccessDailyCashFlowCommandPort _quickAccessCommandPort;
    private readonly IEventDailyCashFlowPublisherCommandPort _eventPublisher;

    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;

    /**
     * Dependency Injection via Hexagonal Ports Constructor (IoC)
     */
    public DailyCashFlowBalanceService(
        IQuickAccessDailyCashFlowQueryPort quickAccessQueryPort,
        IDurableDailyCashFlowQueryPort durableQueryPort,
        IDurableDailyCashFlowCommandPort durableCommandPort,
        IQuickAccessDailyCashFlowCommandPort quickAccessCommandPort,
        IEventDailyCashFlowPublisherCommandPort eventPublisher)
    {
        _quickAccessQueryPort = quickAccessQueryPort;
        _durableQueryPort = durableQueryPort;
        _durableCommandPort = durableCommandPort;
        _quickAccessCommandPort = quickAccessCommandPort;
        _eventPublisher = eventPublisher;
        
        // Resilience Governance Configuration: Breaks the circuit after 3 consecutive failures, waits 30 seconds
        _circuitBreaker = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (ex, timespan) => 
                { 
                    Console.WriteLine($"[DAILY CASH FLOW CIRCUIT BREAKER] Opened! Reason: {ex.Message}. Routing traffic directly to Durable storage."); 
                },
                onReset: () => 
                { 
                    Console.WriteLine("[DAILY CASH FLOW CIRCUIT BREAKER] Closed! QuickAccess middleware is back online."); 
                }
            ); 
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
        if (dailyCashFlowEvent.EventType.CompareTo("throw") == 0) 
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
        try
        {
            // Executes the data fetch protected by the Circuit Breaker targeting the QuickAccess Port
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                try
                {
                    // 1. Tries to retrieve the value immediately from the QuickAccess Query Port
                    decimal cachedValue = await _quickAccessQueryPort.readBalanceValue(DateTime.Now);
                    return cachedValue;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[QUICK ACCESS ERROR] {ex.Message}. Tripping Circuit Breaker and routing to Durable Query Port.");
                    throw; // Rethrow to inform Polly about the failure and update the circuit state
                }
            });
        }
        catch (BrokenCircuitException)
        {
            // 2. Critical Fallback: Circuit is broken. Bypasses QuickAccess completely and targets the Durable Query Port directly
            Console.WriteLine("[FALLBACK CRITICAL] Circuit is broken. Bypassing QuickAccess and querying Durable Query Port directly.");
            return await _durableQueryPort.readBalanceValue(DateTime.Now);
        }
    }
}
