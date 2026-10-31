using ServerAutomatedStorage.Domain.Entities;

namespace ServerAutomatedStorage.Domain.Interfaces
{
    public interface ICellRepository : IRepository<Cell>
    {
        Task<Cell?> GetWithItemAsync(Guid id, CancellationToken ct = default);
    }
}
