using Microsoft.EntityFrameworkCore;
using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Infrastructure.Persistence.Repositories
{
    public class TransactionRepository(AppDbContext context) : Repository<Transaction>(context), ITransactionRepository
    {
        public async Task<bool> AnyForItemAsync(Guid itemId, CancellationToken ct = default)
        {
            return await DbSet.AnyAsync(t => t.ItemId == itemId, ct);
        }

        public async Task<(IReadOnlyList<Transaction> Items, int TotalCount)> GetPagedAsync(
            TransactionFilter filter, int skip, int take, CancellationToken ct = default)
        {
            var query = DbSet.AsNoTracking();

            if (filter.UserId is not null)
                query = query.Where(t => t.UserId == filter.UserId);
            if (filter.ItemId is not null)
                query = query.Where(t => t.ItemId == filter.ItemId);
            if (filter.Type is not null)
                query = query.Where(t => t.Type == filter.Type);
            if (filter.FromUtc is not null)
                query = query.Where(t => t.CreatedAtUtc >= filter.FromUtc);
            if (filter.ToUtc is not null)
                query = query.Where(t => t.CreatedAtUtc <= filter.ToUtc);

            var total = await query.CountAsync(ct);

            var items = await query
                .Include(t => t.User)
                .Include(t => t.Item)
                .ThenInclude(i => i!.Cell)
                .ThenInclude(c => c!.Cabinet)
                .OrderByDescending(t => t.CreatedAtUtc)
                .ThenByDescending(t => t.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);

            return (items, total);
        }

        public async Task<IReadOnlyList<TransactionSummary>> GetSummariesSinceAsync(
            DateTime fromUtc, CancellationToken ct = default)
        {
            return await DbSet
                .AsNoTracking()
                .Where(t => t.CreatedAtUtc >= fromUtc)
                .Select(t => new TransactionSummary(
                    t.CreatedAtUtc, t.Type, t.Quantity,
                    t.ItemId, t.Item!.Name,
                    t.UserId, t.User!.FullName))
                .ToListAsync(ct);
        }
    }
}
