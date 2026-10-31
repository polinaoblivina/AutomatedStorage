using ServerAutomatedStorage.Domain.Entities;

namespace ServerAutomatedStorage.Domain.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByIdWithRoleAsync(Guid id, CancellationToken ct = default);
        Task<User?> GetByCardUidAsync(string cardUid, CancellationToken ct = default);
        Task<IReadOnlyList<User>> GetAllWithRolesAsync(CancellationToken ct = default);

    }
}