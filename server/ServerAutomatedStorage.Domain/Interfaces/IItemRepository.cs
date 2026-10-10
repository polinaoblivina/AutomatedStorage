using ServerAutomatedStorage.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Domain.Interfaces
{
    public interface IItemRepository : IRepository<Item>
    {
        Task<IReadOnlyList<Item>> GetAllWithCellAsync(bool includeArchived, CancellationToken ct = default);
        Task<Item?> GetWithCellAsync(Guid id, CancellationToken ct = default);
    }
}
