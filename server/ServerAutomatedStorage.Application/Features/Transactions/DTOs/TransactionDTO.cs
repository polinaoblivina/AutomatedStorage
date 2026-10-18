using System;
using System.Collections.Generic;
using System.Text;
using ServerAutomatedStorage.Domain.Entities;

namespace ServerAutomatedStorage.Application.Features.Transactions.DTOs
{
    public sealed record TransactionDTO(
        Guid Id,
        DateTime CreatedAtUtc,
        Guid UserId,
        string? UserFullName,
        Guid ItemId,
        string? ItemName,
        string? CellName,
        string? CabinetName,
        string Type,
        int Quantity,
        double WeightBefore,
        double WeightAfter)
    {
        public static TransactionDTO From(Transaction t) => new(
            t.Id, t.CreatedAtUtc,
            t.UserId, t.User?.FullName,
            t.ItemId, t.Item?.Name, t.Item?.Cell?.Name, t.Item?.Cell?.Cabinet?.Name,
            t.Type.ToString(), t.Quantity, t.WeightBefore, t.WeightAfter);
    }
}
