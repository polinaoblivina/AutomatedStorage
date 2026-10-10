using Microsoft.EntityFrameworkCore;
using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Infrastructure.Persistence.Repositories
{
    public class CellRepository(AppDbContext context) : Repository<Cell>(context), ICellRepository
    {
        public async Task<Cell?> GetWithItemAsync(Guid id, CancellationToken ct = default)
        {
            return await DbSet
                        .Include (c => c.Item)
                        .Include(c => c.Cabinet)
                        .FirstOrDefaultAsync (c => c.Id == id, ct);
        }
    }
}
