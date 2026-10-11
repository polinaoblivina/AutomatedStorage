using Microsoft.EntityFrameworkCore;
using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Infrastructure.Persistence.Repositories
{
    public sealed class RefreshTokenRepository(AppDbContext context)
        : Repository<RefreshToken>(context), IRefreshTokenRepository
    {
        public async Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default) =>
            await DbSet.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        public async Task RevokeFamilyAsync(Guid familyId, DateTime nowUtc, CancellationToken ct = default) =>
            await DbSet
                .Where(t => t.FamilyId == familyId && t.RevokedAtUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAtUtc, nowUtc), ct);

        public async Task RevokeAllForUserAsync(Guid userId, DateTime nowUtc, CancellationToken ct = default) =>
            await DbSet
                .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAtUtc, nowUtc), ct);

        public async Task DeleteExpiredAsync(Guid userId, DateTime nowUtc, CancellationToken ct = default) =>
            await DbSet
                .Where(t => t.UserId == userId && t.ExpiresAtUtc < nowUtc)
                .ExecuteDeleteAsync(ct);
    }
}
