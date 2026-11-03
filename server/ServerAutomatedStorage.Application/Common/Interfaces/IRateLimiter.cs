namespace ServerAutomatedStorage.Application.Common.Interfaces
{
    public readonly record struct RateLimitResult(bool Allowed, TimeSpan RetryAfter);

    public interface IRateLimiter
    {
        Task<RateLimitResult> HitAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken);
    }
}
