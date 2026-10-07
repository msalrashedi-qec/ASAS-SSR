using Core.Enums;
using System.Linq.Expressions;

namespace Core.Interfaces
{
    public interface IBaseRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<T> GetMax(Expression<Func<T, bool>> criteria, Expression<Func<T, object>> valueSelector);
        Task<T> FindAsync(Expression<Func<T, bool>> criteria);
        Task<T> FindAsync(Expression<Func<T, bool>> criteria, string[] includes);
        Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> criteria, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> criteria,
            Expression<Func<T, object>> orderBy, string orderByDirection = OrderBy.Ascending, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> criteria, string[] includes, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> criteria, string[] includes,
            Expression<Func<T, object>> orderBy, string orderByDirection = OrderBy.Ascending, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> criteria, int skip, int take, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> criteria, int skip, int take,
            Expression<Func<T, object>> orderBy, string orderByDirection = OrderBy.Ascending, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> criteria, string[] includes, int skip, int take,
            Expression<Func<T, object>> orderBy, string orderByDirection = OrderBy.Ascending, CancellationToken cancellationToken = default);
        Task<T> AddAsync(T entity);
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
        Task<T> Update(T entity);
        Task UpdateRange(IEnumerable<T> entities);
        Task<bool> Delete(T entity);
        int DeleteRange(IEnumerable<T> entities);
        Task<int> CountAsync();
        Task<int> CountAsync(Expression<Func<T, bool>> criteria);
        Task<bool> Existing(Expression<Func<T, bool>> criteria);

    }
}
