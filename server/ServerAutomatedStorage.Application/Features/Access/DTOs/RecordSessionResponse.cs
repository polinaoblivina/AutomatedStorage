namespace ServerAutomatedStorage.Application.Features.Access.DTOs
{
    public sealed record CellSessionResult(
        Guid CellId,
        bool Success,
        string Message,
        string? ItemName = null,
        string? TransactionType = null,
        int Quantity = 0,
        int? StockQuantity = null);

    public sealed record RecordSessionResponse(
        bool Success,
        string Message,
        bool AlreadyRecorded,
        IReadOnlyList<CellSessionResult> Cells);
}
