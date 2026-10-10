using System;
using System.Collections.Generic;
using System.Text;
using ServerAutomatedStorage.Domain.Entities;

namespace ServerAutomatedStorage.Domain.Interfaces
{
    public interface ITransactionRepository : IRepository<Transaction>
    {
        Task<bool> AnyForItemAsync(Guid itemId, CancellationToken ct = default);
    }
}
