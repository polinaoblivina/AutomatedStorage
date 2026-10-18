using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Domain.Interfaces
{
    public sealed record TransactionFilter(
        Guid? UserId = null,
        Guid? ItemId = null,
        TransactionType? Type = null,
        DateTime? FromUtc = null,
        DateTime? ToUtc = null);

    public sealed record TransactionSummary(
        DateTime CreatedAtUtc,
        TransactionType Type,
        int Quantity,
        Guid ItemId,
        string ItemName,
        Guid UserId,
        string UserFullName);

    public interface ITransactionRepository : IRepository<Transaction>
    {
        Task<bool> AnyForItemAsync(Guid itemId, CancellationToken ct = default);

        Task<(IReadOnlyList<Transaction> Items, int TotalCount)> GetPagedAsync(
            TransactionFilter filter, int skip, int take, CancellationToken ct = default);
        Task<IReadOnlyList<TransactionSummary>> GetSummariesSinceAsync(DateTime fromUtc, CancellationToken ct = default);
    }
}
