// File: apps/daily-cash-flow-service/src/DailyCashFlowService.API/3_Technology/Middleware/Outbound/DAL/Query/DurableDailyCashFlowDALQuery.cs
using System.Data.Entity;
using BaseFramework.DAL;
using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Query;
using DailyCashFlowBalanceMS.DailyCashFlow.Technology.Middleware.Outbound.Dal;

namespace DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.Query;

/**
 * ============================================================================
 * TOGAF DIMENSION: Phase D - Technology Architecture (Outbound Middleware)
 * HEXAGONAL ROLE:  Outbound Driven Adapter - Persistent/Durable Query
 * CONCEPTS:        High Performance Scaler Projections / EF6 NoTracking Read Engine
 * ============================================================================
 * This infrastructure component handles heavy persistent state read aggregations
 * for .NET 8.0, executing ultra-fast calculations against the database query replica.
 */
public class SQLDailyCashFlowDALQuery : AbstractCRUDDAL, IDurableDailyCashFlowQueryPort
{
    private readonly DailyCashFlowQueryContext _context;

    /**
     * Dependency Injection via Segregated Entity Framework 6 Query DbContext Pool (IoC).
     * Connects transparently to the Read-Only replica database instance infrastructure.
     */
    public SQLDailyCashFlowDALQuery(DailyCashFlowQueryContext context)
    {
        _context = context;
    }

    /**
     * Computes the aggregated sum value of all transactions matching a specific date target.
     * Bypasses memory tracing tracking mechanisms natively to optimize container resources.
     */
    public async Task<decimal> readBalanceValue(DateTime date)
    {
        // Executes the SQL SUM aggregation directly into the relational database engine.
        // Due to the QueryContext configurations, this statement runs with zero allocation tracing overhead.
        var aggregatedSum = await _context.CashFlowEntries
            .Where(x => DbFunctions.TruncateTime(x.TransactionDate) == date.Date)
            .SumAsync(x => (decimal?)x.Amount);

        // Handles DBNull or empty datasets gracefully returning absolute decimal zero values
        return aggregatedSum ?? 0.0m;
    }
}
