using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServerAutomatedStorage.Application.Common.Interfaces;
using ServerAutomatedStorage.Domain.Interfaces;
using ServerAutomatedStorage.Infrastructure.Authentication;
using ServerAutomatedStorage.Infrastructure.Identity;
using ServerAutomatedStorage.Infrastructure.Persistence;
using ServerAutomatedStorage.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using ServerAutomatedStorage.Infrastructure.Caching;
using StackExchange.Redis;

namespace ServerAutomatedStorage.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("Postgres") ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<ICabinetRepository, CabinetRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IItemRepository, ItemRepository>();
            services.AddScoped<ICellRepository, CellRepository>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IDeviceSessionRepository, DeviceSessionRepository>();

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddErrorDescriber<RussianIdentityErrorDescriber>()
            .AddDefaultTokenProviders();
            services.AddApiAuthentication(configuration);

            services.AddScoped<IIdentityService, IdentityService>();

            AddCaching(services, configuration);

            return services;
        }

        private static void AddCaching(IServiceCollection services, IConfiguration configuration)
        {
            var redisConnectionString = configuration.GetConnectionString("Redis");

            if (string.IsNullOrWhiteSpace(redisConnectionString))
            {
                services.AddDistributedMemoryCache();
                services.AddSingleton<IRateLimiter, NullRateLimiter>();
                return;
            }

            var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
            redisOptions.AbortOnConnectFail = false;
            redisOptions.ConnectTimeout = 1000;
            redisOptions.SyncTimeout = 500;
            redisOptions.AsyncTimeout = 500;

            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOptions));
            services.AddOptions<RedisCacheOptions>().Configure<IConnectionMultiplexer>((options, multiplexer) =>
                options.ConnectionMultiplexerFactory = () => Task.FromResult(multiplexer));
            services.AddStackExchangeRedisCache(_ => { });
            services.AddSingleton<IRateLimiter, RedisRateLimiter>();
        }
    }
}
