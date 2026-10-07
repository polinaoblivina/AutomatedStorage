using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Infrastructure.Persistence.Repositories
{
    public sealed class CabinetRepository(AppDbContext context) : Repository<Cabinet>(context), ICabinetRepository
    {
        public async Task<Cabinet?> GetWithCellsAsync(Guid id, CancellationToken ct = default)
        {
            return await DbSet
                        .Include(c => c.Cells)
                        .ThenInclude(cell => cell.Item)
                        .FirstOrDefaultAsync(c => c.Id == id, ct);

        }

        public async Task<IReadOnlyList<Cabinet>> GetAllWithCellsAsync(CancellationToken ct)
        {
            return await DbSet
                         .AsNoTracking()
                         .Include(c => c.Cells)
                         .ThenInclude(cell => cell.Item)
                         .OrderBy(c => c.Name)
                         .ToListAsync(ct);
        }
    }
}
