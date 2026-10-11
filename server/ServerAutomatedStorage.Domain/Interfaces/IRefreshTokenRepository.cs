using ServerAutomatedStorage.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Domain.Interfaces
{
    public interface IRefreshTokenRepository : IRepository<RefreshToken>
    {
        Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
        Task RevokeFamilyAsync(Guid familyId, DateTime nowUtc, CancellationToken ct = default);
        Task RevokeAllForUserAsync(Guid userId, DateTime nowUtc, CancellationToken ct = default);
        Task DeleteExpiredAsync(Guid userId, DateTime nowUtc, CancellationToken ct = default);
    }
}
