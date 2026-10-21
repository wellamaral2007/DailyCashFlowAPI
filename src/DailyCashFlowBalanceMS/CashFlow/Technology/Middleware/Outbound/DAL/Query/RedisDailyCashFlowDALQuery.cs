using System;
using System.Threading.Tasks;
using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Query;
using StackExchange.Redis;

namespace DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.Query;

public class RedisDailyCashFlowDALQuery : IDailyCashFlowQueryPort
{
    private readonly IConnectionMultiplexer _redis;
    private readonly SQLDailyCashFlowDALQuery _sqlDALQuery;
    private const string CacheKeyPrefix = "daily_flow_";

    public RedisDailyCashFlowDALQuery(IConnectionMultiplexer redis, SQLDailyCashFlowDALQuery sqlDALQuery)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _sqlDALQuery = sqlDALQuery ?? throw new ArgumentNullException(nameof(sqlDALQuery));
    }

    public async Task<decimal> readBalanceValue(DateTime date)
    {
        var db = _redis.GetDatabase();
        string key = $"{CacheKeyPrefix}{date:yyyy-MM-dd}";

        try
        {
            // 🚀 The asynchronous call executes natively here inside the adapter implementation [11.12]
            RedisValue cacheData = await db.StringGetAsync(key);

            if (cacheData.HasValue)
            {
                // Forces explicit string casting to eliminate delegate ambiguity [1.1]
                string rawStringData = cacheData.ToString();
                var cultureInfo = new System.Globalization.CultureInfo("pt-BR");
                
                // Parses using Brazilian culture format to accept commas cleanly [1.1]
                if (decimal.TryParse(rawStringData, System.Globalization.NumberStyles.Any, cultureInfo, out decimal cachedBalance))
                {
                    Console.WriteLine($"[CACHE HIT] Balance resolved from Redis memory for key '{key}': {cachedBalance}");
                    return cachedBalance;
                }
                else 
                {
                    // Fallback try with Invariant Culture if comma parsing fails [1.1]
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

        // 🛡️ ACTIVE CQRS FALLBACK: Executes query against MySQL Read Replica if Redis misses or drops
        return await _sqlDALQuery.readBalanceValue(date);
    }
}
