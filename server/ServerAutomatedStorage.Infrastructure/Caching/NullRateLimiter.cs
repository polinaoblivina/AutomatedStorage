using ServerAutomatedStorage.Application.Common.Interfaces;

namespace ServerAutomatedStorage.Infrastructure.Caching
{
    public sealed class NullRateLimiter : IRateLimiter
    {
        public Task<RateLimitResult> HitAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken)
            => Task.FromResult(new RateLimitResult(true, TimeSpan.Zero));
    }
}
