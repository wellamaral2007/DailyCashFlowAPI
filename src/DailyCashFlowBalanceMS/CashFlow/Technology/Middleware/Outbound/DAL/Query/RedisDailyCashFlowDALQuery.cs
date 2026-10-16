// File: apps/daily-cash-flow-service/src/DailyCashFlowService.API/3_Technology/Middleware/Outbound/Cache/RedisDailyCashFlowDALQuery.cs
using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Query;
using StackExchange.Redis;
using Polly;
using Polly.CircuitBreaker;

namespace DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.Query;

/**
 * ============================================================================
 * TOGAF DIMENSION: Phase D - Technology Architecture (Outbound Middleware)
 * HEXAGONAL ROLE:  Outbound Driven Adapter - Cache Query
 * CONCEPTS:        High Performance Caching / Sub-millisecond Key-Value Reads
 * ============================================================================
 * This infrastructure component implements clean read-only caching mechanisms,
 * fetching high-speed data state projections completely isolated from the Core.
 */
public class RedisDailyCashFlowDALQuery : IDailyCashFlowQueryPort
{
    private readonly IConnectionMultiplexer _redis;
    private const string CacheKeyPrefix = "daily_flow_";
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;

    private SQLDailyCashFlowDALQuery _sqlDailyCashFlowDALQuery;

    /**
     * Dependency Injection via Centralized Connection Multiplexer Pool (IoC)
     */
    public RedisDailyCashFlowDALQuery(IConnectionMultiplexer redis, SQLDailyCashFlowDALQuery _sqlDALQuery)
    {
        _redis = redis;
        _sqlDailyCashFlowDALQuery = _sqlDALQuery ?? throw new ArgumentNullException(nameof(_sqlDALQuery));
        // Resilience Governance Configuration: Breaks the circuit after 3 consecutive failures, waits 30 seconds
        _circuitBreaker = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (ex, timespan) => 
                { 
                    Console.WriteLine($"[DAILY CASH FLOW CIRCUIT BREAKER] Opened! Reason: {ex.Message}. Routing traffic directly to Durable storage."); 
                },
                onReset: () => 
                { 
                    Console.WriteLine("[DAILY CASH FLOW CIRCUIT BREAKER] Closed! QuickAccess middleware is back online."); 
                }
            ); 
    }

    /**
     * Executes a direct and optimized fetch of the consolidated net balance from memory.
     */
    public async Task<decimal> readBalanceValue(DateTime date)
    {
        try
        {
            // Executes the data fetch protected by the Circuit Breaker targeting the QuickAccess Port
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                try
                {
                    var db = _redis.GetDatabase();
                    string key = $"{CacheKeyPrefix}{date:yyyy-MM-dd}";
                    
                    var cachedValue = await db.StringGetAsync(key);
                    if (cachedValue.IsNullOrEmpty) 
                    {
                        return 0.0m; // Returns absolute zero acting as a traditional Cache Miss fallback trigger
                    }
                    
                    return decimal.Parse(cachedValue.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[QUICK ACCESS ERROR] {ex.Message}. Tripping Circuit Breaker and routing to Durable Query Port.");
                    throw; // Rethrow to inform Polly about the failure and update the circuit state
                }
            });
        }
        catch (BrokenCircuitException)
        {
            // 2. Critical Fallback: Circuit is broken. Bypasses QuickAccess completely and targets the Durable Query Port directly
            Console.WriteLine("[FALLBACK CRITICAL] Circuit is broken. Bypassing QuickAccess and querying Durable Query Port directly.");
            return await _sqlDailyCashFlowDALQuery.readBalanceValue(DateTime.Now);
        }

    }

    /**
     * Hydrates the fast-access read cache memory mapping (Cache Miss Hydration lifecycle)
     */
    public async Task SetCachedBalanceAsync(decimal balance, TimeSpan ttl)
    {
        var db = _redis.GetDatabase();
        string key = $"{CacheKeyPrefix}{DateTime.UtcNow:yyyy-MM-dd}";
        await db.StringSetAsync(key, balance.ToString(), ttl);
    }
}
