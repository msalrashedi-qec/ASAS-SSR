using Core.Enums;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly Func<ApplicationDbContext> _getWriteContext;
        private readonly SemaphoreSlim _writeGate;

        public BaseRepository(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            Func<ApplicationDbContext> getWriteContext,
            SemaphoreSlim writeGate)
        {
            _contextFactory = contextFactory;
            _getWriteContext = getWriteContext;
            _writeGate = writeGate;
        }

        public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Set<T>().AsNoTracking().ToListAsync(cancellationToken);
        }

        public async Task<T> GetMax(
            Expression<Func<T, bool>> criteria,
            Expression<Func<T, object>> valueSelector)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<T>()
                .Where(criteria)
                .OrderByDescending(valueSelector)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task<T> FindAsync(Expression<Func<T, bool>> criteria)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<T>().AsNoTracking().FirstOrDefaultAsync(criteria);
        }

        public async Task<T> FindAsync(Expression<Func<T, bool>> criteria, string[] includes)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            IQueryable<T> query = context.Set<T>();

            if (includes != null)
                foreach (var include in includes)
                    query = query.Include(include);

            return await query.AsNoTracking().FirstOrDefaultAsync(criteria);
        }

        public async Task<IEnumerable<T>> FindAllAsync(
            Expression<Func<T, bool>> criteria,
            CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Set<T>()
                .Where(criteria)
                .Distinct()
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<T>> FindAllAsync(
            Expression<Func<T, bool>> criteria,
            Expression<Func<T, object>> orderBy,
            string orderByDirection = OrderBy.Ascending,
            CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            IQueryable<T> query = context.Set<T>().Where(criteria).Distinct();

            if (orderBy != null)
                query = orderByDirection == OrderBy.Ascending
                    ? query.OrderBy(orderBy)
                    : query.OrderByDescending(orderBy);

            return await query.AsNoTracking().ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<T>> FindAllAsync(
            Expression<Func<T, bool>> criteria,
            string[] includes,
            CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            IQueryable<T> query = context.Set<T>().Where(criteria).Distinct();

            if (includes != null)
                foreach (var include in includes)
                    query = query.Include(include);

            return await query.AsNoTracking().ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> criteria, string[] includes, Expression<Func<T, object>> orderBy, string orderByDirection = OrderBy.Ascending, CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            IQueryable<T> query = context.Set<T>().Where(criteria).Distinct();

            if (orderBy != null)
                query = orderByDirection == OrderBy.Ascending
                    ? query.OrderBy(orderBy)
                    : query.OrderByDescending(orderBy);

            if (includes != null)
                foreach (var include in includes)
                    query = query.Include(include);

            return await query.AsNoTracking().ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<T>> FindAllAsync(
            Expression<Func<T, bool>> criteria,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Set<T>()
                .Where(criteria)
                .Distinct()
                .Skip(skip)
                .Take(take)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<T>> FindAllAsync(
            Expression<Func<T, bool>> criteria,
            int skip,
            int take,
            Expression<Func<T, object>> orderBy,
            string orderByDirection = OrderBy.Ascending,
            CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            IQueryable<T> query = context.Set<T>().Where(criteria).Distinct();

            if (orderBy != null)
                query = orderByDirection == OrderBy.Ascending
                    ? query.OrderBy(orderBy)
                    : query.OrderByDescending(orderBy);

            return await query
                .Skip(skip)
                .Take(take)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<T>> FindAllAsync(
            Expression<Func<T, bool>> criteria,
            string[] includes,
            int skip,
            int take,
            Expression<Func<T, object>> orderBy,
            string orderByDirection = OrderBy.Ascending,
            CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            IQueryable<T> query = context.Set<T>().Where(criteria).Distinct();

            if (orderBy != null)
                query = orderByDirection == OrderBy.Ascending
                    ? query.OrderBy(orderBy)
                    : query.OrderByDescending(orderBy);

            if (includes != null)
                foreach (var include in includes)
                    query = query.Include(include);

            return await query
                .Skip(skip)
                .Take(take)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<T> AddAsync(T entity)
        {
            await _writeGate.WaitAsync();
            try
            {
                await _getWriteContext().Set<T>().AddAsync(entity);
                return entity;
            }
            finally
            {
                _writeGate.Release();
            }
        }

        public async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
        {
            var entityList = entities.ToList();
            await _writeGate.WaitAsync();
            try
            {
                await _getWriteContext().Set<T>().AddRangeAsync(entityList);
                return entityList;
            }
            finally
            {
                _writeGate.Release();
            }
        }

        public async Task<bool> Delete(T entity)
        {
            await _writeGate.WaitAsync();
            try
            {
                _getWriteContext().Set<T>().Remove(entity);
                return true;
            }
            finally
            {
                _writeGate.Release();
            }
        }

        public int DeleteRange(IEnumerable<T> entities)
        {
            var entityList = entities.ToList();
            _writeGate.Wait();
            try
            {
                _getWriteContext().Set<T>().RemoveRange(entityList);
                return entityList.Count;
            }
            finally
            {
                _writeGate.Release();
            }
        }

        public async Task<T> Update(T entity)
        {
            if (entity == null)
                return null;

            await _writeGate.WaitAsync();
            try
            {
                var context = _getWriteContext();
                var entityType = context.Model.FindEntityType(typeof(T));
                var primaryKey = entityType?.FindPrimaryKey();
                var pkProperty = primaryKey?.Properties.FirstOrDefault();

                if (pkProperty != null)
                {
                    var keyValue = entity.GetType().GetProperty(pkProperty.Name)?.GetValue(entity);
                    var trackedEntity = context.Set<T>().Local.FirstOrDefault(existing =>
                        existing.GetType().GetProperty(pkProperty.Name)?.GetValue(existing)?.Equals(keyValue) == true);

                    if (trackedEntity != null && !ReferenceEquals(trackedEntity, entity))
                        context.Entry(trackedEntity).State = EntityState.Detached;
                }

                if (context.Entry(entity).State == EntityState.Detached)
                    context.Set<T>().Attach(entity);

                context.Entry(entity).State = EntityState.Modified;
                return entity;
            }
            finally
            {
                _writeGate.Release();
            }
        }

        public async Task UpdateRange(IEnumerable<T> entities)
        {
            await _writeGate.WaitAsync();
            try
            {
                _getWriteContext().Set<T>().UpdateRange(entities);
            }
            finally
            {
                _writeGate.Release();
            }
        }

        public async Task<int> CountAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<T>().CountAsync();
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> criteria)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<T>().CountAsync(criteria);
        }

        public async Task<bool> Existing(Expression<Func<T, bool>> criteria)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<T>().AnyAsync(criteria);
        }
    }
}
