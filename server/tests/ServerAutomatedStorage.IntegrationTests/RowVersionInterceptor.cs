using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ServerAutomatedStorage.IntegrationTests
{
    public sealed class RowVersionInterceptor : SaveChangesInterceptor
    {
        private const string VersionProperty = "Version";

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            Apply(eventData.Context);
            return result;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Apply(eventData.Context);
            return ValueTask.FromResult(result);
        }

        private static void Apply(DbContext? context)
        {
            if (context is null)
                return;

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.State is not (EntityState.Added or EntityState.Modified))
                    continue;

                if (entry.Metadata.FindProperty(VersionProperty) is not { IsConcurrencyToken: true })
                    continue;

                var property = entry.Property(VersionProperty);
                var original = property.OriginalValue is uint value ? value : 0u;
                property.CurrentValue = entry.State == EntityState.Added ? 1u : original + 1;
            }
        }
    }
}
