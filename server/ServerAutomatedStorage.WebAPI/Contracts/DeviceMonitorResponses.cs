namespace ServerAutomatedStorage.WebAPI.Contracts
{
    public class DeviceMonitorResponses
    {
        public enum DeviceEventType
        {
            CardScan,
            Session
        }

        public sealed record DeviceEventDTO(
            DateTime OccurredAtUtc,
            string DeviceName,
            DeviceEventType Type,
            string? CardUid,
            string? UserName,
            bool Success,
            string Message);

        public sealed record DeviceStatusDTO(
            string Name,
            bool IsOnline,
            DateTime? LastSeenUtc,
            double? WeightGrams,
            bool? RfidOk,
            bool? ScaleOk,
            DeviceState? State,
            string? FirmwareVersion,
            string? RemoteAddress,
            DeviceEventDTO? LastEvent,
            IReadOnlyList<ScaleReading>? Scales);
    }
}
