using Microsoft.EntityFrameworkCore;
using BaseFramework.DAL;
using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Command;
using DailyCashFlowBalanceMS.CashFlow.Domain.Entity;
using DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.ORM;
using System.Threading.Tasks;

namespace DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.Command;

/**
 * ============================================================================
 * TOGAF DIMENSION: Phase D - Technology Architecture (Outbound Middleware)
 * HEXAGONAL ROLE:  Outbound Driven Adapter - Persistent Storage Command
 * CONCEPTS:        Relational Transaction Writes / Context Inversion
 * ============================================================================
 * This component encapsulates relational database persistence capabilities,
 * directly invoking structural AbstractCRUDDAL implementations without leaking context.
 */
public class SQLDailyCashFlowDALCommand : AbstractCRUDDAL, IDurableDailyCashFlowCommandPort
{
    /**
     * Initializes a new instance forwarding the verified connection string directly to the base framework context.
     */
    public SQLDailyCashFlowDALCommand(string connectionString) : base(connectionString) { }

    /**
     * Gets or sets the physical database table mapping configuration used by Entity Framework Core.
     */
    public DbSet<DailyCashFlowORM> CashFlowEntries { get; set; } = null!;

    /**
     * Maps the rich business domain entity model into physical schema definitions and persists into the database.
     */
    public async Task insertCreditDebit(DailyCashFlowEntity dailyCashFlowEvent)
    {
        var ormEntity = new DailyCashFlowORM
        {
            Id = dailyCashFlowEvent.Id,
            Amount = dailyCashFlowEvent.CreditDebitValue,
            TransactionDate = dailyCashFlowEvent.ThowDate
        };

        await this.AddAsync(ormEntity);
    }
}
