using CioSystem.Models;

namespace CioSystem.API.Services
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
    }
}