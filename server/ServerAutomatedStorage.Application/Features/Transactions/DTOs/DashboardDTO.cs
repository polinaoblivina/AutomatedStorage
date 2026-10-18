using ServerAutomatedStorage.Application.Features.Items.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Transactions.DTOs
{
    public sealed record DashboardTotalsDTO(
        int Cabinets,
        int Cells,
        int FreeCells,
        int Items,
        int TotalUnits,
        int LowStockItems,
        int ActiveUsers,
        int WithdrawnToday,
        int OperationsToday);

    public sealed record DailyMovementDTO(DateOnly Date, int Withdrawn, int Replenished, int Operations);

    public sealed record RankedEntryDTO(Guid Id, string Name, int Quantity, int Operations);

    public sealed record DashboardDTO(
        DashboardTotalsDTO Totals,
        IReadOnlyList<DailyMovementDTO> Daily,
        IReadOnlyList<RankedEntryDTO> TopItems,
        IReadOnlyList<RankedEntryDTO> TopUsers,
        IReadOnlyList<ItemDTO> LowStock);
}
