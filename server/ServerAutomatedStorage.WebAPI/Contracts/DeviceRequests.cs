namespace ServerAutomatedStorage.WebAPI.Contracts
{
    public sealed record CheckCardRequest(string CardUid);
    public sealed record RecordSessionRequest(string CardUid, Guid CellId, double WeightBefore, double WeightAfter);
}
