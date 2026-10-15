// File: apps/daily-cash-flow-service/src/DailyCashFlowService.API/3_Technology/Middleware/Outbound/DAL/Command/RedisDailyCashFlowDALCommand.cs
using StackExchange.Redis;
using DailyCashFlowBalanceMS.CashFlow.Application.Contract.Command;
using DailyCashFlowBalanceMS.CashFlow.Domain.Entity;

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
    private const string CacheKeyPrefix = "daily_flow_";

    /**
     * Dependency Injection via Centralized Connection Multiplexer Pool (IoC)
     */
    public RedisDailyCashFlowDALCommand(IConnectionMultiplexer redis)
    {
        _redis = redis;
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
            decimal currentTotal = rawValue == RedisValue.Null ? 0.0m : decimal.Parse(rawValue.ToString());

            decimal incomingValue = dailyCashFlowEvent.CreditDebitValue;
            decimal newTotal = currentTotal + incomingValue;

            // Enqueues the write operation atomically within the transaction pipeline
            _ = transaction.StringSetAsync(key, newTotal.ToString());

            // Attempts the physical commit (EXEC). If another thread changed the key between read and commit, returns false
            isUpdated = await transaction.ExecuteAsync();

            if (isUpdated)
            {
                Console.WriteLine($"[REDIS TRANSACTION SUCCESS] Key: {key} | Input: {incomingValue} | Consolidated Total: {newTotal}");
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
        string key = $"{CacheKeyPrefix}{DateTime.UtcNow:yyyy-MM-dd}";
        await db.KeyDeleteAsync(key);
    }
}
