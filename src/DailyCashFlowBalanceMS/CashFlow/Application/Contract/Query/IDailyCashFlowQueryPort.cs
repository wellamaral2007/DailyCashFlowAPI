using System;
using System.Threading.Tasks;

namespace DailyCashFlowBalanceMS.CashFlow.Application.Contract.Query;

/**
 * TOGAF DIMENSION: Phase C - Application Architecture (Outbound Port)
 * HEXAGONAL ROLE:  Outbound Driven Port - CQRS High Performance Read Pipeline
 * CONCEPTS:        Query Segregation / Resilient Cache-Aside Pattern
 */
public interface IDailyCashFlowQueryPort
{
    /**
     * Resolves the consolidated ledger balance value for a given date from the cache store.
     */
    public Task<decimal> readBalanceValue(DateTime date);
}
