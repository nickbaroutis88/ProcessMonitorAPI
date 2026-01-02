using System.Linq.Expressions;

namespace ProcessMonitorApi.Repository;

public interface ISQLiteRepository
{
    Task<bool> AddAsync<T>(T entity) where T : class;
    Task<bool> UpdateAsync<T>(T entity) where T : class;
    Task<bool> DeleteAsync<T>(T entity) where T : class;
    Task<T?> GetEntityAsync<T>(Expression<Func<T, bool>> predicate) where T : class;
    Task<IEnumerable<T>?> GetAllAsync<T>() where T : class;
}
