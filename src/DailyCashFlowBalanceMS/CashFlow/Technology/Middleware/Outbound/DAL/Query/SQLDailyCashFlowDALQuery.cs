using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseFramework.DAL;
using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Query;
using DailyCashFlowBalanceMS.CashFlow.Domain.Entity;
using DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.ORM;
using Microsoft.EntityFrameworkCore;

namespace DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.Query;

/**
 * ============================================================================
 * TOGAF DIMENSION: Phase D - Technology Architecture (Outbound Middleware)
 * HEXAGONAL ROLE:  Outbound Driven Adapter - Persistent Storage Query
 * CONCEPTS:        Relational Data Aggregation / CQRS Read Pipeline Fallback
 * ============================================================================
 * This component encapsulates relational database read operations from MySQL,
 * executing optimized synchronous time-windowed ledger sum aggregations.
 */
public class SQLDailyCashFlowDALQuery : AbstractCRUDDAL, IDailyCashFlowQueryPort
{
    /**
     * Initializes a new instance forwarding the verified connection string directly to the base framework context.
     */
    public SQLDailyCashFlowDALQuery(string connectionString) : base(connectionString) { }

    /**
     * Gets or sets the physical database table mapping configuration used by Entity Framework Core.
     */
    public DbSet<DailyCashFlowORM> CashFlowEntries { get; set; } = null!;

    /**
     * Aggregates and calculates the consolidated net balance for a given date directly from relational replicas.
     */
    public async Task<decimal> readBalanceValue(DateTime date)
    {
        decimal totalBalance = await this.CashFlowEntries
            .AsNoTracking()
            .Where(x => x.TransactionDate.Date == date.Date)
            .SumAsync(orm => orm.Amount);

        return Math.Round(totalBalance, 2, MidpointRounding.AwayFromZero);
    }
}
