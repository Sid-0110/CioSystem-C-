using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CioSystem.Core
{
    // <summary>
    /// 泛型儲存庫介面 - 定義基本的 CRUD 操作
    /// 這是學習 Repository 模式的核心介面
    /// </summary>
    /// <typeparam name="T">實體類型，必須繼承自 BaseEntity</typeparam>
    public interface IRepository<T> where T : BaseEntity
	{
        /// <summary>
        /// 根據 ID 取得實體
        /// </summary>
        /// <param name="id">實體 ID</param>
        /// <returns>實體物件，如果不存在則返回 null</returns>
        Task<T?> GetByIdAsync(int id);

        /// <summary>
        /// 取得所有實體
        /// </summary>
        /// <returns>實體列表</returns>
        Task<IEnumerable<T>> GetAllAsync();

        /// <summary>
        /// 根據條件查詢實體
        /// </summary>
        /// <param name="predicate">查詢條件</param>
        /// <returns>符合條件的實體列表</returns>
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// 新增實體
        /// </summary>
        /// <param name="entity">要新增的實體</param>
        /// <returns>新增後的實體</returns>
        Task<T> AddAsync(T entity);

        /// <summary>
        /// 更新實體
        /// </summary>
        /// <param name="entity">要更新的實體</param>
        /// <returns>更新後的實體</returns>
        Task<T> UpdateAsync(T entity);

        /// <summary>
        /// 刪除實體（軟刪除）
        /// </summary>
        /// <param name="id">要刪除的實體 ID</param>
        /// <returns>是否刪除成功</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// 永久刪除實體
        /// </summary>
        /// <param name="id">要刪除的實體 ID</param>
        /// <returns>是否刪除成功</returns>
        Task<bool> HardDeleteAsync(int id);

        /// <summary>
        /// 檢查實體是否存在
        /// </summary>
        /// <param name="id">實體 ID</param>
        /// <returns>是否存在</returns>
        Task<bool> ExistsAsync(int id);

        /// <summary>
        /// 計算符合條件的實體數量
        /// </summary>
        /// <param name="predicate">查詢條件</param>
        /// <returns>實體數量</returns>
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);
    }
}

