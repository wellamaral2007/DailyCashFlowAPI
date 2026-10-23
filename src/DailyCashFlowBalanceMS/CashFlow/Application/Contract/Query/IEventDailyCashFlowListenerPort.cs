using DailyCashFlowBalanceMS.CashFlow.Domain.Entity;
using System.Threading.Tasks;

namespace DailyCashFlowBalanceMS.CashFlow.Application.Contract.Command;

/**
 * TOGAF DIMENSION: Phase C - Application Architecture (Inbound Port Alignment)
 * HEXAGONAL ROLE:  Inbound Driving Port - Event Driven Architecture (EDA)
 * CONCEPTS:        Asynchronous Message Consumption / Reactive Stream Listener
 */
public interface IEventDailyCashFlowListenerPort
{
    /**
     * Intercepts and processes an incoming financial transaction event from the message broker.
     */
    public Task ListenDailyCashEventAsync(DailyCashFlowEntity DailyCashFlowEntity);
}
