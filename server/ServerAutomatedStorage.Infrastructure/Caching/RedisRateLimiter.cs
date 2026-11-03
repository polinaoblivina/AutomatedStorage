using Microsoft.Extensions.Logging;
using ServerAutomatedStorage.Application.Common.Interfaces;
using StackExchange.Redis;

namespace ServerAutomatedStorage.Infrastructure.Caching
{
    public sealed class RedisRateLimiter(IConnectionMultiplexer redis, ILogger<RedisRateLimiter> logger) : IRateLimiter
    {
        private const string Script = """
            local count = redis.call('INCR', KEYS[1])
            local ttl = redis.call('PTTL', KEYS[1])
            if ttl < 0 then
                redis.call('PEXPIRE', KEYS[1], ARGV[1])
                ttl = tonumber(ARGV[1])
            end
            return { count, ttl }
            """;

        public async Task<RateLimitResult> HitAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken)
        {
            try
            {
                var raw = await redis.GetDatabase().ScriptEvaluateAsync(
                    Script,
                    [(RedisKey)$"ratelimit:{key}"],
                    [(RedisValue)(long)window.TotalMilliseconds]);

                var result = (RedisResult[])raw!;
                var count = (long)result[0];
                var ttlMilliseconds = (long)result[1];

                return count <= limit
                    ? new RateLimitResult(true, TimeSpan.Zero)
                    : new RateLimitResult(false, TimeSpan.FromMilliseconds(ttlMilliseconds));
            }
            catch (RedisException ex)
            {
                logger.LogWarning(ex, "Redis недоступен, ограничитель частоты пропускает запрос {Key}", key);
                return new RateLimitResult(true, TimeSpan.Zero);
            }
        }
    }
}
