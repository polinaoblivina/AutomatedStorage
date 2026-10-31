using ServerAutomatedStorage.Domain.Entities;

namespace ServerAutomatedStorage.Domain.Interfaces
{
    public interface IItemRepository : IRepository<Item>
    {
        Task<IReadOnlyList<Item>> GetAllWithCellAsync(bool includeArchived, CancellationToken ct = default);
        Task<Item?> GetWithCellAsync(Guid id, CancellationToken ct = default);
    }
}
