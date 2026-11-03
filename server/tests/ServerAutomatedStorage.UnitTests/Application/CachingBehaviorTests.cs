using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ServerAutomatedStorage.Application.Common.Behaviors;
using ServerAutomatedStorage.Application.Common.Interfaces;

namespace ServerAutomatedStorage.UnitTests.Application
{
    public class CachingBehaviorTests
    {
        private sealed record CachedQuery(int Id) : IRequest<string>, ICacheableQuery
        {
            public string CacheKey => $"test:{Id}";
            public TimeSpan CacheDuration => TimeSpan.FromSeconds(30);
        }

        private sealed record PlainQuery : IRequest<string>;

        private static IDistributedCache NewCache() =>
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

        private static CachingBehavior<TRequest, string> NewBehavior<TRequest>(IDistributedCache cache)
            where TRequest : notnull =>
            new(cache, NullLogger<CachingBehavior<TRequest, string>>.Instance);

        private static RequestHandlerDelegate<string> Handler(Func<string> produce, Action onCall) =>
            _ =>
            {
                onCall();
                return Task.FromResult(produce());
            };

        [Fact]
        public async Task SecondCall_WithSameKey_IsServedFromCache()
        {
            var behavior = NewBehavior<CachedQuery>(NewCache());
            var calls = 0;
            var next = Handler(() => "value", () => calls++);

            var first = await behavior.Handle(new CachedQuery(1), next, CancellationToken.None);
            var second = await behavior.Handle(new CachedQuery(1), next, CancellationToken.None);

            Assert.Equal("value", first);
            Assert.Equal("value", second);
            Assert.Equal(1, calls);
        }

        [Fact]
        public async Task DifferentKeys_AreCachedSeparately()
        {
            var behavior = NewBehavior<CachedQuery>(NewCache());
            var calls = 0;
            var next = Handler(() => "value", () => calls++);

            await behavior.Handle(new CachedQuery(1), next, CancellationToken.None);
            await behavior.Handle(new CachedQuery(2), next, CancellationToken.None);

            Assert.Equal(2, calls);
        }

        [Fact]
        public async Task RequestWithoutCacheContract_AlwaysReachesHandler()
        {
            var behavior = NewBehavior<PlainQuery>(NewCache());
            var calls = 0;
            var next = Handler(() => "value", () => calls++);

            await behavior.Handle(new PlainQuery(), next, CancellationToken.None);
            await behavior.Handle(new PlainQuery(), next, CancellationToken.None);

            Assert.Equal(2, calls);
        }

        [Fact]
        public async Task WhenCacheIsDown_RequestStillReturnsFreshData()
        {
            var cache = Substitute.For<IDistributedCache>();
            cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("redis down"));
            cache.SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("redis down"));
            var behavior = NewBehavior<CachedQuery>(cache);
            var next = Handler(() => "fresh", () => { });

            var result = await behavior.Handle(new CachedQuery(1), next, CancellationToken.None);

            Assert.Equal("fresh", result);
        }
    }
}
