using Microsoft.AspNetCore.SignalR;
using ServerAutomatedStorage.WebAPI.Hubs;

namespace ServerAutomatedStorage.WebAPI.Services
{
    public sealed class DeviceStatusWatcher(
        DeviceMonitor monitor,
        IHubContext<DevicesHub, IDevicesClient> hub,
        TimeProvider timeProvider,
        ILogger<DeviceStatusWatcher> logger) : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);

        private readonly Dictionary<string, bool> _lastOnline = new(StringComparer.OrdinalIgnoreCase);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(CheckInterval, timeProvider);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                foreach (var status in monitor.GetStatuses())
                {
                    var wasOnline = _lastOnline.GetValueOrDefault(status.Name);
                    _lastOnline[status.Name] = status.IsOnline;

                    if (wasOnline && !status.IsOnline)
                    {
                        logger.LogWarning("Устройство {Device} перестало выходить на связь", status.Name);
                        await hub.Clients.All.DeviceStatusChanged(status);
                    }
                }
            }
        }
    }
}
