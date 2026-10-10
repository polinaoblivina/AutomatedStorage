using ServerAutomatedStorage.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Domain.Interfaces
{
    public interface ICellRepository : IRepository<Cell>
    {
        Task<Cell?> GetWithItemAsync(Guid id, CancellationToken ct = default);
    }
}
