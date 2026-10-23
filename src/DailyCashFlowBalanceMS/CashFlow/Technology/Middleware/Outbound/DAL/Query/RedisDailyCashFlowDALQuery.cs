using System;
using System.Threading.Tasks;
using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Query;
using StackExchange.Redis;

namespace DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.Query;

/**
 * ============================================================================
 * TOGAF DIMENSION: Phase D - Technology Architecture (Outbound Middleware)
 * HEXAGONAL ROLE:  Outbound Driven Adapter - Cache Query Pipeline
 * CONCEPTS:        Cache-Aside Pattern / Circuit Breaker Fallback Rerouting
 * ============================================================================
 * This component encapsulates fast-access state read operations from Redis,
 * automatically falling back to relational database read replicas upon failure.
 */
public class RedisDailyCashFlowDALQuery : IDailyCashFlowQueryPort
{
    private readonly IConnectionMultiplexer _redis;
    private readonly SQLDailyCashFlowDALQuery _sqlDALQuery;
    private const string CacheKeyPrefix = "daily_flow_";

    /**
     * Dependency Injection constructor coordinating cache pools and relational query fallbacks.
     */
    public RedisDailyCashFlowDALQuery(IConnectionMultiplexer redis, SQLDailyCashFlowDALQuery sqlDALQuery)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _sqlDALQuery = sqlDALQuery ?? throw new ArgumentNullException(nameof(sqlDALQuery));
    }

    /**
     * Resolves the consolidated balance value for a target date with embedded resilience boundaries.
     */
    public async Task<decimal> readBalanceValue(DateTime date)
    {
        var db = _redis.GetDatabase();
        string key = $"{CacheKeyPrefix}{date:yyyy-MM-dd}";

        try
        {
            // Asynchronous data extraction from the in-memory key-value storage engine
            RedisValue cacheData = await db.StringGetAsync(key);

            if (cacheData.HasValue)
            {
                // Explicit string casting to eliminate method overload signature ambiguity [1.1]
                string rawStringData = cacheData.ToString();
                var cultureInfo = new System.Globalization.CultureInfo("pt-BR");
                
                // Evaluates and parses numbers containing commas via Brazilian culture formatting [1.1]
                if (decimal.TryParse(rawStringData, System.Globalization.NumberStyles.Any, cultureInfo, out decimal cachedBalance))
                {
                    Console.WriteLine($"[CACHE HIT] Balance resolved from Redis memory for key '{key}': {cachedBalance}");
                    return cachedBalance;
                }
                else 
                {
                    // Secondary parsing validation attempt using invariant culture rules [1.1]
                    if (decimal.TryParse(rawStringData, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal cachedBalanceInvariant))
                    {
                        Console.WriteLine($"[CACHE HIT] Balance resolved via Invariant Culture for key '{key}': {cachedBalanceInvariant}");
                        return cachedBalanceInvariant;
                    }
                }
            }

            Console.WriteLine($"[CACHE MISS] Key '{key}' not found or empty in Redis memory. Triggering fallback mesh.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[REDIS NETWORK EXCEPTION HANDLED] Reason: {ex.Message}. Routing to SQL replication node.");
        }

        // Active CQRS Fallback: reroutes execution to aggregate balances from MySql Read Replicas
        return await _sqlDALQuery.readBalanceValue(date);
    }
}
