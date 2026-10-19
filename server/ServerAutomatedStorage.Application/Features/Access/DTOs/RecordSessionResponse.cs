using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Access.DTOs
{
    public sealed record RecordSessionResponse(
        bool Success,
        string Message,
        string? ItemName = null,
        string? TransactionType = null,
        int Quantity = 0,
        int? StockQuantity = null);
}
