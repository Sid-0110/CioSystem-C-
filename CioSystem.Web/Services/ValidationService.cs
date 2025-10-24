using CioSystem.Models;
using CioSystem.Services;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using ServicesValidationResult = CioSystem.Services.ValidationResult;

namespace CioSystem.Web.Services
{
    /// <summary>
    /// 統一驗證服務
    /// 提供各種數據驗證邏輯
    /// </summary>
    public interface IValidationService
    {
        ServicesValidationResult ValidateProduct(Product product);
        ServicesValidationResult ValidateInventory(Inventory inventory);
        ServicesValidationResult ValidateSale(Sale sale);
        ServicesValidationResult ValidatePurchase(Purchase purchase);
        ServicesValidationResult ValidateEmail(string email);
        ServicesValidationResult ValidatePhone(string phone);
        ServicesValidationResult ValidateSKU(string sku);
        ServicesValidationResult ValidatePrice(decimal price);
        ServicesValidationResult ValidateQuantity(int quantity);
    }

    public class ValidationService : IValidationService
    {
        private readonly ILogger<ValidationService> _logger;

        public ValidationService(ILogger<ValidationService> logger)
        {
            _logger = logger;
        }

        public ServicesValidationResult ValidateProduct(Product product)
        {
            var errors = new List<string>();

            if (product == null)
            {
                errors.Add("產品資料不能為空");
                return new ServicesValidationResult { IsValid = false, Errors = errors };
            }

            // 產品名稱驗證
            if (string.IsNullOrWhiteSpace(product.Name))
                errors.Add("產品名稱不能為空");
            else if (product.Name.Length > 100)
                errors.Add("產品名稱不能超過100個字符");
            else if (product.Name.Length < 2)
                errors.Add("產品名稱至少需要2個字符");

            // SKU 驗證
            var skuValidation = ValidateSKU(product.SKU);
            if (!skuValidation.IsValid)
                errors.AddRange(skuValidation.Errors);

            // 價格驗證
            var priceValidation = ValidatePrice(product.Price);
            if (!priceValidation.IsValid)
                errors.AddRange(priceValidation.Errors);

            // 成本價驗證
            if (product.CostPrice < 0)
                errors.Add("成本價不能為負數");
            else if (product.CostPrice > 1000000)
                errors.Add("成本價不能超過1,000,000");

            // 庫存水平驗證
            if (product.MinStockLevel < 0)
                errors.Add("最低庫存水平不能為負數");
            if (product.MaxStockLevel < 0)
                errors.Add("最高庫存水平不能為負數");
            if (product.MinStockLevel > product.MaxStockLevel)
                errors.Add("最低庫存水平不能大於最高庫存水平");

            // 分類驗證
            if (string.IsNullOrWhiteSpace(product.Category))
                errors.Add("產品分類不能為空");
            else if (product.Category.Length > 50)
                errors.Add("產品分類不能超過50個字符");

            // 品牌驗證
            if (!string.IsNullOrWhiteSpace(product.Brand) && product.Brand.Length > 50)
                errors.Add("品牌名稱不能超過50個字符");

            // 描述驗證
            if (!string.IsNullOrWhiteSpace(product.Description) && product.Description.Length > 500)
                errors.Add("產品描述不能超過500個字符");

            return new ServicesValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        public ServicesValidationResult ValidateInventory(Inventory inventory)
        {
            var errors = new List<string>();

            if (inventory == null)
            {
                errors.Add("庫存資料不能為空");
                return new ServicesValidationResult { IsValid = false, Errors = errors };
            }

            // 產品ID驗證
            if (inventory.ProductId <= 0)
                errors.Add("產品ID必須大於0");

            // 數量驗證
            var quantityValidation = ValidateQuantity(inventory.Quantity);
            if (!quantityValidation.IsValid)
                errors.AddRange(quantityValidation.Errors);

            // 安全庫存驗證
            if (inventory.SafetyStock < 0)
                errors.Add("安全庫存不能為負數");

            // 預留數量驗證
            if (inventory.ReservedQuantity < 0)
                errors.Add("預留數量不能為負數");

            // 生產日期驗證
            if (inventory.ProductionDate.HasValue && inventory.ProductionDate.Value > DateTime.Now)
                errors.Add("生產日期不能是未來日期");

            // 最後盤點日期驗證
            if (inventory.LastCountDate.HasValue && inventory.LastCountDate.Value > DateTime.Now)
                errors.Add("最後盤點日期不能是未來日期");

            return new ServicesValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        public ServicesValidationResult ValidateSale(Sale sale)
        {
            var errors = new List<string>();

            if (sale == null)
            {
                errors.Add("銷售資料不能為空");
                return new ServicesValidationResult { IsValid = false, Errors = errors };
            }

            // 產品ID驗證
            if (sale.ProductId <= 0)
                errors.Add("產品ID必須大於0");

            // 數量驗證
            var quantityValidation = ValidateQuantity(sale.Quantity);
            if (!quantityValidation.IsValid)
                errors.AddRange(quantityValidation.Errors);

            // 單價驗證
            var priceValidation = ValidatePrice(sale.UnitPrice);
            if (!priceValidation.IsValid)
                errors.AddRange(priceValidation.Errors);

            // 客戶名稱驗證
            if (string.IsNullOrWhiteSpace(sale.CustomerName))
                errors.Add("客戶名稱不能為空");
            else if (sale.CustomerName.Length > 100)
                errors.Add("客戶名稱不能超過100個字符");

            return new ServicesValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        public ServicesValidationResult ValidatePurchase(Purchase purchase)
        {
            var errors = new List<string>();

            if (purchase == null)
            {
                errors.Add("進貨資料不能為空");
                return new ServicesValidationResult { IsValid = false, Errors = errors };
            }

            // 產品ID驗證
            if (purchase.ProductId <= 0)
                errors.Add("產品ID必須大於0");

            // 數量驗證
            var quantityValidation = ValidateQuantity(purchase.Quantity);
            if (!quantityValidation.IsValid)
                errors.AddRange(quantityValidation.Errors);

            // 單價驗證
            var priceValidation = ValidatePrice(purchase.UnitPrice);
            if (!priceValidation.IsValid)
                errors.AddRange(priceValidation.Errors);

            // 供應商驗證
            if (string.IsNullOrWhiteSpace(purchase.Supplier))
                errors.Add("供應商不能為空");
            else if (purchase.Supplier.Length > 100)
                errors.Add("供應商名稱不能超過100個字符");

            return new ServicesValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        public ServicesValidationResult ValidateEmail(string email)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(email))
            {
                errors.Add("電子郵件不能為空");
                return new ServicesValidationResult { IsValid = false, Errors = errors };
            }

            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!emailRegex.IsMatch(email))
                errors.Add("電子郵件格式無效");

            if (email.Length > 254)
                errors.Add("電子郵件長度不能超過254個字符");

            return new ServicesValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        public ServicesValidationResult ValidatePhone(string phone)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(phone))
            {
                errors.Add("電話號碼不能為空");
                return new ServicesValidationResult { IsValid = false, Errors = errors };
            }

            // 台灣手機號碼格式驗證
            var phoneRegex = new Regex(@"^09\d{8}$");
            if (!phoneRegex.IsMatch(phone))
                errors.Add("電話號碼格式無效，請輸入正確的手機號碼");

            return new ServicesValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        public ServicesValidationResult ValidateSKU(string sku)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(sku))
            {
                errors.Add("產品編號不能為空");
                return new ServicesValidationResult { IsValid = false, Errors = errors };
            }

            if (sku.Length < 3)
                errors.Add("產品編號至少需要3個字符");
            else if (sku.Length > 50)
                errors.Add("產品編號不能超過50個字符");

            // SKU 只能包含字母、數字、連字符和底線
            var skuRegex = new Regex(@"^[A-Za-z0-9-_]+$");
            if (!skuRegex.IsMatch(sku))
                errors.Add("產品編號只能包含字母、數字、連字符和底線");

            return new ServicesValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        public ServicesValidationResult ValidatePrice(decimal price)
        {
            var errors = new List<string>();

            if (price < 0)
                errors.Add("價格不能為負數");
            else if (price > 1000000)
                errors.Add("價格不能超過1,000,000");
            else if (price == 0)
                errors.Add("價格不能為零");

            return new ServicesValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        public ServicesValidationResult ValidateQuantity(int quantity)
        {
            var errors = new List<string>();

            if (quantity <= 0)
                errors.Add("數量必須大於0");
            else if (quantity > 100000)
                errors.Add("數量不能超過100,000");

            return new ServicesValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }
    }

}