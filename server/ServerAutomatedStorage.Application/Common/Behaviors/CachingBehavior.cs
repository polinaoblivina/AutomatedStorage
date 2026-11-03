using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using ServerAutomatedStorage.Application.Common.Interfaces;

namespace ServerAutomatedStorage.Application.Common.Behaviors
{
    public sealed class CachingBehavior<TRequest, TResponse>(
        IDistributedCache cache,
        ILogger<CachingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (request is not ICacheableQuery cacheable)
                return await next(cancellationToken);

            var cached = await TryReadAsync(cacheable.CacheKey, cancellationToken);
            if (cached is not null)
                return cached;

            var response = await next(cancellationToken);
            await TryWriteAsync(cacheable, response, cancellationToken);
            return response;
        }

        private async Task<TResponse?> TryReadAsync(string key, CancellationToken cancellationToken)
        {
            try
            {
                var bytes = await cache.GetAsync(key, cancellationToken);
                if (bytes is null)
                    return default;

                return JsonSerializer.Deserialize<TResponse>(bytes);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Кеш недоступен при чтении ключа {Key}, берём данные из базы", key);
                return default;
            }
        }

        private async Task TryWriteAsync(ICacheableQuery cacheable, TResponse response, CancellationToken cancellationToken)
        {
            try
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(response);
                var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = cacheable.CacheDuration };
                await cache.SetAsync(cacheable.CacheKey, bytes, options, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Кеш недоступен при записи ключа {Key}", cacheable.CacheKey);
            }
        }
    }
}
