namespace ServerAutomatedStorage.Application.Common.Interfaces
{
    public interface ICacheableQuery
    {
        string CacheKey { get; }
        TimeSpan CacheDuration { get; }
    }
}
