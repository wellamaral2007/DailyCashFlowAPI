// File: apps/daily-cash-flow-service/src/DailyCashFlowService.API/3_Technology/Middleware/Outbound/DAL/Query/DurableDailyCashFlowDALQuery.cs
using System.Data.Entity;
using BaseFramework.DAL;
using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Query;

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
public class SQLDailyCashFlowDALQuery : AbstractCRUDDAL, IDailyCashFlowQueryPort
{
    private readonly DailyCashFlowQueryContext _context;

    /**
     * Dependency Injection via Segregated Entity Framework 6 Query DbContext Pool (IoC).
     * Instantiates the custom query context utilizing the lazy-loaded connection string 
     * pointing transparently to the Read-Only database replica network infrastructure.
     */
    public SQLDailyCashFlowDALQuery(string sqlConnectionString)
    {
        if (string.IsNullOrWhiteSpace(sqlConnectionString))
        {
            throw new ArgumentNullException(nameof(sqlConnectionString), "Database connection string cannot be null or empty inside the storage layer.");
        }

        // FIXED: Initializing the dedicated query context instance manually inside the constructor 
        // to correctly resolve the decoupled interface lifecycle instead of utilizing invalid ': base()' chains.
        _context = new DailyCashFlowQueryContext(sqlConnectionString);
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
