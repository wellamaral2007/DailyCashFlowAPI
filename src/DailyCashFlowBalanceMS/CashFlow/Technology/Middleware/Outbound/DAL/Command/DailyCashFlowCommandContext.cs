using System.Data.Entity;
using DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.ORM;

namespace DailyCashFlowBalanceMS.DailyCashFlow.Technology.Middleware.Outbound.Dal;

/**
 * ============================================================================
 * TOGAF DIMENSION: Phase D - Technology Architecture (Outbound Infrastructure)
 * HEXAGONAL ROLE:  Durable Storage Command DBContext - Pointing to PRIMARY/WRITER Database (EF6)
 * CONCEPTS:        ACID Write Pipeline / Change Tracking Engine Enabled / Fluent API
 * ============================================================================
 * This class serves as the official persistent data gateway for state mutations.
 * It manages transactional write operations, ensuring strict data durability.
 */
public class DailyCashFlowCommandContext : DbContext
{
    /**
     * EF6 Constructor accepting the primary database connection string directly from IoC bootstrapping.
     */
    public DailyCashFlowCommandContext(string connectionString) : base(connectionString)
    {
        // Disables automatic schema initialization or migrations checks inside production containers
        Database.SetInitializer<DailyCashFlowCommandContext>(null);
    }

    /**
     * Represents the transactional dataset ledger mapping against physical database tables for writes.
     */
    public DbSet<DailyCashFlowORM> CashFlowEntries { get; set; } = null!;

    /**
     * Fluent API Boundary to enforce database typing, rowversion tokens, and precision constraints.
     */
    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureModelMapping(modelBuilder);
    }

    /**
     * Static mapping rules method shared to guarantee schema symmetry across database contexts.
     */
    public static void ConfigureModelMapping(DbModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DailyCashFlowORM>()
            .ToTable("tb_daily_cash_flow");

        modelBuilder.Entity<DailyCashFlowORM>()
            .HasKey(e => e.Id);

        modelBuilder.Entity<DailyCashFlowORM>()
            .Property(e => e.Id)
            .HasColumnName("id")
            .HasMaxLength(50)
            .IsRequired();

        // MANDATORY FINANCE POLICY: High-precision decimal constraints mapping to prevent calculation drifts
        modelBuilder.Entity<DailyCashFlowORM>()
            .Property(e => e.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();

        modelBuilder.Entity<DailyCashFlowORM>()
            .Property(e => e.TransactionDate)
            .HasColumnName("transaction_date")
            .IsRequired();

        // Optimistic Locking Token enforcing relational concurrency protection on state writes
        modelBuilder.Entity<DailyCashFlowORM>()
            .Property(e => e.Version)
            .IsRowVersion()
            .HasColumnName("version");
    }
}
