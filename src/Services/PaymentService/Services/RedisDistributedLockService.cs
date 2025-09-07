using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace PaymentService.Services;

public class RedisDistributedLockService : IDistributedLockService
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisDistributedLockService> _logger;

    public RedisDistributedLockService(IConnectionMultiplexer redis, ILogger<RedisDistributedLockService> logger)
    {
        _database = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<IDistributedLock?> AcquireLockAsync(string key, TimeSpan expiration, TimeSpan? waitTime = null)
    {
        var lockKey = $"lock:{key}";
        var lockValue = Guid.NewGuid().ToString();
        var maxWaitTime = waitTime ?? TimeSpan.FromSeconds(5);
        var startTime = DateTime.UtcNow;

        _logger.LogDebug("Attempting to acquire lock for key {LockKey} with expiration {Expiration}", 
            lockKey, expiration);

        while (DateTime.UtcNow - startTime < maxWaitTime)
        {
            try
            {
                // Try to set the lock with expiration
                var acquired = await _database.StringSetAsync(lockKey, lockValue, expiration, When.NotExists);
                
                if (acquired)
                {
                    _logger.LogDebug("Successfully acquired lock for key {LockKey}", lockKey);
                    return new RedisDistributedLock(_database, lockKey, lockValue, expiration, _logger);
                }

                // Wait a bit before retrying
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error attempting to acquire lock for key {LockKey}", lockKey);
                return null;
            }
        }

        _logger.LogWarning("Failed to acquire lock for key {LockKey} within {WaitTime}", lockKey, maxWaitTime);
        return null;
    }

    public async Task<SagaLockState?> AcquireSagaLockAsync(
        string sagaInstanceId, 
        string donationId, 
        string paymentMethod, 
        decimal amount, 
        TimeSpan expiration, 
        TimeSpan? waitTime = null)
    {
        // Create a comprehensive lock key that includes all relevant parameters
        var lockKey = $"saga-lock:donation:{donationId}:method:{paymentMethod}:amount:{amount:F2}";
        var lockToken = $"saga:{sagaInstanceId}:token:{Guid.NewGuid()}";
        var maxWaitTime = waitTime ?? TimeSpan.FromSeconds(5);
        var startTime = DateTime.UtcNow;

        _logger.LogDebug(
            "Attempting to acquire saga lock for donation {DonationId}, method {PaymentMethod}, amount {Amount}, saga {SagaInstanceId}", 
            donationId, paymentMethod, amount, sagaInstanceId);

        while (DateTime.UtcNow - startTime < maxWaitTime)
        {
            try
            {
                // Try to set the lock with expiration
                var acquired = await _database.StringSetAsync(lockKey, lockToken, expiration, When.NotExists);
                
                if (acquired)
                {
                    var lockState = new SagaLockState
                    {
                        LockKey = lockKey,
                        LockToken = lockToken,
                        SagaInstanceId = sagaInstanceId,
                        AcquiredAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.Add(expiration),
                        IsAcquired = true,
                        DonationId = donationId,
                        PaymentMethod = paymentMethod,
                        Amount = amount
                    };

                    _logger.LogInformation(
                        "Successfully acquired saga lock {LockKey} for saga {SagaInstanceId}", 
                        lockKey, sagaInstanceId);
                    
                    return lockState;
                }

                // Wait a bit before retrying
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error attempting to acquire saga lock for donation {DonationId}", donationId);
                return null;
            }
        }

        _logger.LogWarning(
            "Failed to acquire saga lock for donation {DonationId} within {WaitTime}. Another payment may be in progress.", 
            donationId, maxWaitTime);
        return null;
    }

    public async Task<bool> ExtendSagaLockAsync(SagaLockState lockState, TimeSpan extension)
    {
        if (lockState == null || !lockState.IsAcquired)
        {
            _logger.LogWarning("Cannot extend invalid or unacquired saga lock");
            return false;
        }

        try
        {
            // Lua script to atomically check and extend the saga lock
            const string script = @"
                if redis.call('GET', KEYS[1]) == ARGV[1] then
                    return redis.call('EXPIRE', KEYS[1], ARGV[2])
                else
                    return 0
                end";

            var result = await _database.ScriptEvaluateAsync(
                script, 
                new RedisKey[] { lockState.LockKey }, 
                new RedisValue[] { lockState.LockToken, (int)extension.TotalSeconds });

            var extended = (int)result == 1;
            
            if (extended)
            {
                lockState.ExpiresAt = DateTime.UtcNow.Add(extension);
                _logger.LogDebug(
                    "Extended saga lock {LockKey} for saga {SagaInstanceId} by {Extension}", 
                    lockState.LockKey, lockState.SagaInstanceId, extension);
            }
            else
            {
                lockState.IsAcquired = false;
                _logger.LogWarning(
                    "Failed to extend saga lock {LockKey} for saga {SagaInstanceId} - lock may have expired", 
                    lockState.LockKey, lockState.SagaInstanceId);
            }

            return extended;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error extending saga lock {LockKey} for saga {SagaInstanceId}", 
                lockState.LockKey, lockState.SagaInstanceId);
            return false;
        }
    }

    public async Task<bool> ReleaseSagaLockAsync(SagaLockState lockState)
    {
        if (lockState == null || !lockState.IsAcquired)
        {
            _logger.LogDebug("Saga lock is already released or invalid");
            return true; // Consider already released as success
        }

        try
        {
            // Lua script to atomically check and delete the saga lock
            const string script = @"
                if redis.call('GET', KEYS[1]) == ARGV[1] then
                    return redis.call('DEL', KEYS[1])
                else
                    return 0
                end";

            var result = await _database.ScriptEvaluateAsync(
                script, 
                new RedisKey[] { lockState.LockKey }, 
                new RedisValue[] { lockState.LockToken });

            var released = (int)result == 1;
            
            if (released)
            {
                lockState.IsAcquired = false;
                _logger.LogInformation(
                    "Released saga lock {LockKey} for saga {SagaInstanceId}", 
                    lockState.LockKey, lockState.SagaInstanceId);
            }
            else
            {
                lockState.IsAcquired = false; // Mark as released anyway
                _logger.LogWarning(
                    "Saga lock {LockKey} for saga {SagaInstanceId} was not found in Redis - may have already expired", 
                    lockState.LockKey, lockState.SagaInstanceId);
            }

            return true; // Return true even if lock was already gone
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error releasing saga lock {LockKey} for saga {SagaInstanceId}", 
                lockState.LockKey, lockState.SagaInstanceId);
            return false;
        }
    }

    public async Task<bool> IsSagaLockValidAsync(SagaLockState lockState)
    {
        if (lockState == null || !lockState.IsAcquired)
        {
            return false;
        }

        try
        {
            // Check if the lock still exists and has the correct token
            var currentValue = await _database.StringGetAsync(lockState.LockKey);
            var isValid = currentValue.HasValue && currentValue == lockState.LockToken;
            
            if (!isValid)
            {
                lockState.IsAcquired = false;
                _logger.LogWarning(
                    "Saga lock {LockKey} for saga {SagaInstanceId} is no longer valid", 
                    lockState.LockKey, lockState.SagaInstanceId);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error validating saga lock {LockKey} for saga {SagaInstanceId}", 
                lockState.LockKey, lockState.SagaInstanceId);
            return false;
        }
    }
}

public class RedisDistributedLock : IDistributedLock
{
    private readonly IDatabase _database;
    private readonly ILogger _logger;
    private readonly string _lockValue;
    private bool _disposed;

    public RedisDistributedLock(
        IDatabase database, 
        string key, 
        string lockValue, 
        TimeSpan expiration, 
        ILogger logger)
    {
        _database = database;
        _logger = logger;
        _lockValue = lockValue;
        Key = key;
        Expiration = expiration;
        AcquiredAt = DateTime.UtcNow;
    }

    public string Key { get; }
    public DateTime AcquiredAt { get; }
    public TimeSpan Expiration { get; }
    public bool IsExpired => DateTime.UtcNow - AcquiredAt > Expiration;

    public async Task<bool> ExtendAsync(TimeSpan additionalTime)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RedisDistributedLock));

        try
        {
            // Lua script to atomically check and extend the lock
            const string script = @"
                if redis.call('GET', KEYS[1]) == ARGV[1] then
                    return redis.call('EXPIRE', KEYS[1], ARGV[2])
                else
                    return 0
                end";

            var result = await _database.ScriptEvaluateAsync(
                script, 
                new RedisKey[] { Key }, 
                new RedisValue[] { _lockValue, (int)additionalTime.TotalSeconds });

            var extended = (int)result == 1;
            
            if (extended)
            {
                _logger.LogDebug("Extended lock for key {LockKey} by {AdditionalTime}", Key, additionalTime);
            }
            else
            {
                _logger.LogWarning("Failed to extend lock for key {LockKey} - lock may have expired or been released", Key);
            }

            return extended;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extending lock for key {LockKey}", Key);
            return false;
        }
    }

    public async Task ReleaseLockAsync()
    {
        if (_disposed)
            return;

        try
        {
            // Lua script to atomically check and delete the lock
            const string script = @"
                if redis.call('GET', KEYS[1]) == ARGV[1] then
                    return redis.call('DEL', KEYS[1])
                else
                    return 0
                end";

            var result = await _database.ScriptEvaluateAsync(
                script, 
                new RedisKey[] { Key }, 
                new RedisValue[] { _lockValue });

            var released = (int)result == 1;
            
            if (released)
            {
                _logger.LogDebug("Released lock for key {LockKey}", Key);
            }
            else
            {
                _logger.LogWarning("Lock for key {LockKey} was not released - may have already expired", Key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing lock for key {LockKey}", Key);
        }
        finally
        {
            _disposed = true;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await ReleaseLockAsync();
        }
    }
}
