using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using CioSystem.Core;

namespace CioSystem.Data.Repositories
{
    /// <summary>
    /// 優化的 Repository 實現
    /// 提供進階的查詢優化和效能提升
    /// </summary>
    public class OptimizedRepository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly CioSystemDbContext _context;
        protected readonly DbSet<T> _dbSet;
        protected readonly ILogger<OptimizedRepository<T>> _logger;

        public OptimizedRepository(CioSystemDbContext context, ILogger<OptimizedRepository<T>> logger)
        {
            _context = context;
            _dbSet = context.Set<T>();
            _logger = logger;
        }

        /// <summary>
        /// 取得所有實體（優化版本）
        /// </summary>
        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            try
            {
                _logger.LogDebug("執行 GetAllAsync 查詢");
                // 檢查是否有 IsDeleted 屬性
                var hasIsDeleted = typeof(T).GetProperty("IsDeleted") != null;
                if (hasIsDeleted)
                {
                    // 使用反射動態建立查詢條件
                    var parameter = Expression.Parameter(typeof(T), "x");
                    var property = Expression.Property(parameter, "IsDeleted");
                    var constant = Expression.Constant(false);
                    var equal = Expression.Equal(property, constant);
                    var lambda = Expression.Lambda<Func<T, bool>>(equal, parameter);

                    return await _dbSet.Where(lambda).AsNoTracking().ToListAsync();
                }
                else
                {
                    return await _dbSet.AsNoTracking().ToListAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllAsync 執行時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 根據 ID 取得實體（優化版本）
        /// </summary>
        public virtual async Task<T?> GetByIdAsync(int id)
        {
            try
            {
                _logger.LogDebug("執行 GetByIdAsync 查詢，ID: {Id}", id);
                return await _dbSet.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetByIdAsync 執行時發生錯誤，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 根據條件查詢實體（優化版本）
        /// </summary>
        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                _logger.LogDebug("執行 FindAsync 查詢");
                return await _dbSet.Where(predicate).AsNoTracking().ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FindAsync 執行時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 取得分頁資料（優化版本）
        /// </summary>
        public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate = null, Expression<Func<T, object>>? orderBy = null)
        {
            try
            {
                _logger.LogDebug("執行 GetPagedAsync 查詢，頁碼: {PageNumber}, 頁面大小: {PageSize}",
                    pageNumber, pageSize);

                var query = _dbSet.AsQueryable();

                if (predicate != null)
                {
                    query = query.Where(predicate);
                }

                // 應用排序
                if (orderBy != null)
                {
                    query = query.OrderBy(orderBy);
                }
                else
                {
                    query = query.OrderBy(e => e.Id);
                }

                // 並行執行計數和分頁查詢
                var totalCountTask = query.CountAsync();
                var dataTask = query
                    .AsNoTracking()
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                await Task.WhenAll(totalCountTask, dataTask);

                var totalCount = await totalCountTask;
                var data = await dataTask;

                _logger.LogDebug("GetPagedAsync 完成，總計: {TotalCount}, 本頁: {PageCount}",
                    totalCount, data.Count);

                return (data, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPagedAsync 執行時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 計數查詢（優化版本）
        /// </summary>
        public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            try
            {
                _logger.LogDebug("執行 CountAsync 查詢");

                if (predicate == null)
                {
                    return await _dbSet.CountAsync();
                }

                return await _dbSet.CountAsync(predicate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CountAsync 執行時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 存在性查詢（優化版本）
        /// </summary>
        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                _logger.LogDebug("執行 ExistsAsync 查詢");
                return await _dbSet.AnyAsync(predicate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExistsAsync 執行時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 新增實體（優化版本）
        /// </summary>
        public virtual async Task<T> AddAsync(T entity)
        {
            try
            {
                _logger.LogDebug("執行 AddAsync 操作");
                var result = await _dbSet.AddAsync(entity);
                return result.Entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddAsync 執行時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 批量新增實體（優化版本）
        /// </summary>
        public virtual async Task AddRangeAsync(IEnumerable<T> entities)
        {
            try
            {
                _logger.LogDebug("執行 AddRangeAsync 操作，數量: {Count}", entities.Count());
                await _dbSet.AddRangeAsync(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddRangeAsync 執行時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 更新實體（優化版本）
        /// </summary>
        public virtual void Update(T entity)
        {
            try
            {
                _logger.LogDebug("執行 Update 操作");
                _dbSet.Update(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update 執行時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 批量更新實體（優化版本）
        /// </summary>
        public virtual void UpdateRange(IEnumerable<T> entities)
        {
            try
            {
                _logger.LogDebug("執行 UpdateRange 操作，數量: {Count}", entities.Count());
                _dbSet.UpdateRange(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateRange 執行時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 刪除實體（優化版本）
        /// </summary>
        public virtual void Remove(T entity)
        {
            try
            {
                _logger.LogDebug("執行 Remove 操作");
                _dbSet.Remove(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Remove 執行時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 批量刪除實體（優化版本）
        /// </summary>
        public virtual void RemoveRange(IEnumerable<T> entities)
        {
            try
            {
                _logger.LogDebug("執行 RemoveRange 操作，數量: {Count}", entities.Count());
                _dbSet.RemoveRange(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RemoveRange 執行時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 執行原生 SQL 查詢（優化版本）
        /// </summary>
        public virtual async Task<IEnumerable<T>> ExecuteRawQueryAsync(string sql, params object[] parameters)
        {
            try
            {
                _logger.LogDebug("執行原生 SQL 查詢: {Sql}", sql);
                return await _dbSet.FromSqlRaw(sql, parameters).AsNoTracking().ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExecuteRawQueryAsync 執行時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 執行原生 SQL 命令（優化版本）
        /// </summary>
        public virtual async Task<int> ExecuteRawCommandAsync(string sql, params object[] parameters)
        {
            try
            {
                _logger.LogDebug("執行原生 SQL 命令: {Sql}", sql);
                return await _context.Database.ExecuteSqlRawAsync(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExecuteRawCommandAsync 執行時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 取得查詢建構器（優化版本）
        /// </summary>
        public virtual IQueryable<T> GetQueryable()
        {
            return _dbSet.AsQueryable();
        }

        /// <summary>
        /// 取得只讀查詢建構器（優化版本）
        /// </summary>
        public virtual IQueryable<T> GetReadOnlyQueryable()
        {
            return _dbSet.AsNoTracking().AsQueryable();
        }

        /// <summary>
        /// 更新實體（介面要求）
        /// </summary>
        public virtual async Task<T> UpdateAsync(T entity)
        {
            try
            {
                _logger.LogDebug("執行 UpdateAsync 操作");
                _dbSet.Update(entity);
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateAsync 執行時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 刪除實體（軟刪除）
        /// </summary>
        public virtual async Task<bool> DeleteAsync(int id)
        {
            try
            {
                _logger.LogDebug("執行 DeleteAsync 操作，ID: {Id}", id);
                var entity = await _dbSet.FindAsync(id);
                if (entity == null)
                    return false;

                entity.IsDeleted = true;
                entity.UpdatedAt = DateTime.UtcNow;
                _dbSet.Update(entity);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteAsync 執行時發生錯誤，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 永久刪除實體
        /// </summary>
        public virtual async Task<bool> HardDeleteAsync(int id)
        {
            try
            {
                _logger.LogDebug("執行 HardDeleteAsync 操作，ID: {Id}", id);
                var entity = await _dbSet.FindAsync(id);
                if (entity == null)
                    return false;

                _dbSet.Remove(entity);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HardDeleteAsync 執行時發生錯誤，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 檢查實體是否存在
        /// </summary>
        public virtual async Task<bool> ExistsAsync(int id)
        {
            try
            {
                _logger.LogDebug("執行 ExistsAsync 查詢，ID: {Id}", id);
                return await _dbSet.AnyAsync(e => e.Id == id && !e.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExistsAsync 執行時發生錯誤，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 取得第一個符合條件的實體
        /// </summary>
        public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                _logger.LogDebug("執行 FirstOrDefaultAsync 查詢");
                return await _dbSet.Where(predicate).AsNoTracking().FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FirstOrDefaultAsync 執行時發生錯誤");
                throw;
            }
        }
    }
}