// File: apps/daily-cash-flow-service/src/DailyCashFlowService.API/3_Technology/Middleware/Outbound/Cache/RedisDailyCashFlowDALQuery.cs
using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Query;
using StackExchange.Redis;

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
public class RedisDailyCashFlowDALQuery : IQuickAccessDailyCashFlowQueryPort
{
    private readonly IConnectionMultiplexer _redis;
    private const string CacheKeyPrefix = "daily_flow_";

    /**
     * Dependency Injection via Centralized Connection Multiplexer Pool (IoC)
     */
    public RedisDailyCashFlowDALQuery(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    /**
     * Executes a direct and optimized fetch of the consolidated net balance from memory.
     */
    public async Task<decimal> readBalanceValue(DateTime date)
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
