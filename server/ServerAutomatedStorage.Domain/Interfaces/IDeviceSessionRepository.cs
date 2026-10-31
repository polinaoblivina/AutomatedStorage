using ServerAutomatedStorage.Domain.Entities;

namespace ServerAutomatedStorage.Domain.Interfaces
{
    public interface IDeviceSessionRepository : IRepository<DeviceSession>
    {
        Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    }
}
