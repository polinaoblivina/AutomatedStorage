namespace ServerAutomatedStorage.WebAPI.Contracts
{
    public sealed record UpdateItemRequest(string Name, double UnitWeightGrams, int MinQuantity);

    public sealed record MoveItemRequest(Guid CellId);

    public sealed record ReplenishStockRequest(int Quantity);

    public sealed record AdjustStockRequest(int ActualQuantity);
}
}
