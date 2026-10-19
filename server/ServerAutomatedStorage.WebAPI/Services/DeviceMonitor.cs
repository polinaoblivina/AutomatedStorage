using Microsoft.Extensions.Options;
using ServerAutomatedStorage.Infrastructure.Authentication;
using ServerAutomatedStorage.WebAPI.Contracts;
using System.Collections.Concurrent;
using static ServerAutomatedStorage.WebAPI.Contracts.DeviceMonitorResponses;

namespace ServerAutomatedStorage.WebAPI.Services
{
    public sealed class DeviceMonitor(IOptionsMonitor<DeviceApiKeyOptions> options, TimeProvider timeProvider)
    {
        private static readonly TimeSpan OnlineWindow = TimeSpan.FromSeconds(30);
        private const int EventCapacity = 100;

        private readonly ConcurrentDictionary<string, DeviceSnapshot> _devices = new(StringComparer.OrdinalIgnoreCase);
        private readonly LinkedList<DeviceEventDTO> _events = new();
        private readonly Lock _eventsLock = new();

        public void RecordHeartbeat(string deviceName, HeartbeatRequest heartbeat, string? remoteAddress)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;

            _devices.AddOrUpdate(
                deviceName,
                _ => new DeviceSnapshot(now, heartbeat, remoteAddress, null),
                (_, current) => current with { LastSeenUtc = now, Heartbeat = heartbeat, RemoteAddress = remoteAddress });
        }

        public DeviceEventDTO RecordEvent(
            string deviceName, DeviceEventType type, string? cardUid, string? userName, bool success, string message)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var deviceEvent = new DeviceEventDTO(now, deviceName, type, cardUid, userName, success, message);

            _devices.AddOrUpdate(
                deviceName,
                _ => new DeviceSnapshot(now, null, null, deviceEvent),
                (_, current) => current with { LastSeenUtc = now, LastEvent = deviceEvent });

            lock (_eventsLock)
            {
                _events.AddFirst(deviceEvent);
                while (_events.Count > EventCapacity)
                    _events.RemoveLast();
            }

            return deviceEvent;
        }

        public IReadOnlyList<DeviceEventDTO> GetEvents(DateTime? sinceUtc, DeviceEventType? type, int take)
        {
            lock (_eventsLock)
            {
                return _events
                    .Where(e => sinceUtc is null || e.OccurredAtUtc > sinceUtc)
                    .Where(e => type is null || e.Type == type)
                    .Take(take)
                    .ToList();
            }
        }

        public IReadOnlyList<DeviceStatusDTO> GetStatuses()
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;

            return options.Get(DeviceApiKeyDefaults.Scheme).Devices
                .Select(d => d.Name)
                .Concat(_devices.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n)
                .Select(name => BuildStatus(name, now))
                .ToList();
        }

        public DeviceStatusDTO GetStatus(string deviceName) =>
            BuildStatus(deviceName, timeProvider.GetUtcNow().UtcDateTime);

                private DeviceStatusDTO BuildStatus(string name, DateTime now)
        {
            if (!_devices.TryGetValue(name, out var snapshot))
                return new DeviceStatusDTO(name, false, null, null, null, null, null, null, null, null, null);

            var heartbeat = snapshot.Heartbeat;
            return new DeviceStatusDTO(
                name,
                now - snapshot.LastSeenUtc <= OnlineWindow,
                snapshot.LastSeenUtc,
                heartbeat?.WeightGrams,
                heartbeat?.RfidOk,
                heartbeat?.ScaleOk,
                heartbeat?.State,
                heartbeat?.FirmwareVersion,
                snapshot.RemoteAddress,
                snapshot.LastEvent,
                heartbeat?.Scales);
        }


        private sealed record DeviceSnapshot(
            DateTime LastSeenUtc,
            HeartbeatRequest? Heartbeat,
            string? RemoteAddress,
            DeviceEventDTO? LastEvent);
    }
}
