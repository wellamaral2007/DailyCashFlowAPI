using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Google.Cloud.Functions.Hosting;
using DailyCashFlowBalanceService = DailyCashFlowBalanceMS.CashFlow.Application.Service.DailyCashFlowBalanceService;
using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Command;
using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Query;
using DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.Command;
using DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.Query;
using BaseFramework.Technology.Util;
using DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.EDA.Controller;

[assembly: FunctionsStartup(typeof(DailyCashFlowBalanceMS.ProgramStartup))]

namespace DailyCashFlowBalanceMS;

/**
 * ============================================================================
 * TOGAF DIMENSION: Phase D - Technology Architecture (Central IoC Bootstrapper)
 * HEXAGONAL ROLE:  Inversion of Control (IoC) Composition Root Mesh
 * CONCEPTS:        Dependency Injection / Serverless Host Bootstrapping
 * ============================================================================
 * This class coordinates the central dependency injection container orchestration,
 * binding pure hexagonal port contracts to concrete technology infrastructure adapters.
 */
public class ProgramStartup : FunctionsStartup
{
    /**
     * Registers application infrastructure adapters and ports within the DI container mesh.
     */
    public override void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
    {
        string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        bool isDevelopment = environment.Equals("Development", StringComparison.OrdinalIgnoreCase);

        /**
         * REDIS CONFIGURATION (LAZY SINGLETON POOL - ELIMINATES HEALTHCHECK DEADLOCKS)
         * Establishes a thread-safe connection pool optimized for low-latency serverless instances.
         */
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            string redisConnectionString = isDevelopment
                ? (Environment.GetEnvironmentVariable("ConnectionStrings__redis-connection") ?? "127.0.0.1:6379,ssl=false,abortConnect=false")
                : Environment.GetEnvironmentVariable("REDIS_CONNECTION")!;

            if (string.IsNullOrEmpty(redisConnectionString))
            {
                throw new ArgumentNullException(nameof(redisConnectionString), "[CRITICAL] Redis connection string environment variable is missing.");
            }

            Console.WriteLine($"[RAW REDIS TELEMETRY] REDIS_CONNECTION string raw value: {redisConnectionString}");
            
            var configurationOptions = ConfigurationOptions.Parse(redisConnectionString);
            configurationOptions.AbortOnConnectFail = false; 
            configurationOptions.ConnectTimeout = 5000;

            return ConnectionMultiplexer.Connect(configurationOptions);
        });

        /**
         * 1. Durable Command Port (MySQL Write Engine)
         * Maps database mutation capabilities through decoupled configuration strings.
         */
        services.AddScoped<IDurableDailyCashFlowCommandPort>(sp =>
        {
            string? writeConnectionString = isDevelopment
                ? Environment.GetEnvironmentVariable("ConnectionStrings__db-write-connection")
                : Environment.GetEnvironmentVariable("DB_WRITE_CONNECTION"); 

            if (string.IsNullOrEmpty(writeConnectionString))
            {
                throw new ArgumentNullException(nameof(writeConnectionString), "[CRITICAL] DB Write connection string environment variable is missing or null.");
            }
            return new SQLDailyCashFlowDALCommand(writeConnectionString);
        });

        /**
         * 2. QuickAccess Command Port (Redis Cache Write Engine)
         */
        services.AddScoped<IQuickAccessDailyCashFlowCommandPort, RedisDailyCashFlowDALCommand>();

        /**
         * 3. Event Publisher Command Port (Outbound Messaging/PubSub Channel)
         */
        services.AddScoped<IEventDailyCashFlowPublisherCommandPort, GenerateDailyCashFlowController>();

        /**
         * 4. RESILIENT READ PIPELINE SWITCH (REDIS PRIMARY W/ SQL FALLBACK BACKING)
         * Explicitly binds the lookup interface using a synchronous factory to eliminate async container issues.
         */
        services.AddScoped<IDailyCashFlowQueryPort>(sp =>
        {
            var redisPool = sp.GetRequiredService<IConnectionMultiplexer>();

            string? readConnectionString = isDevelopment
                ? Environment.GetEnvironmentVariable("ConnectionStrings__db-read-connection")
                : Environment.GetEnvironmentVariable("DB_READ_CONNECTION");

            if (string.IsNullOrEmpty(readConnectionString))
            {
                throw new ArgumentNullException(nameof(readConnectionString), "[CRITICAL] Database read connection string environment variable is missing or null.");
            }

            var sqlDALQuery = new SQLDailyCashFlowDALQuery(readConnectionString);

            return new RedisDailyCashFlowDALQuery(redisPool, sqlDALQuery);
        });

        /**
         * Hexagon Core Business Facade (Application Layer / SAGA Orchestrator)
         */
        services.AddScoped<DailyCashFlowBalanceService>();

        /**
         * Registers the native formatter required by the Google FaaS CloudEvents interceptor pipeline.
         */
        services.AddSingleton<CloudNative.CloudEvents.CloudEventFormatter>(new CloudNative.CloudEvents.SystemTextJson.JsonEventFormatter());
    }

    /**
     * Injects corporate middleware filters into the web host request execution flow.
     */
    public override void Configure(WebHostBuilderContext context, IApplicationBuilder app)
    {
        app.UseMiddleware<BaseFramework.Technology.Util.GlobalErrorHandlingMiddleware>();
    }
}
