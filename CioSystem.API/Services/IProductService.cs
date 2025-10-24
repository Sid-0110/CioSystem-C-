using CioSystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CioSystem.API.Services
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task<Product?> GetProductBySkuAsync(string sku);
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category);
        Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm);
        Task<(IEnumerable<Product> Products, int TotalCount)> GetProductsPagedAsync(
            int pageNumber,
            int pageSize,
            string? category = null,
            ProductStatus? status = null);
        Task<Product> CreateProductAsync(Product product);
        Task<Product> UpdateProductAsync(int id, Product product);
        Task<bool> DeleteProductAsync(int id);
        Task<bool> HardDeleteProductAsync(int id);
        Task<bool> ProductExistsAsync(int id);
        Task<bool> SkuExistsAsync(string sku, int? excludeId = null);
        Task<bool> UpdateProductStatusAsync(int id, ProductStatus status);
        Task<IEnumerable<Product>> GetLowStockProductsAsync();
        Task<ProductStatistics> GetProductStatisticsAsync();
        Task<int> BatchUpdateProductStatusAsync(IEnumerable<int> productIds, ProductStatus status);
        Task<CioSystem.API.Services.ValidationResult> ValidateProductAsync(Product product);
    }
}