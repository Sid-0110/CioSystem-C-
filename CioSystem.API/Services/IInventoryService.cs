using CioSystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CioSystem.API.Services
{
    /// <summary>
    /// 庫存服務接口
    /// </summary>
    public interface IInventoryService
    {
        /// <summary>
        /// 取得所有庫存項目
        /// </summary>
        /// <returns>庫存項目列表</returns>
        Task<IEnumerable<Inventory>> GetAllInventoryAsync();

        /// <summary>
        /// 根據 ID 取得庫存項目
        /// </summary>
        /// <param name="id">庫存 ID</param>
        /// <returns>庫存項目</returns>
        Task<Inventory?> GetInventoryByIdAsync(int id);

        /// <summary>
        /// 創建庫存項目
        /// </summary>
        /// <param name="inventory">庫存項目</param>
        /// <returns>創建的庫存項目</returns>
        Task<Inventory> CreateInventoryAsync(Inventory inventory);

        /// <summary>
        /// 更新庫存項目
        /// </summary>
        /// <param name="inventory">庫存項目</param>
        /// <returns>是否成功</returns>
        Task<Inventory> UpdateInventoryAsync(int id, Inventory inventory);

        /// <summary>
        /// 刪除庫存項目
        /// </summary>
        /// <param name="id">庫存 ID</param>
        /// <returns>是否成功</returns>
        Task<bool> DeleteInventoryAsync(int id);

        /// <summary>
        /// 調整庫存數量
        /// </summary>
        /// <param name="inventoryId">庫存 ID</param>
        /// <param name="quantityAdjustment">數量調整值 (正數為增加，負數為減少)</param>
        /// <returns>是否成功</returns>
        Task<bool> AdjustInventoryQuantityAsync(int inventoryId, int quantityAdjustment, string reason);

        /// <summary>
        /// 取得庫存移動記錄
        /// </summary>
        /// <param name="inventoryId">庫存 ID</param>
        /// <returns>庫存移動記錄列表</returns>
        Task<IEnumerable<InventoryMovement>> GetInventoryMovementsAsync(int inventoryId);

        /// <summary>
        /// 取得總庫存數量
        /// </summary>
        /// <returns>總庫存數量</returns>
        Task<int> GetTotalStockQuantityAsync();

        /// <summary>
        /// 取得總庫存價值
        /// </summary>
        /// <returns>總庫存價值</returns>
        Task<decimal> GetTotalStockValueAsync();

        /// <summary>
        /// 取得庫存統計資訊
        /// </summary>
        /// <returns>庫存統計資訊</returns>
        Task<InventoryStatistics> GetInventoryStatisticsAsync();
    }
}