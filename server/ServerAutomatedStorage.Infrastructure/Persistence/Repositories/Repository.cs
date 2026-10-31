using Microsoft.EntityFrameworkCore;
using ServerAutomatedStorage.Domain.Common;
using ServerAutomatedStorage.Domain.Interfaces;

namespace ServerAutomatedStorage.Infrastructure.Persistence.Repositories
{
    public class Repository<T>(AppDbContext context) : IRepository<T> where T : Entity
    {
        protected AppDbContext Context { get; } = context;
        protected DbSet<T> DbSet { get; } = context.Set<T>();

        public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await DbSet.FirstOrDefaultAsync(e => e.Id == id, ct);

        public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default) =>
            await DbSet.ToListAsync(ct);

        public async Task AddAsync(T entity, CancellationToken ct = default) =>
            await DbSet.AddAsync(entity, ct);

        public void Remove(T entity) => DbSet.Remove(entity);
    }
}
