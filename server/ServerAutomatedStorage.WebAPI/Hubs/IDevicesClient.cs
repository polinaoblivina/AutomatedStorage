using static ServerAutomatedStorage.WebAPI.Contracts.DeviceMonitorResponses;

namespace ServerAutomatedStorage.WebAPI.Hubs
{
    public interface IDevicesClient
    {
        Task DeviceStatusChanged(DeviceStatusDTO status);
        Task DeviceEventRecorded(DeviceEventDTO deviceEvent);
    }
}
