using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Enums;
using ServerAutomatedStorage.Infrastructure.Identity;

namespace ServerAutomatedStorage.Infrastructure.Persistence
{
    public sealed class SeedSettings
    {
        public string AdminFullName { get; set; } = "Администратор";
        public string AdminEmail { get; set; } = string.Empty;
        public string AdminPassword { get; set; } = string.Empty;
    }

    public static class DbInitializer
    {
        public const string AdminRole = "Admin";
        public const string StorekeeperRole = "Storekeeper";
        public const string EmployeeRole = "Employee";

        private static readonly (string Name, Permission Permissions)[] DefaultRoles =
        [
            (AdminRole, Permission.OpenCell | Permission.ManageStock | Permission.ViewJournal
                        | Permission.ManageUsers | Permission.ManageSystem),
            (StorekeeperRole, Permission.OpenCell | Permission.ManageStock | Permission.ViewJournal),
            (EmployeeRole, Permission.OpenCell)
        ];

        public static async Task InitialiseDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
        {
            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbInitializer));

            await context.Database.MigrateAsync(ct);

            await SeedRolesAsync(context, ct);

            var seed = configuration.GetSection("Seed").Get<SeedSettings>() ?? new SeedSettings();
            await SeedAdminAsync(context, userManager, seed, logger, ct);
        }

        private static async Task SeedRolesAsync(AppDbContext context, CancellationToken ct)
        {
            var existing = await context.StorageRoles.Select(r => r.Name).ToListAsync(ct);

            foreach (var (name, permissions) in DefaultRoles.Where(r => !existing.Contains(r.Name)))
                context.StorageRoles.Add(new Role(name, permissions));

            await context.SaveChangesAsync(ct);
        }

        private static async Task SeedAdminAsync(
            AppDbContext context, UserManager<ApplicationUser> userManager,
            SeedSettings seed, ILogger logger, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(seed.AdminEmail) || string.IsNullOrWhiteSpace(seed.AdminPassword))
            {
                logger.LogWarning("Seed:AdminEmail / Seed:AdminPassword are not configured, admin was not created");
                return;
            }

            if (await userManager.FindByEmailAsync(seed.AdminEmail) is not null)
                return;

            var adminRole = await context.StorageRoles.FirstAsync(r => r.Name == AdminRole, ct);

            await using var transaction = await context.Database.BeginTransactionAsync(ct);

            var admin = new User(seed.AdminFullName, adminRole);
            context.StorageUsers.Add(admin);
            await context.SaveChangesAsync(ct);

            var result = await userManager.CreateAsync(new ApplicationUser
            {
                UserName = seed.AdminEmail,
                Email = seed.AdminEmail,
                EmailConfirmed = true,
                DomainUserId = admin.Id
            }, seed.AdminPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to seed admin: {errors}");
            }

            await transaction.CommitAsync(ct);
            logger.LogInformation("Seeded admin {Email}", seed.AdminEmail);
        }
    }
}

