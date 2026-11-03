using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using ServerAutomatedStorage.Application.Common.Interfaces;
using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Enums;
using ServerAutomatedStorage.Infrastructure.Identity;
using ServerAutomatedStorage.Infrastructure.Persistence;

namespace ServerAutomatedStorage.IntegrationTests
{
    public sealed class SwitchableRateLimiter : IRateLimiter
    {
        public bool Deny { get; set; }

        public Task<RateLimitResult> HitAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken)
            => Task.FromResult(Deny ? new RateLimitResult(false, TimeSpan.FromSeconds(42)) : new RateLimitResult(true, TimeSpan.Zero));
    }

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        public const string DeviceName = "test-cabinet";
        public const string DeviceKey = "test-device-key-0123456789abcdef";
        public const string AdminEmail = "admin@test.local";
        public const string AdminPassword = "Admin123!";

        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly SemaphoreSlim _tokenLock = new(1, 1);
        private string? _adminToken;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.UseSetting("ConnectionStrings:Postgres", "Host=localhost;Database=smartstorage_tests_unused");
            builder.UseSetting("Jwt:Secret", "integration-tests-secret-at-least-32-chars!");
            builder.UseSetting("Jwt:Issuer", "SmartStorage");
            builder.UseSetting("Jwt:Audience", "SmartStorage");
            builder.UseSetting("Devices:0:Name", DeviceName);
            builder.UseSetting("Devices:0:ApiKey", DeviceKey);

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.RemoveAll<IRateLimiter>();
                services.AddSingleton<SwitchableRateLimiter>();
                services.AddSingleton<IRateLimiter>(sp => sp.GetRequiredService<SwitchableRateLimiter>());
                _connection.Open();
                services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection).AddInterceptors(new RowVersionInterceptor()).ReplaceService<IModelCustomizer, SqliteModelCustomizer>());
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);
            SeedAsync(host.Services).GetAwaiter().GetResult();
            return host;
        }

        public async Task<string> AdminTokenAsync()
        {
            await _tokenLock.WaitAsync();
            try
            {
                if (_adminToken is not null)
                    return _adminToken;

                using var client = CreateClient();
                _adminToken = await ApiClient.LoginAsync(client, AdminEmail, AdminPassword);
                return _adminToken;
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        private static async Task SeedAsync(IServiceProvider services)
        {
            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await context.Database.EnsureCreatedAsync();

            var admin = new Role("Admin", Permission.OpenCell | Permission.ManageStock | Permission.ViewJournal
                                          | Permission.ManageUsers | Permission.ManageSystem);
            context.StorageRoles.Add(admin);
            context.StorageRoles.Add(new Role("Storekeeper", Permission.OpenCell | Permission.ManageStock | Permission.ViewJournal));
            context.StorageRoles.Add(new Role("Employee", Permission.OpenCell));

            var adminUser = new User("Администратор", admin);
            context.StorageUsers.Add(adminUser);
            await context.SaveChangesAsync();

            var result = await userManager.CreateAsync(new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                EmailConfirmed = true,
                DomainUserId = adminUser.Id
            }, AdminPassword);

            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                _connection.Dispose();
        }
    }
}
