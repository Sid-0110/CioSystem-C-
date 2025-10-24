using CioSystem.Models;

namespace CioSystem.API.Services
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
    }
}