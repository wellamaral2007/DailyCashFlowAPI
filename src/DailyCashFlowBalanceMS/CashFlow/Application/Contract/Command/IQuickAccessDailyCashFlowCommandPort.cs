using DailyCashFlowBalanceMS.CashFlow.Domain.Entity;
using System;
using System.Threading.Tasks;

namespace DailyCashFlowBalanceMS.CashFlow.Application.Contract.Command;

/**
 * TOGAF DIMENSION: Phase C - Application Architecture (Outbound Port)
 * HEXAGONAL ROLE:  Outbound Driven Port - Fast Access Memory Cache Invalidation
 * CONCEPTS:        In-Memory State Mutation / Volatile Storage Decoupling
 */
public interface IQuickAccessDailyCashFlowCommandPort
{
    /**
     * Updates the volatile cached ledger balance state inside the database cluster.
     */
    public Task updateBalanceValue(DailyCashFlowEntity DailyCashFlowEntity, DateTime date);
}
