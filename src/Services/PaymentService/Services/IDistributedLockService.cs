using System;
using System.Threading.Tasks;

namespace PaymentService.Services;

public interface IDistributedLockService
{
    Task<IDistributedLock?> AcquireLockAsync(string key, TimeSpan expiration, TimeSpan? waitTime = null);
    
    // Saga-scoped lock operations
    Task<SagaLockState?> AcquireSagaLockAsync(string sagaInstanceId, string donationId, string paymentMethod, decimal amount, TimeSpan expiration, TimeSpan? waitTime = null);
    Task<bool> ExtendSagaLockAsync(SagaLockState lockState, TimeSpan extension);
    Task<bool> ReleaseSagaLockAsync(SagaLockState lockState);
    Task<bool> IsSagaLockValidAsync(SagaLockState lockState);
}

public interface IDistributedLock : IAsyncDisposable
{
    string Key { get; }
    DateTime AcquiredAt { get; }
    TimeSpan Expiration { get; }
    bool IsExpired { get; }
    Task<bool> ExtendAsync(TimeSpan additionalTime);
    Task ReleaseLockAsync();
}

// Saga lock state that can be persisted in orchestration
public class SagaLockState
{
    public string LockKey { get; set; } = string.Empty;
    public string LockToken { get; set; } = string.Empty;
    public string SagaInstanceId { get; set; } = string.Empty;
    public DateTime AcquiredAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsAcquired { get; set; }
    
    // Lock context for better debugging and monitoring
    public string DonationId { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
