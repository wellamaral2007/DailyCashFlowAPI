using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Google.Cloud.Functions.Hosting; // 🌐 Benefit from Google's native FaaS initialization host
using DailyCashFlowBalanceService = DailyCashFlowBalanceMS.CashFlow.Application.Service.DailyCashFlowBalanceService;
using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Command;
using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Query;
using DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.Command;
using DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.Query;
using BaseFramework.Technology.Util;
using DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.EDA.Controller;

[assembly: FunctionsStartup(typeof(DailyCashFlowBalanceMS.ProgramStartup))]

namespace DailyCashFlowBalanceMS;

public class ProgramStartup : FunctionsStartup
{
    public override void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
    {
        string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        bool isDevelopment = environment.Equals("Development", StringComparison.OrdinalIgnoreCase);

        // ============================================================================
        // 🌐 HEXAGONAL ARCHITECTURE: DIRECT CONNECTION STRING INJECTION INTO REPOSITORIES
        // ============================================================================

        // ============================================================================
        // ⚡ ADAPTERS AND PORTS REGISTRATION (HEXAGONAL DEPENDENCY INJECTION MESH)
        // ============================================================================
        
        // ============================================================================
        // ⚡ REDIS CONFIGURATION (LAZY SINGLETON POOL - ELIMINATES HEALTHCHECK DEADLOCKS)
        // ============================================================================
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            string redisConnectionString = isDevelopment
                ? (Environment.GetEnvironmentVariable("ConnectionStrings__redis-connection") ?? "127.0.0.1:6379,ssl=false,abortConnect=false")
                : Environment.GetEnvironmentVariable("REDIS_CONNECTION")!;

            if (string.IsNullOrEmpty(redisConnectionString))
            {
                throw new ArgumentNullException(nameof(redisConnectionString), "[CRITICAL] Redis connection string environment variable is missing.");
            }
            // 🔍 UNMASKED TELEMETRY LOG: Spits the raw Redis string payload to target Cloud Logging buffer
            Console.WriteLine($"[RAW REDIS TELEMETRY] REDIS_CONNECTION string raw value: {redisConnectionString}");
            // 🚀 LAZY WIRES: Configures options to allow fast booting without blocking network synchronization threads [11.12]
            var configurationOptions = ConfigurationOptions.Parse(redisConnectionString);
            configurationOptions.AbortOnConnectFail = false; // Prevents crashing during initialization if the host is lazy [11.12]
            configurationOptions.ConnectTimeout = 5000;

            return ConnectionMultiplexer.Connect(configurationOptions);
        });

        // ============================================================================
        // ⚡ ADAPTERS AND PORTS REGISTRATION (HEXAGONAL DEPENDENCY INJECTION MESH)
        // ============================================================================

        // 1. Durable Command Port (MySQL Write Engine)
        // 🚀 STRONGLY-TYPED DI MAPPING: Binds interface explicitly to the concrete class via environment variables [11.10]
        services.AddScoped<IDurableDailyCashFlowCommandPort>(sp =>
        {
            string? writeConnectionString = isDevelopment
                ? Environment.GetEnvironmentVariable("ConnectionStrings__db-write-connection")
                : Environment.GetEnvironmentVariable("DB_WRITE_CONNECTION"); // 🚀 Deadlock-free, instantaneous memory load

            if (string.IsNullOrEmpty(writeConnectionString))
            {
                throw new ArgumentNullException(nameof(writeConnectionString), "[CRITICAL] DB Write connection string environment variable is missing or null.");
            }
            return new SQLDailyCashFlowDALCommand(writeConnectionString);
        });

        // 2. QuickAccess Command Port (Redis Cache Write Engine)
        services.AddScoped<IQuickAccessDailyCashFlowCommandPort, RedisDailyCashFlowDALCommand>();

        // 3. Event Publisher Command Port (Outbound Messaging/PubSub Channel)
        services.AddScoped<IEventDailyCashFlowPublisherCommandPort, GenerateDailyCashFlowController>();

        // 🚀 4. RESILIENT READ PIPELINE SWITCH (REDIS PRIMARY W/ SQL FALLBACK BACKING)
        // IDailyCashFlowQueryPort is explicitly bound to the resilient Redis adapter pulling from environment mappings.
        // The lambda signature remains synchronously managed to prevent delegate Task exceptions.
        services.AddScoped<IDailyCashFlowQueryPort>(sp =>
        {
            // A. Captures the Singleton Connection Multiplexer pool for Redis
            var redisPool = sp.GetRequiredService<IConnectionMultiplexer>();

            // B. Resolves the read-only connection string for database replication nodes
            string? readConnectionString = isDevelopment
                ? Environment.GetEnvironmentVariable("ConnectionStrings__db-read-connection")
                : Environment.GetEnvironmentVariable("DB_READ_CONNECTION");

            if (string.IsNullOrEmpty(readConnectionString))
            {
                throw new ArgumentNullException(nameof(readConnectionString), "[CRITICAL] Database read connection string environment variable is missing or null.");
            }

            // C. Instantiates the dedicated SQL Relational Query Repository
            var sqlDALQuery = new SQLDailyCashFlowDALQuery(readConnectionString);

            // D. Returns the resilient Redis adapter injecting the SQL repository as the active fallback engine
            return new RedisDailyCashFlowDALQuery(redisPool, sqlDALQuery);
        });


        // ============================================================================
        // ⚙️ WORKFLOW WORKERS & CORE SERVICE BOUNDARIES
        // ============================================================================
        
        // Hexagon Core Business Facade (Application Layer / SAGA Orchestrator)
        services.AddScoped<DailyCashFlowBalanceService>();

        // Registers the native formatter required by the Google FaaS CloudEvents interceptor pipeline
        services.AddSingleton<CloudNative.CloudEvents.CloudEventFormatter>(new CloudNative.CloudEvents.SystemTextJson.JsonEventFormatter());
    }

    public override void Configure(WebHostBuilderContext context, IApplicationBuilder app)
    {
        // Activates the reactive custom pipeline from BaseFramework shared middleware
        app.UseMiddleware<BaseFramework.Technology.Util.GlobalErrorHandlingMiddleware>();
    }
}
