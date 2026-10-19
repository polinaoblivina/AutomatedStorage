using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Access.DTOs
{
    public sealed record CellSessionResult(
        Guid CellId,
        bool Success,
        string Message,
        string? ItemName = null,
        string? TransactionType = null,
        int Quantity = 0,
        int? StockQuantity = null)
    {
        public static CellSessionResult From(Guid cellId, RecordSessionResponse response) => new(
            cellId, response.Success, response.Message, response.ItemName,
            response.TransactionType, response.Quantity, response.StockQuantity);

        public static CellSessionResult CellNotFound(Guid cellId) => new(cellId, false, "Ячейка не найдена");
    }

    public sealed record RecordSessionsResponse(
        bool Success,
        string Message,
        bool AlreadyRecorded,
        IReadOnlyList<CellSessionResult> Cells);
}

