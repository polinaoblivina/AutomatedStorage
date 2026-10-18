namespace ServerAutomatedStorage.WebAPI.Contracts
{
    public sealed record CheckCardRequest(string CardUid);
    public sealed record RecordSessionRequest(string CardUid, Guid CellId, double WeightBefore, double WeightAfter);

    public enum DeviceState
    {
        Idle,
        SessionOpen
    }

    public sealed record ScaleReading(string? CellId, double WeightGrams, bool Ok);

    public sealed record HeartbeatRequest(
        double WeightGrams,
        bool RfidOk,
        bool ScaleOk,
        DeviceState State = DeviceState.Idle,
        string? FirmwareVersion = null,
        IReadOnlyList<ScaleReading>? Scales = null);
}
