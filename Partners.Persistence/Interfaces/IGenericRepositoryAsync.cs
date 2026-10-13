using System.Linq.Expressions;

namespace Partners.Persistence.Interfaces
{
    public interface IGenericRepositoryAsync<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<T?> GetByIdAsync(long id);
        Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> equater);
        Task<T?> GetByGUIDAsync(Guid id);

        Task<bool> AnyAsync(Expression<Func<T, bool>> equater);
        Task<IReadOnlyList<T>> GetAllAsync();
        IQueryable<T> Where(Expression<Func<T, bool>> expression);
        Task<IReadOnlyList<T>> GetPagedReponseAsync(int pageNumber, int pageSize);
        Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entitys);
        Task<int> UpdateAsync(T entity, CancellationToken cancellationToken = default);
        Task<int> UpdateAsync(Action<T> setter, Func<T, bool> equater);
        Task DeleteAsync(T entity);
        Task<int> DeleteAsync(Func<T, bool> equater);
    }
}
