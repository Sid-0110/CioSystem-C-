using Microsoft.EntityFrameworkCore.Storage;
using CioSystem.Core;
using CioSystem.Models;
using CioSystem.Data.Repositories;

namespace CioSystem.Data
{
    /// <summary>
    /// 工作單元實作
    /// 這是學習 Unit of Work 模式的具體實作類別
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly CioSystemDbContext _context;
        private IDbContextTransaction? _transaction;
        private bool _disposed = false;

        // 儲存庫快取
        private IRepository<Product>? _products;
        private IRepository<Inventory>? _inventory;

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="context">資料庫上下文</param>
        public UnitOfWork(CioSystemDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 取得泛型儲存庫
        /// </summary>
        /// <typeparam name="T">實體類型</typeparam>
        /// <returns>儲存庫實例</returns>
        public IRepository<T> GetRepository<T>() where T : BaseEntity
        {
            return new Repository<T>(_context);
        }

        /// <summary>
        /// 取得產品儲存庫
        /// </summary>
        public IRepository<Product> Products
        {
            get
            {
                _products ??= new Repository<Product>(_context);
                return _products;
            }
        }

        /// <summary>
        /// 取得庫存儲存庫
        /// </summary>
        public IRepository<Inventory> Inventory
        {
            get
            {
                _inventory ??= new Repository<Inventory>(_context);
                return _inventory;
            }
        }

        /// <summary>
        /// 儲存所有變更
        /// </summary>
        /// <returns>受影響的記錄數</returns>
        public async Task<int> SaveChangesAsync()
        {
            try
            {
                return await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // 記錄錯誤並重新拋出
                throw new InvalidOperationException("儲存變更時發生錯誤", ex);
            }
        }

        /// <summary>
        /// 開始資料庫交易
        /// </summary>
        public async Task BeginTransactionAsync()
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("交易已經開始");
            }

            _transaction = await _context.Database.BeginTransactionAsync();
        }

        /// <summary>
        /// 提交資料庫交易
        /// </summary>
        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("沒有活躍的交易");
            }

            try
            {
                await _transaction.CommitAsync();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        /// <summary>
        /// 回滾資料庫交易
        /// </summary>
        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("沒有活躍的交易");
            }

            try
            {
                await _transaction.RollbackAsync();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        /// <summary>
        /// 執行資料庫交易
        /// </summary>
        /// <typeparam name="T">返回類型</typeparam>
        /// <param name="action">要執行的工作</param>
        /// <returns>工作結果</returns>
        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action)
        {
            // 如果已經在交易中，直接執行
            if (_transaction != null)
            {
                return await action();
            }

            // 開始新交易
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var result = await action();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// 執行資料庫交易（無返回值）
        /// </summary>
        /// <param name="action">要執行的工作</param>
        public async Task ExecuteInTransactionAsync(Func<Task> action)
        {
            // 如果已經在交易中，直接執行
            if (_transaction != null)
            {
                await action();
                return;
            }

            // 開始新交易
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await action();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// 檢查是否有未儲存的變更
        /// </summary>
        /// <returns>是否有未儲存的變更</returns>
        public bool HasUnsavedChanges()
        {
            return _context.ChangeTracker.HasChanges();
        }

        /// <summary>
        /// 取得變更追蹤器
        /// </summary>
        /// <returns>變更追蹤器</returns>
        public Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker GetChangeTracker()
        {
            return _context.ChangeTracker;
        }

        /// <summary>
        /// 清除所有變更追蹤
        /// </summary>
        public void ClearChangeTracker()
        {
            _context.ChangeTracker.Clear();
        }

        /// <summary>
        /// 執行批次操作
        /// </summary>
        /// <typeparam name="T">實體類型</typeparam>
        /// <param name="entities">要操作的實體列表</param>
        /// <param name="operation">操作類型</param>
        /// <returns>操作的實體數量</returns>
        public async Task<int> BatchOperationAsync<T>(
            IEnumerable<T> entities, 
            BatchOperation operation) where T : BaseEntity
        {
            if (entities == null || !entities.Any())
                return 0;

            var dbSet = _context.Set<T>();

            switch (operation)
            {
                case BatchOperation.Add:
                    await dbSet.AddRangeAsync(entities);
                    break;

                case BatchOperation.Update:
                    dbSet.UpdateRange(entities);
                    break;

                case BatchOperation.Delete:
                    // 軟刪除
                    foreach (var entity in entities)
                    {
                        entity.IsDeleted = true;
                        entity.UpdatedAt = DateTime.Now;
                    }
                    dbSet.UpdateRange(entities);
                    break;

                case BatchOperation.HardDelete:
                    dbSet.RemoveRange(entities);
                    break;

                default:
                    throw new ArgumentException($"不支援的操作類型: {operation}");
            }

            return entities.Count();
        }

        /// <summary>
        /// 釋放資源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 釋放資源
        /// </summary>
        /// <param name="disposing">是否正在釋放</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _transaction?.Dispose();
                _context?.Dispose();
                _disposed = true;
            }
        }

        /// <summary>
        /// 釋放資源（非同步版本）
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                if (_transaction != null)
                    await _transaction.DisposeAsync();

                if (_context != null)
                    await _context.DisposeAsync();

                _disposed = true;
            }

            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// 批次操作類型
    /// </summary>
    public enum BatchOperation
    {
        /// <summary>
        /// 新增
        /// </summary>
        Add,

        /// <summary>
        /// 更新
        /// </summary>
        Update,

        /// <summary>
        /// 刪除（軟刪除）
        /// </summary>
        Delete,

        /// <summary>
        /// 永久刪除
        /// </summary>
        HardDelete
    }
}