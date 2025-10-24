using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using CioSystem.Core;

namespace CioSystem.Data.Repositories
{
    /// <summary>
    /// 泛型儲存庫實作
    /// 這是學習 Repository 模式的具體實作類別
    /// </summary>
    /// <typeparam name="T">實體類型，必須繼承自 BaseEntity</typeparam>
    public class Repository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly CioSystemDbContext _context;
        protected readonly DbSet<T> _dbSet;

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="context">資料庫上下文</param>
        public Repository(CioSystemDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = context.Set<T>();
        }

        /// <summary>
        /// 根據 ID 取得實體
        /// </summary>
        /// <param name="id">實體 ID</param>
        /// <returns>實體物件，如果不存在則返回 null</returns>
        public virtual async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        /// <summary>
        /// 取得所有實體
        /// </summary>
        /// <returns>實體列表</returns>
        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        /// <summary>
        /// 根據條件查詢實體
        /// </summary>
        /// <param name="predicate">查詢條件</param>
        /// <returns>符合條件的實體列表</returns>
        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        /// <summary>
        /// 新增實體
        /// </summary>
        /// <param name="entity">要新增的實體</param>
        /// <returns>新增後的實體</returns>
        public virtual async Task<T> AddAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await _dbSet.AddAsync(entity);
            return entity;
        }

        /// <summary>
        /// 新增多個實體
        /// </summary>
        /// <param name="entities">要新增的實體列表</param>
        /// <returns>新增後的實體列表</returns>
        public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            await _dbSet.AddRangeAsync(entities);
            return entities;
        }

        /// <summary>
        /// 更新實體
        /// </summary>
        /// <param name="entity">要更新的實體</param>
        /// <returns>更新後的實體</returns>
        public virtual T Update(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _dbSet.Update(entity);
            return entity;
        }

        /// <summary>
        /// 更新實體（非同步版本）
        /// </summary>
        /// <param name="entity">要更新的實體</param>
        /// <returns>更新後的實體</returns>
        public virtual async Task<T> UpdateAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _dbSet.Update(entity);
            return await Task.FromResult(entity);
        }

        /// <summary>
        /// 刪除實體（軟刪除）
        /// </summary>
        /// <param name="id">要刪除的實體 ID</param>
        /// <returns>是否刪除成功</returns>
        public virtual async Task<bool> DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null)
                return false;

            // 軟刪除：設定 IsDeleted 為 true
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.Now;
            
            return true;
        }

        /// <summary>
        /// 永久刪除實體
        /// </summary>
        /// <param name="id">要刪除的實體 ID</param>
        /// <returns>是否刪除成功</returns>
        public virtual async Task<bool> HardDeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null)
                return false;

            _dbSet.Remove(entity);
            return true;
        }

        /// <summary>
        /// 檢查實體是否存在
        /// </summary>
        /// <param name="id">實體 ID</param>
        /// <returns>是否存在</returns>
        public virtual async Task<bool> ExistsAsync(int id)
        {
            return await _dbSet.AnyAsync(e => e.Id == id);
        }

        /// <summary>
        /// 計算符合條件的實體數量
        /// </summary>
        /// <param name="predicate">查詢條件</param>
        /// <returns>實體數量</returns>
        public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.CountAsync(predicate);
        }

        /// <summary>
        /// 計算所有實體數量
        /// </summary>
        /// <returns>實體數量</returns>
        public virtual async Task<int> CountAsync()
        {
            return await _dbSet.CountAsync();
        }

        /// <summary>
        /// 分頁查詢
        /// </summary>
        /// <param name="pageNumber">頁碼（從 1 開始）</param>
        /// <param name="pageSize">每頁大小</param>
        /// <param name="predicate">查詢條件（可選）</param>
        /// <param name="orderBy">排序條件（可選）</param>
        /// <returns>分頁結果</returns>
        public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            Expression<Func<T, bool>>? predicate = null,
            Expression<Func<T, object>>? orderBy = null)
        {
            var query = _dbSet.AsQueryable();

            // 應用查詢條件
            if (predicate != null)
                query = query.Where(predicate);

            // 計算總數
            var totalCount = await query.CountAsync();

            // 應用排序
            if (orderBy != null)
                query = query.OrderBy(orderBy);

            // 分頁
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        /// <summary>
        /// 取得第一個符合條件的實體
        /// </summary>
        /// <param name="predicate">查詢條件</param>
        /// <returns>實體物件，如果不存在則返回 null</returns>
        public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        /// <summary>
        /// 檢查是否有任何實體符合條件
        /// </summary>
        /// <param name="predicate">查詢條件</param>
        /// <returns>是否有符合條件的實體</returns>
        public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        /// <summary>
        /// 取得包含相關實體的查詢
        /// </summary>
        /// <param name="includeProperties">要包含的導航屬性</param>
        /// <returns>包含相關實體的查詢</returns>
        public virtual IQueryable<T> GetQueryableWithIncludes(params Expression<Func<T, object>>[] includeProperties)
        {
            var query = _dbSet.AsQueryable();

            foreach (var includeProperty in includeProperties)
            {
                query = query.Include(includeProperty);
            }

            return query;
        }

        /// <summary>
        /// 執行原始 SQL 查詢
        /// </summary>
        /// <param name="sql">SQL 查詢語句</param>
        /// <param name="parameters">查詢參數</param>
        /// <returns>查詢結果</returns>
        public virtual async Task<IEnumerable<T>> FromSqlRawAsync(string sql, params object[] parameters)
        {
            return await _dbSet.FromSqlRaw(sql, parameters).ToListAsync();
        }

        /// <summary>
        /// 釋放資源
        /// </summary>
        public void Dispose()
        {
            _context?.Dispose();
        }

        /// <summary>
        /// 釋放資源（非同步版本）
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_context != null)
                await _context.DisposeAsync();
        }
    }
}