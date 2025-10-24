using CioSystem.Models;
using CioSystem.Services.Cache;
using CioSystem.Services.DTOs;

namespace CioSystem.Services
{
    /// <summary>
    /// 進貨服務接口
    /// </summary>
    public interface IPurchasesService
    {
        /// <summary>
        /// 取得所有進貨記錄
        /// </summary>
        /// <returns>進貨記錄列表</returns>
        Task<IEnumerable<Purchase>> GetAllPurchasesAsync();

        /// <summary>
        /// 根據ID取得進貨記錄
        /// </summary>
        /// <param name="id">進貨記錄ID</param>
        /// <returns>進貨記錄</returns>
        Task<Purchase?> GetPurchaseByIdAsync(int id);

        /// <summary>
        /// 創建進貨記錄
        /// </summary>
        /// <param name="purchase">進貨記錄</param>
        /// <returns>創建結果</returns>
        Task<ValidationResult> CreatePurchaseAsync(Purchase purchase);

        /// <summary>
        /// 更新進貨記錄
        /// </summary>
        /// <param name="purchase">進貨記錄</param>
        /// <returns>更新結果</returns>
        Task<ValidationResult> UpdatePurchaseAsync(Purchase purchase);

        /// <summary>
        /// 刪除進貨記錄（軟刪除）
        /// </summary>
        /// <param name="id">進貨記錄ID</param>
        /// <returns>刪除結果</returns>
        Task<bool> DeletePurchaseAsync(int id);

        /// <summary>
        /// 檢查進貨記錄是否存在
        /// </summary>
        /// <param name="id">進貨記錄ID</param>
        /// <returns>是否存在</returns>
        Task<bool> PurchaseExistsAsync(int id);

        /// <summary>
        /// 取得分頁進貨記錄
        /// </summary>
        /// <param name="pageNumber">頁碼</param>
        /// <param name="pageSize">每頁大小</param>
        /// <param name="productId">產品ID篩選（可選）</param>
        /// <returns>分頁進貨記錄和總數量</returns>
        Task<(IEnumerable<Purchase> Purchases, int TotalCount)> GetPurchasesPagedAsync(int pageNumber, int pageSize, int? productId = null);

        /// <summary>
        /// 取得所有進貨記錄（帶連續序號）
        /// </summary>
        /// <returns>帶序號的進貨記錄列表</returns>
        Task<IEnumerable<PurchaseWithSequenceDto>> GetAllPurchasesWithSequenceAsync();

        /// <summary>
        /// 取得分頁進貨記錄（帶連續序號）
        /// </summary>
        /// <param name="pageNumber">頁碼</param>
        /// <param name="pageSize">每頁大小</param>
        /// <param name="productId">產品ID篩選（可選）</param>
        /// <returns>帶序號的分頁進貨記錄</returns>
        Task<IEnumerable<PurchaseWithSequenceDto>> GetPurchasesPagedWithSequenceAsync(int pageNumber, int pageSize, int? productId = null);

        /// <summary>
        /// 重新排序進貨記錄ID為連續序號
        /// </summary>
        /// <param name="includeDeleted">是否包含已刪除的記錄</param>
        /// <returns>重新排序結果</returns>
        Task<ValidationResult> ReorderPurchaseIdsAsync(bool includeDeleted = false);

        /// <summary>
        /// 取得進貨統計資料
        /// </summary>
        /// <returns>進貨統計資料</returns>
        Task<PurchasesStatistics> GetPurchasesStatisticsAsync();
    }
}