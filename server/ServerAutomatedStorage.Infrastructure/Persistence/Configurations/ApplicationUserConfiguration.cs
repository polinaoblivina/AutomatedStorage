using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Infrastructure.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Infrastructure.Persistence.Configurations
{
    internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.HasOne<User>()
                .WithOne()
                .HasForeignKey<ApplicationUser>(u => u.DomainUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
