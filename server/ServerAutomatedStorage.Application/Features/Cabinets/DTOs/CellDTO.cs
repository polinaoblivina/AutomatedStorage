using ServerAutomatedStorage.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Cabinets.DTOs
{
    public sealed record CellItemDTO(
        Guid Id,
        string Name,
        double UnitWeightGrams,
        int StockQuantity,
        int MinQuantity,
        bool IsLowStock);

    public sealed record CellDTO(Guid Id, string Name, CellItemDTO? Item)
    {
        public static CellDTO From(Cell cell) => new(
            cell.Id,
            cell.Name,
            cell.Item is { } item
                ? new CellItemDTO(item.Id, item.Name, item.UnitWeightGrams, item.StockQuantity, item.MinQuantity, item.IsLowStock)
                : null);
    }
}
