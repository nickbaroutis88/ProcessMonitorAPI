using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ProcessMonitorApi.Repository;

public class SQLiteRepository(AppDbContext context) : ISQLiteRepository
{
    public async Task<bool> AddAsync<T>(T entity) where T : class
    {
        try
        {
            await context.Set<T>().AddAsync(entity);
            await context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateAsync<T>(T entity) where T : class
    {
        try
        {
            context.Set<T>().Update(entity);
            await context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteAsync<T>(T entity) where T : class
    {
        try
        {
            context.Set<T>().Remove(entity);
            await context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<T?> GetEntityAsync<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        return await context.Set<T>().FirstOrDefaultAsync(predicate);
    }

    public async Task<IEnumerable<T>?> GetAllAsync<T>() where T : class
    {
        return await context.Set<T>().ToListAsync();
    }
}
