using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace PaymentService.Services;

public class InMemoryDistributedLockService : IDistributedLockService
{
    private readonly ConcurrentDictionary<string, InMemoryLockInfo> _locks = new();
    private readonly ILogger<InMemoryDistributedLockService> _logger;

    public InMemoryDistributedLockService(ILogger<InMemoryDistributedLockService> logger)
    {
        _logger = logger;
    }

    public async Task<IDistributedLock?> AcquireLockAsync(string key, TimeSpan expiration, TimeSpan? waitTime = null)
    {
        var lockKey = $"lock:{key}";
        var lockValue = Guid.NewGuid().ToString();
        var maxWaitTime = waitTime ?? TimeSpan.FromSeconds(5);
        var startTime = DateTime.UtcNow;

        _logger.LogDebug("Attempting to acquire in-memory lock for key {LockKey} with expiration {Expiration}", 
            lockKey, expiration);

        while (DateTime.UtcNow - startTime < maxWaitTime)
        {
            // Clean up expired locks
            CleanupExpiredLocks();

            var lockInfo = new InMemoryLockInfo
            {
                Value = lockValue,
                AcquiredAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(expiration)
            };

            if (_locks.TryAdd(lockKey, lockInfo))
            {
                _logger.LogDebug("Successfully acquired in-memory lock for key {LockKey}", lockKey);
                return new InMemoryDistributedLock(this, lockKey, lockValue, expiration, _logger);
            }

            // Wait a bit before retrying
            await Task.Delay(100);
        }

        _logger.LogWarning("Failed to acquire in-memory lock for key {LockKey} within {WaitTime}", lockKey, maxWaitTime);
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
            "Attempting to acquire in-memory saga lock for donation {DonationId}, method {PaymentMethod}, amount {Amount}, saga {SagaInstanceId}", 
            donationId, paymentMethod, amount, sagaInstanceId);

        while (DateTime.UtcNow - startTime < maxWaitTime)
        {
            // Clean up expired locks
            CleanupExpiredLocks();

            var lockInfo = new InMemoryLockInfo
            {
                Value = lockToken,
                AcquiredAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(expiration)
            };

            if (_locks.TryAdd(lockKey, lockInfo))
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
                    "Successfully acquired in-memory saga lock {LockKey} for saga {SagaInstanceId}", 
                    lockKey, sagaInstanceId);
                
                return lockState;
            }

            // Wait a bit before retrying
            await Task.Delay(100);
        }

        _logger.LogWarning(
            "Failed to acquire in-memory saga lock for donation {DonationId} within {WaitTime}. Another payment may be in progress.", 
            donationId, maxWaitTime);
        return null;
    }

    public Task<bool> ExtendSagaLockAsync(SagaLockState lockState, TimeSpan extension)
    {
        if (lockState == null || !lockState.IsAcquired)
        {
            _logger.LogWarning("Cannot extend invalid or unacquired saga lock");
            return Task.FromResult(false);
        }

        if (_locks.TryGetValue(lockState.LockKey, out var lockInfo) && lockInfo.Value == lockState.LockToken)
        {
            lockInfo.ExpiresAt = lockInfo.ExpiresAt.Add(extension);
            lockState.ExpiresAt = DateTime.UtcNow.Add(extension);
            
            _logger.LogDebug(
                "Extended in-memory saga lock {LockKey} for saga {SagaInstanceId} by {Extension}", 
                lockState.LockKey, lockState.SagaInstanceId, extension);
            
            return Task.FromResult(true);
        }
        
        lockState.IsAcquired = false;
        _logger.LogWarning(
            "Failed to extend in-memory saga lock {LockKey} for saga {SagaInstanceId} - lock may have expired", 
            lockState.LockKey, lockState.SagaInstanceId);
        
        return Task.FromResult(false);
    }

    public Task<bool> ReleaseSagaLockAsync(SagaLockState lockState)
    {
        if (lockState == null || !lockState.IsAcquired)
        {
            _logger.LogDebug("In-memory saga lock is already released or invalid");
            return Task.FromResult(true); // Consider already released as success
        }

        var released = false;
        if (_locks.TryGetValue(lockState.LockKey, out var lockInfo) && lockInfo.Value == lockState.LockToken)
        {
            released = _locks.TryRemove(lockState.LockKey, out _);
        }

        lockState.IsAcquired = false;
        
        if (released)
        {
            _logger.LogInformation(
                "Released in-memory saga lock {LockKey} for saga {SagaInstanceId}", 
                lockState.LockKey, lockState.SagaInstanceId);
        }
        else
        {
            _logger.LogWarning(
                "In-memory saga lock {LockKey} for saga {SagaInstanceId} was not found - may have already expired", 
                lockState.LockKey, lockState.SagaInstanceId);
        }

        return Task.FromResult(true); // Return true even if lock was already gone
    }

    public Task<bool> IsSagaLockValidAsync(SagaLockState lockState)
    {
        if (lockState == null || !lockState.IsAcquired)
        {
            return Task.FromResult(false);
        }

        // Clean up expired locks first
        CleanupExpiredLocks();

        var isValid = _locks.TryGetValue(lockState.LockKey, out var lockInfo) && 
                      lockInfo.Value == lockState.LockToken &&
                      lockInfo.ExpiresAt > DateTime.UtcNow;
        
        if (!isValid)
        {
            lockState.IsAcquired = false;
            _logger.LogWarning(
                "In-memory saga lock {LockKey} for saga {SagaInstanceId} is no longer valid", 
                lockState.LockKey, lockState.SagaInstanceId);
        }

        return Task.FromResult(isValid);
    }

    internal bool ReleaseLock(string key, string value)
    {
        if (_locks.TryGetValue(key, out var lockInfo) && lockInfo.Value == value)
        {
            return _locks.TryRemove(key, out _);
        }
        return false;
    }

    internal bool ExtendLock(string key, string value, TimeSpan additionalTime)
    {
        if (_locks.TryGetValue(key, out var lockInfo) && lockInfo.Value == value)
        {
            lockInfo.ExpiresAt = lockInfo.ExpiresAt.Add(additionalTime);
            return true;
        }
        return false;
    }

    private void CleanupExpiredLocks()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _locks
            .Where(kvp => kvp.Value.ExpiresAt <= now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _locks.TryRemove(key, out _);
        }
    }

    private class InMemoryLockInfo
    {
        public string Value { get; set; } = string.Empty;
        public DateTime AcquiredAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}

public class InMemoryDistributedLock : IDistributedLock
{
    private readonly InMemoryDistributedLockService _lockService;
    private readonly ILogger _logger;
    private readonly string _lockValue;
    private bool _disposed;

    public InMemoryDistributedLock(
        InMemoryDistributedLockService lockService,
        string key,
        string lockValue,
        TimeSpan expiration,
        ILogger logger)
    {
        _lockService = lockService;
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

    public Task<bool> ExtendAsync(TimeSpan additionalTime)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(InMemoryDistributedLock));

        var extended = _lockService.ExtendLock(Key, _lockValue, additionalTime);
        
        if (extended)
        {
            _logger.LogDebug("Extended in-memory lock for key {LockKey} by {AdditionalTime}", Key, additionalTime);
        }
        else
        {
            _logger.LogWarning("Failed to extend in-memory lock for key {LockKey} - lock may have expired", Key);
        }

        return Task.FromResult(extended);
    }

    public Task ReleaseLockAsync()
    {
        if (_disposed)
            return Task.CompletedTask;

        var released = _lockService.ReleaseLock(Key, _lockValue);
        
        if (released)
        {
            _logger.LogDebug("Released in-memory lock for key {LockKey}", Key);
        }
        else
        {
            _logger.LogWarning("In-memory lock for key {LockKey} was not released - may have already expired", Key);
        }

        _disposed = true;
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await ReleaseLockAsync();
        }
    }
}
