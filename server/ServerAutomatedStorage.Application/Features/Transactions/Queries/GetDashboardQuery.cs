using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Features.Items.DTOs;
using ServerAutomatedStorage.Application.Features.Transactions.DTOs;
using ServerAutomatedStorage.Domain.Enums;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Transactions.Queries
{
    public sealed record GetDashboardQuery(int Days = 14, int TimezoneOffsetMinutes = 0) : IRequest<DashboardDTO>;

    public sealed class GetDashboardValidator : AbstractValidator<GetDashboardQuery>
    {
        public GetDashboardValidator()
        {
            RuleFor(x => x.Days).InclusiveBetween(1, 90);
            RuleFor(x => x.TimezoneOffsetMinutes).InclusiveBetween(-14 * 60, 14 * 60);
        }
    }

    public sealed class GetDashboardHandler(
        ICabinetRepository cabinetRepository,
        IItemRepository itemRepository,
        IUserRepository userRepository,
        ITransactionRepository transactionRepository,
        TimeProvider timeProvider) : IRequestHandler<GetDashboardQuery, DashboardDTO>
    {
        private const int TopSize = 5;

        public async Task<DashboardDTO> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
        {
            var offset = TimeSpan.FromMinutes(request.TimezoneOffsetMinutes);
            var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

            DateOnly LocalDay(DateTime utc) => DateOnly.FromDateTime(utc + offset);

            var today = LocalDay(nowUtc);
            var firstDay = today.AddDays(1 - request.Days);
            var fromUtc = firstDay.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) - offset;

            var cabinets = await cabinetRepository.GetAllWithCellsAsync(cancellationToken);
            var items = await itemRepository.GetAllWithCellAsync(false, cancellationToken);
            var users = await userRepository.GetAllWithRolesAsync(cancellationToken);
            var summaries = await transactionRepository.GetSummariesSinceAsync(fromUtc, cancellationToken);

            var byDay = summaries.ToLookup(s => LocalDay(s.CreatedAtUtc));

            var daily = Enumerable.Range(0, request.Days)
                .Select(i => firstDay.AddDays(i))
                .Select(day => new DailyMovementDTO(
                    day,
                    byDay[day].Where(s => s.Type == TransactionType.Withdrawal).Sum(s => s.Quantity),
                    byDay[day].Where(s => s.Type == TransactionType.Replenishment).Sum(s => s.Quantity),
                    byDay[day].Count()))
                .ToList();

            var withdrawals = summaries.Where(s => s.Type == TransactionType.Withdrawal).ToList();

            var topItems = withdrawals
                .GroupBy(s => new { s.ItemId, s.ItemName })
                .Select(g => new RankedEntryDTO(g.Key.ItemId, g.Key.ItemName, g.Sum(s => s.Quantity), g.Count()))
                .OrderByDescending(e => e.Quantity)
                .Take(TopSize)
                .ToList();

            var topUsers = withdrawals
                .GroupBy(s => new { s.UserId, s.UserFullName })
                .Select(g => new RankedEntryDTO(g.Key.UserId, g.Key.UserFullName, g.Sum(s => s.Quantity), g.Count()))
                .OrderByDescending(e => e.Quantity)
                .Take(TopSize)
                .ToList();

            var cells = cabinets.SelectMany(c => c.Cells).ToList();
            var lowStock = items.Where(i => i.IsLowStock).OrderBy(i => i.StockQuantity).ToList();

            var totals = new DashboardTotalsDTO(
                cabinets.Count,
                cells.Count,
                cells.Count(c => c.Item is null),
                items.Count,
                items.Sum(i => i.StockQuantity),
                lowStock.Count,
                users.Count(u => u.IsActive),
                byDay[today].Where(s => s.Type == TransactionType.Withdrawal).Sum(s => s.Quantity),
                byDay[today].Count());

            return new DashboardDTO(totals, daily, topItems, topUsers, lowStock.Select(ItemDTO.From).ToList());
        }
    }
}
