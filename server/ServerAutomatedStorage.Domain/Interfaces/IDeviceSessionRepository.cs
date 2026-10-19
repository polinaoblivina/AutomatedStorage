using ServerAutomatedStorage.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Domain.Interfaces
{
    public interface IDeviceSessionRepository : IRepository<DeviceSession>
    {
        Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    }
}
