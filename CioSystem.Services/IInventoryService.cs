using CioSystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CioSystem.Services
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
        /// 根據產品 ID 取得庫存項目
        /// </summary>
        /// <param name="productId">產品 ID</param>
        /// <returns>庫存項目</returns>
        Task<Inventory?> GetInventoryByProductIdAsync(int productId);

        /// <summary>
        /// 創建庫存項目
        /// </summary>
        /// <param name="inventory">庫存項目</param>
        /// <returns>創建的庫存項目</returns>
        Task<Inventory> CreateInventoryAsync(Inventory inventory);

        /// <summary>
        /// 更新庫存項目
        /// </summary>
        /// <param name="id">庫存 ID</param>
        /// <param name="inventory">庫存項目</param>
        /// <returns>更新的庫存項目</returns>
        Task<Inventory> UpdateInventoryAsync(int id, Inventory inventory);

        /// <summary>
        /// 更新庫存數量
        /// </summary>
        /// <param name="productId">產品 ID</param>
        /// <param name="quantityAdjustment">數量調整值 (正數為增加，負數為減少)</param>
        /// <returns>是否成功</returns>
        Task<bool> UpdateInventoryQuantityAsync(int productId, int quantityAdjustment);

        /// <summary>
        /// 刪除庫存項目
        /// </summary>
        /// <param name="id">庫存 ID</param>
        /// <returns>是否成功</returns>
        Task<bool> DeleteInventoryAsync(int id);

        /// <summary>
        /// 取得分頁庫存項目
        /// </summary>
        /// <param name="pageNumber">頁碼</param>
        /// <param name="pageSize">每頁大小</param>
        /// <param name="productId">產品ID（可選）</param>
        /// <param name="productSKU">產品編號（可選）</param>
        /// <param name="status">庫存狀態（可選）</param>
        /// <returns>分頁庫存項目和總數</returns>
        Task<(IEnumerable<Inventory> Inventory, int TotalCount)> GetInventoryPagedAsync(int pageNumber, int pageSize, int? productId = null, string? productSKU = null, InventoryStatus? status = null);

        /// <summary>
        /// 取得庫存統計資訊
        /// </summary>
        /// <returns>庫存統計資訊</returns>
        Task<InventoryStatistics> GetInventoryStatisticsAsync();

        /// <summary>
        /// 取得庫存移動記錄
        /// </summary>
        /// <param name="inventoryId">庫存ID（可選）</param>
        /// <param name="startDate">開始日期（可選）</param>
        /// <param name="endDate">結束日期（可選）</param>
        /// <returns>庫存移動記錄列表</returns>
        Task<IEnumerable<InventoryMovement>> GetInventoryMovementsAsync(int? inventoryId = null, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// 依據進貨與銷售紀錄對帳庫存（每個產品的 進貨總量 - 銷售總量 與當前庫存差異）
        /// </summary>
        /// <returns>對帳結果列表</returns>
        Task<IEnumerable<CioSystem.Services.DTOs.ConsistencyReportItemDto>> GetInventoryConsistencyReportAsync();
    }
}