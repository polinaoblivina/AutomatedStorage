using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServerAutomatedStorage.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Infrastructure.Persistence.Configurations
{
    internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.TokenHash)
                .IsRequired()
                .HasMaxLength(64);
            builder.HasIndex(t => t.TokenHash)
                .IsUnique();

            builder.HasIndex(t => t.UserId);
            builder.HasIndex(t => t.FamilyId);

            builder.Property<uint>("Version")
                .IsRowVersion();

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
