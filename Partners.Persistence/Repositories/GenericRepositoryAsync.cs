using Microsoft.EntityFrameworkCore;
using Partners.Persistence.DBContexts;
using Partners.Persistence.Interfaces;
using System.Linq.Expressions;

namespace Partners.Persistence.Repositories
{
    public class GenericRepositoryAsync<T> : IGenericRepositoryAsync<T> where T : class
    {
        private readonly AppDbContext _context;
        private readonly DbSet<T> _dbSet;

        public GenericRepositoryAsync(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(int id)
            => await _dbSet.FindAsync(id);

        public async Task<T?> GetByIdAsync(long id)
            => await _dbSet.FindAsync(id);

        public async Task<T?> GetByGUIDAsync(Guid id)
            => await _dbSet.FindAsync(id);

        public async Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> equater)
            => await _dbSet.FirstOrDefaultAsync(equater);

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> equater)
            => await _dbSet.AnyAsync(equater);

        public async Task<IReadOnlyList<T>> GetAllAsync()
            => await _dbSet.ToListAsync();

        public IQueryable<T> Where(Expression<Func<T, bool>> expression)
            => _dbSet.Where(expression);

        public async Task<IReadOnlyList<T>> GetPagedReponseAsync(int pageNumber, int pageSize)
            => await _dbSet
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

        public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entitys)
        {
            await _dbSet.AddRangeAsync(entitys);
            await _context.SaveChangesAsync();
            return entitys;
        }

        public async Task<int> UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            _dbSet.Update(entity);
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> UpdateAsync(Action<T> setter, Func<T, bool> equater)
        {
            var matches = await _dbSet.ToListAsync();
            foreach (var entity in matches.Where(equater))
                setter(entity);

            return await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(T entity)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<int> DeleteAsync(Func<T, bool> equater)
        {
            var matches = (await _dbSet.ToListAsync()).Where(equater).ToList();
            if (matches.Count == 0) return 0;

            _dbSet.RemoveRange(matches);
            return await _context.SaveChangesAsync();
        }
    }
}
