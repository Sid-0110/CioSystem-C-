using CioSystem.Models;
using CioSystem.Services.Cache; 

namespace CioSystem.Services
{
    /// <summary>
    /// 銷售服務接口
    /// </summary>
    public interface ISalesService
    {
        /// <summary>
        /// 取得所有銷售記錄
        /// </summary>
        /// <returns>銷售記錄列表</returns>
        Task<IEnumerable<Sale>> GetAllSalesAsync();

        /// <summary>
        /// 取得分頁銷售記錄
        /// </summary>
        /// <param name="page">頁碼</param>
        /// <param name="pageSize">每頁大小</param>
        /// <param name="productId">產品ID篩選</param>
        /// <param name="customerName">客戶名稱篩選</param>
        /// <returns>分頁銷售記錄</returns>
        Task<(IEnumerable<Sale> Sales, int TotalCount)> GetSalesPagedAsync(int page, int pageSize, int? productId = null, string? customerName = null);

        /// <summary>
        /// 根據ID取得銷售記錄
        /// </summary>
        /// <param name="id">銷售記錄ID</param>
        /// <returns>銷售記錄</returns>
        Task<Sale?> GetSaleByIdAsync(int id);

        /// <summary>
        /// 創建銷售記錄
        /// </summary>
        /// <param name="sale">銷售記錄</param>
        /// <returns>創建結果</returns>
        Task<ValidationResult> CreateSaleAsync(Sale sale);

        /// <summary>
        /// 更新銷售記錄
        /// </summary>
        /// <param name="sale">銷售記錄</param>
        /// <returns>更新結果</returns>
        Task<ValidationResult> UpdateSaleAsync(Sale sale);

        /// <summary>
        /// 刪除銷售記錄（軟刪除）
        /// </summary>
        /// <param name="id">銷售記錄ID</param>
        /// <returns>刪除結果</returns>
        Task<bool> DeleteSaleAsync(int id);

        /// <summary>
        /// 檢查銷售記錄是否存在
        /// </summary>
        /// <param name="id">銷售記錄ID</param>
        /// <returns>是否存在</returns>
        Task<bool> SaleExistsAsync(int id);

        /// <summary>
        /// 取得銷售統計資料
        /// </summary>
        /// <returns>銷售統計資料</returns>
        Task<SalesStatistics> GetSalesStatisticsAsync();
    }
}