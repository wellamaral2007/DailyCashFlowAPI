using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Google.Cloud.Functions.Hosting; // Benefit from Google's native FaaS initialization host
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
        string projectId = "daily-cashflow-sol-17852";
        string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        bool isDevelopment = environment.Equals("Development", StringComparison.OrdinalIgnoreCase);

        // ============================================================================
        // REDIS CONFIGURATION (LAZY SINGLETON POOL)
        // ============================================================================
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            string redisConnectionString = isDevelopment
                ? (Environment.GetEnvironmentVariable("ConnectionStrings__redis-connection") ?? "127.0.0.1:6379,ssl=false,abortConnect=false")
                : VaultCredentialUtility.GetSecretAsync(projectId, "redis-connection").GetAwaiter().GetResult();
            return ConnectionMultiplexer.Connect(redisConnectionString);
        });

        // ============================================================================
        // HEXAGONAL ARCHITECTURE: DIRECT CONNECTION STRING INJECTION INTO REPOSITORIES
        // ============================================================================

        // ============================================================================
        // ADAPTERS AND PORTS REGISTRATION (HEXAGONAL DEPENDENCY INJECTION MESH)
        // ============================================================================

        // 1. Durable Command Port (MySQL Write Engine)
        services.AddScoped<IDurableDailyCashFlowCommandPort, SQLDailyCashFlowDALCommand>(sp =>
        {
            string writeConnectionString = isDevelopment
                ? (Environment.GetEnvironmentVariable("ConnectionStrings__db-write-connection") ?? Environment.GetEnvironmentVariable("DATABASE")!)
                : VaultCredentialUtility.GetSecretAsync(projectId, "db-write-connection").GetAwaiter().GetResult();

            return new SQLDailyCashFlowDALCommand(writeConnectionString);
        });

        // 2. QuickAccess Command Port (Redis Cache Write Engine)
        services.AddScoped<IQuickAccessDailyCashFlowCommandPort, RedisDailyCashFlowDALCommand>();

        // 3. Event Publisher Command Port (Outbound Messaging/PubSub Channel)
        services.AddScoped<IEventDailyCashFlowPublisherCommandPort, GenerateDailyCashFlowController>();

        // 4. Query Port (MySQL/Redis Read Engine)
        services.AddScoped<IDailyCashFlowQueryPort, SQLDailyCashFlowDALQuery>(sp =>
        {
            string readConnectionString = isDevelopment
                ? (Environment.GetEnvironmentVariable("ConnectionStrings__db-read-connection") ?? Environment.GetEnvironmentVariable("DATABASE")!)
                : VaultCredentialUtility.GetSecretAsync(projectId, "db-read-connection").GetAwaiter().GetResult();

            return new SQLDailyCashFlowDALQuery(readConnectionString);
        });

        // ============================================================================
        // WORKFLOW WORKERS & CORE SERVICE BOUNDARIES
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
