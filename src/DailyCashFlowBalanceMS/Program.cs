// File: apps/daily-cash-flow-service/src/DailyCashFlowBalanceMS/Program.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore; // 📌 Adicionado para habilitar o .UseMySql() do EF Core
using StackExchange.Redis;
using Google.Cloud.Functions.Hosting; // 🌐 Mapeamento do host do Functions Framework
using DailyCashFlowBalanceMS.CashFlow.Application.Service;
using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Command;
using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Query;
using DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.Command;
using DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.Query;
using BaseFramework.Technology.Util;
using DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.EDA.Controller;

// ============================================================================
// 🌐 GOOGLE FUNCTIONS FRAMEWORK BOOTSTRAP HOOK
// ============================================================================
// Este atributo registra esta classe no ciclo de vida de inicialização FaaS da Google
[assembly: FunctionsStartup(typeof(DailyCashFlowBalanceMS.ProgramStartup))]

namespace DailyCashFlowBalanceMS;

public class ProgramStartup : FunctionsStartup
{
    public override void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
    {
        services.AddSingleton<CloudNative.CloudEvents.CloudEventFormatter>(new CloudNative.CloudEvents.SystemTextJson.JsonEventFormatter());
        string projectId = "daily-cashflow-sol-17852";
        
        // Captura o ambiente atual mapeado no seu launch.json (Development)
        string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        bool isDevelopment = environment.Equals("Development", StringComparison.OrdinalIgnoreCase);

        // ============================================================================
        // 🛡️ LAZY FACTORY INJECTION: Eliminating network calls during boot phase
        // ============================================================================

        // 1. StackExchange.Redis Connection Pool initialized ONLY when first requested by the ports
        services.AddSingleton<IConnectionMultiplexer>(sp => 
        {
            try
            {
                string redisConnectionString = isDevelopment 
                    ? (Environment.GetEnvironmentVariable("ConnectionStrings__redis-connection") ?? Environment.GetEnvironmentVariable("redis-connection"))
                    : VaultCredentialUtility.GetSecretAsync(projectId, "redis-connection").GetAwaiter().GetResult();
                    
                return ConnectionMultiplexer.Connect(redisConnectionString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL RUNTIME] Redis Initialization Failed: {ex.Message}");
                throw;
            }
        });

        // 2. Entity Framework 6 Command Context (Write Pipeline)
        services.AddScoped<DailyCashFlowCommandContext>(sp => 
        {
            string writeSqlConnectionString = isDevelopment
                ? (Environment.GetEnvironmentVariable("ConnectionStrings__db-write-connection") ?? Environment.GetEnvironmentVariable("DATABASE"))
                : VaultCredentialUtility.GetSecretAsync(projectId, "db-write-connection").GetAwaiter().GetResult();

            return new DailyCashFlowCommandContext(writeSqlConnectionString);
        });

        // 3. Entity Framework 6 Query Context (Read Pipeline)
        services.AddScoped<DailyCashFlowQueryContext>(sp => 
        {
            string readSqlConnectionString = isDevelopment
                ? (Environment.GetEnvironmentVariable("ConnectionStrings__db-read-connection"))
                : VaultCredentialUtility.GetSecretAsync(projectId, "db-read-connection").GetAwaiter().GetResult();

            return new DailyCashFlowQueryContext(readSqlConnectionString);
        });

        // ============================================================================
        // Hexagonal Architecture: Wiring Driven Adapters (Middleware) to Outbound Ports
        // ============================================================================

        // 4. Register the concrete SQL durable query repository factory passing down the connection configuration
        services.AddScoped<SQLDailyCashFlowDALQuery>(sp => 
        {
            string readSqlConnectionString = isDevelopment
                ? (Environment.GetEnvironmentVariable("ConnectionStrings__db-read-connection") ?? Environment.GetEnvironmentVariable("db-read-connection"))
                : VaultCredentialUtility.GetSecretAsync(projectId, "db-read-connection").GetAwaiter().GetResult();
                
            return new SQLDailyCashFlowDALQuery(readSqlConnectionString);
        });

        // 5. Mapping Persistent Durable Command and Query Ports to their respective repositories
        services.AddScoped<IDurableDailyCashFlowCommandPort, SQLDailyCashFlowDALCommand>();

        // 6. 📌 ENCHAINED WIRING: Resolves the main read port mapping by fetching the Singleton Redis connection
        services.AddScoped<IDailyCashFlowQueryPort, RedisDailyCashFlowDALQuery>(sp => 
        {
            var redisMultiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
            var sqlDALQuery = sp.GetRequiredService<SQLDailyCashFlowDALQuery>();
            return new RedisDailyCashFlowDALQuery(redisMultiplexer, sqlDALQuery);
        });

        // 7. Mapping In-Memory QuickAccess Command Ports to Redis separate repositories
        services.AddScoped<IQuickAccessDailyCashFlowCommandPort, RedisDailyCashFlowDALCommand>();

        // Mapping the abstract core outbound port to the unified controller class implementing it
        services.AddScoped<IEventDailyCashFlowPublisherCommandPort, GenerateDailyCashFlowController>();

        // Application Core Services Mapping (The Hexagon Core Facade)
        services.AddScoped<DailyCashFlowBalanceService>();
    }

    public override void Configure(WebHostBuilderContext context, IApplicationBuilder app)
    {
        // Middleware HTTP Pipeline Request Lifecycles Activation incorporado no pipeline do Functions Framework
        app.UseMiddleware<BaseFramework.Technology.Util.GlobalErrorHandlingMiddleware>();
    }
}
