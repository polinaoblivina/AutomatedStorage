using Microsoft.EntityFrameworkCore;
using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Infrastructure.Persistence.Repositories
{
    public class ItemRepository(AppDbContext context) : Repository<Item>(context), IItemRepository
    {
        public async Task<IReadOnlyList<Item>> GetAllWithCellAsync(bool includeArchived, CancellationToken ct = default)
        {
            var query = DbSet
                        .AsNoTracking()
                        .Include(i => i.Cell)
                        .ThenInclude(c => c!.Cabinet)
                        .AsQueryable();

            if (!includeArchived)
                query = query.Where(i => i.CellId != null);

            return await query.OrderBy(i => i.Name).ToListAsync(ct);
        }

        public async Task<Item?> GetWithCellAsync(Guid id, CancellationToken ct = default)

        {
            return await DbSet
                        .Include(i => i.Cell)
                        .ThenInclude(c => c!.Cabinet)
                        .FirstOrDefaultAsync(i => i.Id == id, ct);
        }
    }
}
