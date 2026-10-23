using System;
using System.Threading.Tasks;

namespace DailyCashFlowBalanceMS.CashFlow.Application.Contract.Query;

/**
 * TOGAF DIMENSION: Phase C - Application Architecture (Outbound Port)
 * HEXAGONAL ROLE:  Outbound Driven Port - CQRS Durable Storage Read Fallback
 * CONCEPTS:        Relational Data Aggregation / Circuit Breaker Target Execution
 */
public interface IDurableDailyCashFlowQueryPort
{
    /**
     * Resolves the consolidated ledger balance value for a given date from the relational replica node.
     */
    public Task<decimal> readBalanceValue(DateTime date);
}
