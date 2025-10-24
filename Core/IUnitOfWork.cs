using System;
using System.Threading.Tasks;

namespace CioSystem.Core
{
    /// <summary>
    /// 工作單元介面 - 管理資料庫交易和儲存庫
    /// 這是學習 Unit of Work 模式的核心介面
    /// </summary>
    public interface IUnitOfWork : IDisposable
	{
        /// <summary>
        /// 取得泛型儲存庫
        /// </summary>
        IRepository<T> GetRepository<T>() where T : BaseEntity;

        /// <summary>
        /// 儲存所有變更
        /// </summary>
        /// <returns>受影響的記錄數</returns   
        Task<int> SaveChangesAsync();

        /// <summary>
        /// 開始資料庫交易
        /// </summary>
        Task BeginTransactionAsync();

        /// <summary>
        /// 提交資料庫交易
        /// </summary>
        Task CommitTransactionAsync();

        /// <summary>
        /// 回滾資料庫交易
        /// </summary>
        Task RollbackTransactionAsync();

        /// <summary>
        /// 執行資料庫交易
        /// </summary>
        /// <param name="action">要執行的工作</param>
        /// <returns>工作結果</returns>
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action);
    }

    // 注意：Product 和 Inventory 實體在 CioSystem.Models 專案中定義
    // 這裡只是介面定義，實際實體需要引用 Models 專案
}

