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

public class SQLDailyCashFlowDALQuery : AbstractCRUDDAL, IDailyCashFlowQueryPort
{
    // Receives the pure connection string from IoC and forwards it to the base infrastructure context
    public SQLDailyCashFlowDALQuery(string connectionString) : base(connectionString) { }

    // Automatic mapping of the physical table in the cloud MySQL via EF Core
    public DbSet<DailyCashFlowORM> CashFlowEntries { get; set; } = null!;

    public async Task<decimal> readBalanceValue(DateTime date)
    {
        decimal totalBalance = await this.CashFlowEntries
            .AsNoTracking()
            .Where(x => x.TransactionDate.Date == date.Date)
            .SumAsync(orm => orm.Amount);

        return Math.Round(totalBalance, 2, MidpointRounding.AwayFromZero);
    }
}
