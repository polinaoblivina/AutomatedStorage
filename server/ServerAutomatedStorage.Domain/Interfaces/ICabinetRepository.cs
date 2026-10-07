using System;
using System.Collections.Generic;
using System.Text;
using ServerAutomatedStorage.Domain.Entities;

namespace ServerAutomatedStorage.Domain.Interfaces
{
    public interface ICabinetRepository : IRepository<Cabinet>
    {
        Task<Cabinet?> GetWithCellsAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<Cabinet>> GatAllWithCellsAsync(CancellationToken ct = default);
    }
}
