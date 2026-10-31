using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ServerAutomatedStorage.Domain.Entities;

namespace ServerAutomatedStorage.IntegrationTests
{
    public sealed class SqliteModelCustomizer(ModelCustomizerDependencies dependencies) : RelationalModelCustomizer(dependencies)
    {
        public override void Customize(ModelBuilder modelBuilder, DbContext context)
        {
            base.Customize(modelBuilder, context);

            modelBuilder.Entity<RefreshToken>()
                .Property<uint>("Version")
                .ValueGeneratedNever();
        }
    }
}
