using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Infrastructure.Persistence.Repositories
{
    public sealed class UserRepository(AppDbContext context) : Repository<User>(context), IUserRepository
    {
        public async Task<User?> GetByIdWithRoleAsync(Guid id, CancellationToken ct = default)
        {
            return await DbSet.Include(u => u.Role)
                              .FirstOrDefaultAsync(u => u.Id == id, ct);
        }

        public async Task<User?> GetByCardUidAsync(string cardUid, CancellationToken ct = default)
        {
            var normalized = User.NormalizeCardUid(cardUid);
            if (normalized is null)
                return null;

            return await DbSet
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.CardUid == normalized, ct);
        }

        public async Task<IReadOnlyList<User>> GetAllWithRolesAsync(CancellationToken ct = default)
        {
            return await DbSet
                .AsNoTracking()
                .Include(u => u.Role)
                .OrderBy(u => u.FullName)
                .ToListAsync(ct);
        }
    }
}
