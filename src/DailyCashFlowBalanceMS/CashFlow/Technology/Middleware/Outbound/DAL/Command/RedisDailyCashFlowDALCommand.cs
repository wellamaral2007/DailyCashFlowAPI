using StackExchange.Redis;
using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Command;
using DailyCashFlowBalanceMS.CashFlow.Domain.Entity;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Outbound.DAL.Command;

/**
 * ============================================================================
 * TOGAF DIMENSION: Phase D - Technology Architecture (Outbound Middleware)
 * HEXAGONAL ROLE:  Outbound Driven Adapter - Cache Command
 * CONCEPTS:        Redis Optimistic Locking / Distributed Transaction Retry Loop
 * ============================================================================
 * This component implements atomic mutation and invalidation of the fast-access state,
 * completely isolating Redis commands from the Core application layer.
 */
public class RedisDailyCashFlowDALCommand : IQuickAccessDailyCashFlowCommandPort
{
    private readonly IConnectionMultiplexer _redis;
    private readonly CultureInfo _brazilianCulture;
    private const string CacheKeyPrefix = "daily_flow_";

    /**
     * Dependency Injection via Centralized Connection Multiplexer Pool (IoC)
     */
    public RedisDailyCashFlowDALCommand(IConnectionMultiplexer redis)
    {
        _redis = redis;
        // 🚀 Enforces pt-BR culture to correctly handle commas during string-to-decimal transformations [1.1]
        _brazilianCulture = new CultureInfo("pt-BR");
    }

    /**
     * Executes the atomic update of the in-memory balance using the Redis WATCH/EXEC pattern (Optimistic Locking).
     */
    public async Task updateBalanceValue(DailyCashFlowEntity dailyCashFlowEvent, DateTime date)
    {
        var db = _redis.GetDatabase();
        string key = $"{CacheKeyPrefix}{date:yyyy-MM-dd}";
    
        bool isUpdated = false;
        int currentAttempt = 0;
        const int maxAttempts = 5;

        while (!isUpdated && currentAttempt < maxAttempts)
        {
            // Creates the transaction managed by the driver (StackExchange.Redis wraps the WATCH command internally)
            var transaction = db.CreateTransaction();

            // Schedules the asynchronous fetch of the current balance
            var currentValueTask = db.StringGetAsync(key);
            
            // Awaits the parallel read resolution to compute the new financial projection with decimal precision
            var rawValue = await currentValueTask;
            
            decimal currentTotal = 0.0m;
            if (rawValue != RedisValue.Null)
            {
                // 🚀 CRITICAL FIX: Explicitly uses pt-BR to parse current values containing commas [1.1]
                string rawStringValue = rawValue.ToString();
                if (!decimal.TryParse(rawStringValue, NumberStyles.Any, _brazilianCulture, out currentTotal))
                {
                    // Secondary safety fallback checking invariant formatting metrics
                    decimal.TryParse(rawStringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out currentTotal);
                }
            }

            decimal incomingValue = dailyCashFlowEvent.CreditDebitValue;
            decimal newTotal = currentTotal + incomingValue;

            // 🚀 CRITICAL FIX: Forces the serialization to save decimals with commas back into Redis memory space [1.1]
            string serializedNewTotal = newTotal.ToString("F2", _brazilianCulture);

            // Enqueues the write operation atomically within the transaction pipeline
            _ = transaction.StringSetAsync(key, serializedNewTotal);

            // Attempts the physical commit (EXEC). If another thread changed the key between read and commit, returns false
            isUpdated = await transaction.ExecuteAsync();

            if (isUpdated)
            {
                Console.WriteLine($"[REDIS TRANSACTION SUCCESS] Key: {key} | Input: {incomingValue.ToString("F2", _brazilianCulture)} | Consolidated Total: {serializedNewTotal}");
            }
            else
            {
                currentAttempt++;
                Console.WriteLine($"[REDIS CONCURRENCY DETECTED] Conflict on key '{key}'. Executing retry attempt {currentAttempt}/{maxAttempts}...");
                await Task.Delay(10); // Small backoff pause before re-executing the transaction loop
            }
        }

        if (!isUpdated)
        {
            throw new InvalidOperationException($"[REDIS EXCEPTION] Failed to acquire optimistic lock for key '{key}' after {maxAttempts} attempts.");
        }
    }

    /**
     * Explicitly invalidates the current balance key to enforce the Cache-Aside pattern alignment.
     */
    public async Task InvalidateBalanceAsync()
    {
        var db = _redis.GetDatabase();
        string key = $"{CacheKeyPrefix}{DateTime.Now:yyyy-MM-dd}";
        await db.KeyDeleteAsync(key);
    }
}
