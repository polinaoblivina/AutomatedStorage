using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServerAutomatedStorage.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Infrastructure.Persistence.Configurations
{
    internal sealed class DeviceSessionConfiguration : IEntityTypeConfiguration<DeviceSession>
    {
        public void Configure(EntityTypeBuilder<DeviceSession> builder)
        {
            builder.ToTable("DeviceSessions");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Key)
                .IsRequired()
                .HasMaxLength(64);
            builder.HasIndex(s => s.Key)
                .IsUnique();

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
