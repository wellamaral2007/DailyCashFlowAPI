// File: apps/daily-cash-flow-service/src/DailyCashFlowBalanceMS/CashFlow/Technology/Middleware/Outbound/DAL/Query/DailyCashFlowQueryContext.cs
using System;
using Microsoft.EntityFrameworkCore;
using DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.ORM;

namespace DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.Query;

public class DailyCashFlowQueryContext : DbContext
{
    private readonly string _connectionString;

    // 🌐 TABELA MAPEADA: Resolve o erro CS1061 do repositório de leitura (linha 46)
    public DbSet<DailyCashFlowORM> CashFlowEntries { get; set; } = null!;

    public DailyCashFlowQueryContext(string connectionString)
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
