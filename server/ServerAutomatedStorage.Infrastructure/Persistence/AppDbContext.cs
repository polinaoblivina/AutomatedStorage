using Microsoft.EntityFrameworkCore;
using ServerAutomatedStorage.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Infrastructure.Persistence
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Cabinet> Cabinets => Set<Cabinet>();
        public DbSet<Cell> Cells => Set<Cell>();
        public DbSet<Item> Items => Set<Item>();
        public DbSet<User> StorageUsers => Set<User>();
        public DbSet<Role> StorageRoles => Set<Role>();
        public DbSet<Transaction> Transactions => Set<Transaction>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
