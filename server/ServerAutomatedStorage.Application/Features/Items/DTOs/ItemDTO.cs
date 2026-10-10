using ServerAutomatedStorage.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Items.DTOs
{
    public sealed record ItemDTO(
        Guid Id,
        string Name,
        double UnitWeightGrams,
        int StockQuantity,
        int MinQuantity,
        bool IsLowStock,
        bool IsArchived,
        Guid? CellId,
        string? CellName,
        Guid? CabinetId,
        string? CabinetName,
        DateTime CreatedAtUtc)
    {
        public static ItemDTO From(Item item) => new(
            item.Id, item.Name, item.UnitWeightGrams, item.StockQuantity, item.MinQuantity,
            item.IsLowStock, item.IsArchived,
            item.CellId, item.Cell?.Name, item.Cell?.CabinetId, item.Cell?.Cabinet?.Name,
            item.CreatedAtUtc);
    }
}
