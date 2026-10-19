using Microsoft.EntityFrameworkCore;
using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Infrastructure.Persistence.Repositories
{
    public sealed class DeviceSessionRepository(AppDbContext context)
        : Repository<DeviceSession>(context), IDeviceSessionRepository
    {
        public async Task<bool> ExistsAsync(string key, CancellationToken ct = default) =>
            await DbSet.AnyAsync(s => s.Key == key, ct);
    }
}
