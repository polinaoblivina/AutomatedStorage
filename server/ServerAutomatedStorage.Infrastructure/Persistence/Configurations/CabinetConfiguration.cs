using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServerAutomatedStorage.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Infrastructure.Persistence.Configurations
{
    internal sealed class CabinetConfiguration : IEntityTypeConfiguration<Cabinet>
    {
        public void Configure(EntityTypeBuilder<Cabinet> builder)
        {
            builder.ToTable("Cabinets");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(200);
            builder.Property(c => c.Location)
                .IsRequired()
                .HasMaxLength(300);

            builder.HasMany(c => c.Cells)
                .WithOne(cell => cell.Cabinet)
                .HasForeignKey(cell => cell.CabinetId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(c => c.Cells)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
