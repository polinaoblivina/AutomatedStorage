using ServerAutomatedStorage.Domain.Entities;

namespace ServerAutomatedStorage.Domain.Interfaces
{
    public interface ICabinetRepository : IRepository<Cabinet>
    {
        Task<Cabinet?> GetWithCellsAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<Cabinet>> GetAllWithCellsAsync(CancellationToken ct = default);
    }
}
