// File: apps/cashflow-service/src/CashFlow.API/2_Application/Contract/Command/IEventPublisherPort.cs
using DailyCashFlowBalanceMS.CashFlow.Domain.Entity;

namespace DailyCashFlowBalanceMS.CashFlow.Application.Contract.Command;

/**
 * TOGAF DIMENSION: Phase C - Application Architecture (Outbound Port)
 * HEXAGONAL ROLE:  Outbound Port - Event Driven Architecture (EDA)
 */
public interface IEventDailyCashFlowPublisherCommandPort
{
    public Task PublishDailyCashEventAsync(DailyCashFlowEntity DailyCashFlowEntity);
}
