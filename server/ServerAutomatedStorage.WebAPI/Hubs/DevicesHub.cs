using Microsoft.AspNetCore.SignalR;
using ServerAutomatedStorage.Domain.Enums;
using ServerAutomatedStorage.WebAPI.Authorization;
using ServerAutomatedStorage.WebAPI.Services;
using static ServerAutomatedStorage.WebAPI.Contracts.DeviceMonitorResponses;

namespace ServerAutomatedStorage.WebAPI.Hubs
{
    [HasPermission(Permission.ViewJournal)]
    public sealed class DevicesHub(DeviceMonitor monitor) : Hub<IDevicesClient>
    {
        public const string Path = "/hubs/devices";

        public IReadOnlyList<DeviceStatusDTO> GetStatuses() => monitor.GetStatuses();
    }
}
