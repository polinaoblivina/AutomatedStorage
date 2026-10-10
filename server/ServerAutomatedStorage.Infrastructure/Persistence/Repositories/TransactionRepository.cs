using Microsoft.EntityFrameworkCore;
using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Infrastructure.Persistence.Repositories
{
    public class TransactionRepositroy(AppDbContext context) : Repository<Transaction>(context), ITransactionRepository
    {
        public async Task<bool> AnyForItemAsync(Guid itemId, CancellationToken ct = default)
        {
            return await DbSet.AnyAsync(t => t.ItemId == itemId, ct);
        }
    }
}
