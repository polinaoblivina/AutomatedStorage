using Microsoft.EntityFrameworkCore.Storage;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Infrastructure.Persistence
{
    public sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
            context.SaveChangesAsync(ct);

        public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default) =>
            new EfTransaction(await context.Database.BeginTransactionAsync(ct));

        private sealed class EfTransaction(IDbContextTransaction transaction) : IUnitOfWorkTransaction
        {
            public Task CommitAsync(CancellationToken ct = default) => transaction.CommitAsync(ct);
            public ValueTask DisposeAsync() => transaction.DisposeAsync();
        }
    }
}
