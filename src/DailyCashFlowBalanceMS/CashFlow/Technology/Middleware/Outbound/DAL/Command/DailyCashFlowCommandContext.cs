using System;
using Microsoft.EntityFrameworkCore;
using DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.ORM; // 📌 Importante

namespace DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.Command;

public class DailyCashFlowCommandContext : DbContext
{
    private readonly string _connectionString;

    // 🌐 CORREÇÃO: Mapeia o ORM correto esperado pela linha 45 do repositório
    public DbSet<DailyCashFlowORM> CashFlowEntries { get; set; } = null!;

    public DailyCashFlowCommandContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseMySql(_connectionString, ServerVersion.AutoDetect(_connectionString));
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (string.Equals(property.Name, "rowversion", StringComparison.OrdinalIgnoreCase))
                {
                    property.SetColumnType("timestamp");
                }
            }
        }
    }
}
