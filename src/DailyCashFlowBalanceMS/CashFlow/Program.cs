using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using DailyCashFlowBalanceMS.CashFlow.Application.Service;
using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Command;
using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Query;
using DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.Command;
using DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.Query;
using DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.ORM;
using BaseFramework.Technology.Util;
using DailyCashFlowBalanceMS.DailyCashFlow.Technology.Middleware.Outbound.Dal;
using GenerateDailyCashFlowMS.CashFlow.Technology.Middleware.Outbound.Eda;


var builder = WebApplication.CreateBuilder(args);

string projectId = "DailyCashFlowBalance";

// ============================================================================
// TOGAF Phase D: Technology Architecture - Credential Vault & Bootstrapping
// ============================================================================

// 1. Asynchronously fetching corporate database secrets from Google Secret Manager V1
string writeSqlConnectionString = await VaultCredentialUtility.GetSecretAsync(projectId, "db-write-connection");
string readSqlConnectionString = await VaultCredentialUtility.GetSecretAsync(projectId, "db-read-connection");
string redisConnectionString = await VaultCredentialUtility.GetSecretAsync(projectId, "redis-connection");

// 2. Wire StackExchange.Redis Connection Pool v2.8.16 as Singleton for network performance
builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
    ConnectionMultiplexer.Connect(redisConnectionString));

// 3. Registering Entity Framework 6 Context Factories passing the dynamic connection strings down
builder.Services.AddScoped<DailyCashFlowCommandContext>(sp => 
    new DailyCashFlowCommandContext(writeSqlConnectionString));

builder.Services.AddScoped<DailyCashFlowQueryContext>(sp => 
    new DailyCashFlowQueryContext(readSqlConnectionString));

// ============================================================================
// Hexagonal Architecture: Wiring Driven Adapters (Middleware) to Outbound Ports
// ============================================================================

// Mapping Persistent Durable Command and Query Ports to EF6 separate repositories
builder.Services.AddScoped<IDurableDailyCashFlowCommandPort, SQLDailyCashFlowDALCommand>();
builder.Services.AddScoped<IDurableDailyCashFlowQueryPort, SQLDailyCashFlowDALQuery>();

// Mapping In-Memory QuickAccess Command and Query Ports to Redis separate repositories
builder.Services.AddScoped<IQuickAccessDailyCashFlowCommandPort, RedisDailyCashFlowDALCommand>();
builder.Services.AddScoped<IQuickAccessDailyCashFlowQueryPort, RedisDailyCashFlowDALQuery>();

builder.Services.AddScoped<IEventDailyCashFlowPublisherCommandPort, GenerateDailyCashFlowController>();

// ============================================================================
// Application Core Services Mapping (The Hexagon Core Facade)
// ============================================================================
builder.Services.AddScoped<DailyCashFlowBalanceService>();

// ============================================================================
// Google Cloud Functions Hosting Serverless Engine Bootstrapping
// ============================================================================
// NOTE: Google.Cloud.Functions.Hosting auto-discovers and registers IHttpFunction 
// and ICloudEventFunction implementations via reflection at runtime. 
// No manual builder.Services extension method call is required here.

var app = builder.Build();

// ============================================================================
// Middleware HTTP Pipeline HTTP Request Lifecycles Activation
// ============================================================================
// 📌 Activating the global secure Error Catch Interceptor from our BaseFramework shared library package
app.UseMiddleware<BaseFramework.Technology.Util.GlobalErrorHandlingMiddleware>();

app.Run();

