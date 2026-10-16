// File: apps/daily-cash-flow-service/src/DailyCashFlowBalanceMS/Program.cs
using StackExchange.Redis;
using DailyCashFlowBalanceMS.CashFlow.Application.Service;
using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Command;
using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Query;
using DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.Command;
using DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.Query;
using BaseFramework.Technology.Util;
using DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.EDA.Controller;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// 🌐 FIXED: Enforcing Google Cloud Run PORT compliance to pass Healthchecks
// ============================================================================
// Reads the dynamic port assigned by GCP infrastructure, fallback to 8080
string port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port)); // Dynamically binds Kestrel to the required port
});

// The actual unique project ID generated on your Google Cloud project workspace
string projectId = "daily-cashflow-sol-17852";

// ============================================================================
// 🛡️ LAZY FACTORY INJECTION: Eliminating network calls during boot phase
// ============================================================================

// 1. StackExchange.Redis Connection Pool initialized ONLY when first requested by the ports
builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
{
    try
    {
        // Network call deferred to runtime execution, preventing Cold Start deadlocks
        string redisConnectionString = VaultCredentialUtility.GetSecretAsync(projectId, "redis-connection").GetAwaiter().GetResult();
        return ConnectionMultiplexer.Connect(redisConnectionString);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[CRITICAL RUNTIME] Redis Initialization Failed: {ex.Message}");
        throw;
    }
});

// 2. Entity Framework 6 Command Context passing connection strings down on demand per request scope
builder.Services.AddScoped<DailyCashFlowCommandContext>(sp => 
{
    string writeSqlConnectionString = VaultCredentialUtility.GetSecretAsync(projectId, "db-write-connection").GetAwaiter().GetResult();
    return new DailyCashFlowCommandContext(writeSqlConnectionString);
});

// 3. Entity Framework 6 Query Context passing connection strings down on demand per request scope
builder.Services.AddScoped<DailyCashFlowQueryContext>(sp => 
{
    string readSqlConnectionString = VaultCredentialUtility.GetSecretAsync(projectId, "db-read-connection").GetAwaiter().GetResult();
    return new DailyCashFlowQueryContext(readSqlConnectionString);
});

// ============================================================================
// Hexagonal Architecture: Wiring Driven Adapters (Middleware) to Outbound Ports
// ============================================================================

// 4. Register the concrete SQL durable query repository factory passing down the lazy connection string context
builder.Services.AddScoped<SQLDailyCashFlowDALQuery>(sp => 
{
    string readSqlConnectionString = VaultCredentialUtility.GetSecretAsync(projectId, "db-read-connection").GetAwaiter().GetResult();
    return new SQLDailyCashFlowDALQuery(readSqlConnectionString);
});

// 5. Mapping Persistent Durable Command and Query Ports to their respective repositories
builder.Services.AddScoped<IDurableDailyCashFlowCommandPort, SQLDailyCashFlowDALCommand>();

// 6. 📌 ENCHAINED WIRING: Resolves the main read port mapping by fetching the Singleton Redis connection 
// alongside the concrete Scoped SQL query repository instance to enable resilient circuit breaker fallbacks.
builder.Services.AddScoped<IDailyCashFlowQueryPort, RedisDailyCashFlowDALQuery>(sp => 
{
    var redisMultiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
    var sqlDALQuery = sp.GetRequiredService<SQLDailyCashFlowDALQuery>();
    return new RedisDailyCashFlowDALQuery(redisMultiplexer, sqlDALQuery);
});

// 7. Mapping In-Memory QuickAccess Command Ports to Redis separate repositories
builder.Services.AddScoped<IQuickAccessDailyCashFlowCommandPort, RedisDailyCashFlowDALCommand>();

// Mapping the abstract core outbound port to the unified controller class implementing it
builder.Services.AddScoped<IEventDailyCashFlowPublisherCommandPort, GenerateDailyCashFlowController>();

// Application Core Services Mapping (The Hexagon Core Facade)
builder.Services.AddScoped<DailyCashFlowBalanceService>();

var app = builder.Build();

// ============================================================================
// Middleware HTTP Pipeline Request Lifecycles Activation
// ============================================================================
app.UseMiddleware<BaseFramework.Technology.Util.GlobalErrorHandlingMiddleware>();

// 📌 MANDATORY FOR CLOUD RUN: Native minimal endpoint to immediately satisfy the readiness health check probe
app.MapGet("/", () => Results.Ok(new { status = "healthy", runtime = "net8.0-FaaS" }));

app.Run();
