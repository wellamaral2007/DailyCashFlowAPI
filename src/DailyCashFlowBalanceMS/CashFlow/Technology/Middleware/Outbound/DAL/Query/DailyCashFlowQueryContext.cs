using System.Data.Entity;
using DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.ORM;

namespace DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.Query;

/**
 * HEXAGONAL ROLE: Durable Storage Query DBContext - READ-ONLY REPLICA (EF6)
 */
public class DailyCashFlowQueryContext : DbContext
{
    public DailyCashFlowQueryContext(string connectionString) : base(connectionString)
    {
        Database.SetInitializer<DailyCashFlowQueryContext>(null);
        
        // MAXIMUM PERFORMANCE TUNING: Disables proxy creation and change tracking natively in EF6
        this.Configuration.ProxyCreationEnabled = false;
        this.Configuration.AutoDetectChangesEnabled = false;
    }

    public DbSet<DailyCashFlowORM> CashFlowEntries { get; set; } = null!;

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<DailyCashFlowORM>().Property(e => e.Amount).HasPrecision(18, 2);
    }
}
