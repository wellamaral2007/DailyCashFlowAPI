using Microsoft.EntityFrameworkCore;
using BaseFramework.DAL;
using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Command;
using DailyCashFlowBalanceMS.CashFlow.Domain.Entity;
using DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.ORM;

namespace DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.Command;

public class SQLDailyCashFlowDALCommand : AbstractCRUDDAL, IDurableDailyCashFlowCommandPort
{
    // Receives the pure connection string from IoC and forwards it to the base infrastructure context
    public SQLDailyCashFlowDALCommand(string connectionString) : base(connectionString) { }

    // EF Core will read this property and automatically map the target physical table
    public DbSet<DailyCashFlowORM> CashFlowEntries { get; set; } = null!;

    public async Task insertCreditDebit(DailyCashFlowEntity dailyCashFlowEvent)
    {
        // Maps the rich domain model from the Core to the physical database ORM layout
        var ormEntity = new DailyCashFlowORM
        {
            Id = dailyCashFlowEvent.Id,
            Amount = dailyCashFlowEvent.CreditDebitValue,
            TransactionDate = dailyCashFlowEvent.ThowDate
        };

        // Triggers the asynchronous insert inherited natively and transparently from AbstractCRUDDAL
        await this.AddAsync(ormEntity);
    }
}
