using System;
using System.Collections.Generic;
using ServerAutomatedStorage.Domain.Entities;
using System.Text;

namespace ServerAutomatedStorage.Domain.Interfaces
{
     public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByIdWithRoleAsync(Guid id, CancellationToken ct = default);
        Task<User?> GetByCardUidAsync(string cardUid, CancellationToken ct = default);
        Task<IReadOnlyList<User>> GetAllWithRolesAsync(CancellationToken ct = default);

    }
}
