using CioSystem.Core;
using CioSystem.Data;
using CioSystem.Models;
using CioSystem.Services;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace CioSystem.Services
{
    /// <summary>
    /// 產品服務實現
    /// </summary>
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProductService> _logger;

        public ProductService(IUnitOfWork unitOfWork, ILogger<ProductService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// 取得所有產品
        /// </summary>
        /// <returns>產品列表</returns>
        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            try
            {
                Expression<Func<Product, bool>> predicate = p => !p.IsDeleted;
                // ✅ 優化：使用 AsNoTracking 提升只讀查詢效能
                var products = await _unitOfWork.GetRepository<Product>().FindAsync(predicate);
                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得所有產品時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 根據ID取得產品
        /// </summary>
        /// <param name="id">產品ID</param>
        /// <returns>產品</returns>
        public async Task<Product?> GetProductByIdAsync(int id)
        {
            try
            {
                Expression<Func<Product, bool>> predicate = p => p.Id == id && !p.IsDeleted;
                var products = await _unitOfWork.GetRepository<Product>().FindAsync(predicate);
                return products.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據ID取得產品時發生錯誤: Id={Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 根據SKU取得產品
        /// </summary>
        /// <param name="sku">產品SKU</param>
        /// <returns>產品</returns>
        public async Task<Product?> GetProductBySkuAsync(string sku)
        {
            try
            {
                Expression<Func<Product, bool>> predicate = p => p.SKU == sku && !p.IsDeleted;
                var products = await _unitOfWork.GetRepository<Product>().FindAsync(predicate);
                return products.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據SKU取得產品時發生錯誤: SKU={SKU}", sku);
                throw;
            }
        }

        /// <summary>
        /// 創建產品
        /// </summary>
        /// <param name="product">產品</param>
        /// <returns>創建的產品</returns>
        public async Task<Product> CreateProductAsync(Product product)
        {
            try
            {
                product.CreatedAt = DateTime.Now;
                product.UpdatedAt = DateTime.Now;
                product.CreatedBy = "System";
                product.UpdatedBy = "System";

                await _unitOfWork.GetRepository<Product>().AddAsync(product);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("成功創建產品: Id={Id}, Name={Name}, SKU={SKU}",
                    product.Id, product.Name, product.SKU);

                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "創建產品時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 更新產品
        /// </summary>
        /// <param name="product">產品</param>
        /// <returns>是否成功</returns>
        public async Task<bool> UpdateProductAsync(Product product)
        {
            try
            {
                product.UpdatedAt = DateTime.Now;
                product.UpdatedBy = "System";

                await _unitOfWork.GetRepository<Product>().UpdateAsync(product);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("成功更新產品: Id={Id}, Name={Name}, SKU={SKU}",
                    product.Id, product.Name, product.SKU);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新產品時發生錯誤: Id={Id}", product.Id);
                return false;
            }
        }

        /// <summary>
        /// 刪除產品
        /// </summary>
        /// <param name="id">產品ID</param>
        /// <returns>是否成功</returns>
        public async Task<bool> DeleteProductAsync(int id)
        {
            try
            {
                var product = await GetProductByIdAsync(id);
                if (product == null)
                {
                    return false;
                }

                product.IsDeleted = true;
                product.UpdatedAt = DateTime.Now;
                product.UpdatedBy = "System";

                await _unitOfWork.GetRepository<Product>().UpdateAsync(product);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("成功刪除產品: Id={Id}, Name={Name}, SKU={SKU}",
                    id, product.Name, product.SKU);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除產品時發生錯誤: Id={Id}", id);
                return false;
            }
        }

        /// <summary>
        /// 檢查產品是否存在
        /// </summary>
        /// <param name="id">產品ID</param>
        /// <returns>是否存在</returns>
        public async Task<bool> ProductExistsAsync(int id)
        {
            try
            {
                Expression<Func<Product, bool>> predicate = p => p.Id == id && !p.IsDeleted;
                var repository = _unitOfWork.GetRepository<Product>();
                return await repository.CountAsync(predicate) > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查產品是否存在時發生錯誤: Id={Id}", id);
                return false;
            }
        }

        /// <summary>
        /// 檢查SKU是否存在
        /// </summary>
        /// <param name="sku">產品SKU</param>
        /// <param name="excludeId">排除的產品ID</param>
        /// <returns>是否存在</returns>
        public async Task<bool> SkuExistsAsync(string sku, int? excludeId = null)
        {
            try
            {
                Expression<Func<Product, bool>> predicate = p => p.SKU == sku && !p.IsDeleted;

                if (excludeId.HasValue)
                {
                    predicate = p => p.SKU == sku && !p.IsDeleted && p.Id != excludeId.Value;
                }

                var repository = _unitOfWork.GetRepository<Product>();
                return await repository.CountAsync(predicate) > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查SKU是否存在時發生錯誤: SKU={SKU}", sku);
                return false;
            }
        }

        /// <summary>
        /// 更新產品狀態
        /// </summary>
        /// <param name="id">產品ID</param>
        /// <param name="status">新狀態</param>
        /// <returns>是否成功</returns>
        public async Task<bool> UpdateProductStatusAsync(int id, ProductStatus status)
        {
            try
            {
                var product = await GetProductByIdAsync(id);
                if (product == null)
                {
                    return false;
                }

                product.Status = status;
                product.UpdatedAt = DateTime.Now;
                product.UpdatedBy = "System";

                await _unitOfWork.GetRepository<Product>().UpdateAsync(product);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("成功更新產品狀態: Id={Id}, Status={Status}", id, status);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新產品狀態時發生錯誤: Id={Id}, Status={Status}", id, status);
                return false;
            }
        }

        /// <summary>
        /// 取得產品統計資訊
        /// </summary>
        /// <returns>產品統計資訊</returns>
        public async Task<ProductStatistics> GetProductStatisticsAsync()
        {
            try
            {
                var repository = _unitOfWork.GetRepository<Product>();
                var allProducts = await repository.FindAsync(p => !p.IsDeleted);

                var stats = new ProductStatistics
                {
                    TotalProducts = allProducts.Count(),
                    ActiveProducts = allProducts.Count(p => p.Status == ProductStatus.Active),
                    InactiveProducts = allProducts.Count(p => p.Status == ProductStatus.Inactive),
                    TotalCategories = allProducts.Select(p => p.Category).Distinct().Count(),
                    AveragePrice = allProducts.Any() ? allProducts.Average(p => p.Price) : 0,
                    TotalValue = allProducts.Sum(p => p.Price)
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得產品統計資訊時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 根據分類取得產品
        /// </summary>
        /// <param name="category">產品分類</param>
        /// <returns>產品列表</returns>
        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category)
        {
            try
            {
                Expression<Func<Product, bool>> predicate = p => !p.IsDeleted && p.Category == category;
                var products = await _unitOfWork.GetRepository<Product>().FindAsync(predicate);
                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據分類取得產品時發生錯誤: Category={Category}", category);
                throw;
            }
        }

        /// <summary>
        /// 搜尋產品
        /// </summary>
        /// <param name="searchTerm">搜尋關鍵字</param>
        /// <returns>產品列表</returns>
        public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
        {
            try
            {
                Expression<Func<Product, bool>> predicate = p => !p.IsDeleted &&
                    (p.Name.Contains(searchTerm) || p.SKU.Contains(searchTerm) || p.Description.Contains(searchTerm));
                var products = await _unitOfWork.GetRepository<Product>().FindAsync(predicate);
                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜尋產品時發生錯誤: SearchTerm={SearchTerm}", searchTerm);
                throw;
            }
        }

        /// <summary>
        /// 取得分頁產品
        /// </summary>
        /// <param name="pageNumber">頁碼</param>
        /// <param name="pageSize">每頁大小</param>
        /// <param name="category">產品分類（可選）</param>
        /// <param name="status">產品狀態（可選）</param>
        /// <returns>分頁產品和總數量</returns>
        public async Task<(IEnumerable<Product> Products, int TotalCount)> GetProductsPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? category = null,
            ProductStatus? status = null)
        {
            try
            {
                var hasSearch = !string.IsNullOrWhiteSpace(searchTerm);
                var term = hasSearch ? searchTerm!.Trim() : string.Empty;
                var hasCategory = !string.IsNullOrEmpty(category);
                var hasStatus = status.HasValue;

                Expression<Func<Product, bool>> predicate = p =>
                    !p.IsDeleted
                    && (!hasSearch || (
                        (p.Name != null && p.Name.Contains(term)) ||
                        (p.SKU != null && p.SKU.Contains(term)) ||
                        (p.Description != null && p.Description.Contains(term))
                    ))
                    && (!hasCategory || p.Category == category)
                    && (!hasStatus || p.Status == status!.Value);

                var repository = _unitOfWork.GetRepository<Product>();
                var totalCount = await repository.CountAsync(predicate);

                var result = await repository.GetPagedAsync(pageNumber, pageSize, predicate, p => p.Id);
                var products = result.Items;

                return (products, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得分頁產品時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 驗證產品資料
        /// </summary>
        /// <param name="product">產品</param>
        /// <returns>驗證結果</returns>
        public async Task<ValidationResult> ValidateProductAsync(Product product)
        {
            var errors = new List<string>();

            if (product == null)
            {
                errors.Add("產品不能為空");
                return new ValidationResult { IsValid = false, Errors = errors };
            }

            if (string.IsNullOrWhiteSpace(product.Name))
            {
                errors.Add("產品名稱不能為空");
            }

            if (string.IsNullOrWhiteSpace(product.SKU))
            {
                errors.Add("產品SKU不能為空");
            }

            if (product.Price < 0)
            {
                errors.Add("產品價格不能為負數");
            }

            if (product.CostPrice < 0)
            {
                errors.Add("產品成本不能為負數");
            }

            // 檢查SKU是否重複
            if (!string.IsNullOrWhiteSpace(product.SKU))
            {
                var skuExists = await SkuExistsAsync(product.SKU, product.Id);
                if (skuExists)
                {
                    errors.Add("產品SKU已存在");
                }
            }

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }
    }
}