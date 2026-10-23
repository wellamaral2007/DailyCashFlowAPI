using DailyCashFlowBalanceMS.CashFlow.Domain.Entity;
using System.Threading.Tasks;

namespace DailyCashFlowBalanceMS.CashFlow.Application.Contract.Command;

/**
 * TOGAF DIMENSION: Phase C - Application Architecture (Outbound Port)
 * HEXAGONAL ROLE:  Outbound Driven Port - Event Driven Architecture (EDA)
 * CONCEPTS:        Asynchronous Message Ingestion / Contract Inversion
 */
public interface IEventDailyCashFlowPublisherCommandPort
{
    /**
     * Dispatches a financial transaction event to the central cloud messaging broker.
     */
    public Task PublishDailyCashEventAsync(DailyCashFlowEntity DailyCashFlowEntity);
}
