using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using CioSystem.Models;

namespace CioSystem.Services
{
    /// <summary>
    /// 驗證服務
    /// 提供統一的驗證邏輯和自定義驗證規則
    /// </summary>
    public interface IValidationService
    {
        /// <summary>
        /// 驗證模型
        /// </summary>
        /// <typeparam name="T">模型類型</typeparam>
        /// <param name="model">要驗證的模型</param>
        /// <returns>驗證結果</returns>
        ValidationResult ValidateModel<T>(T model) where T : class;

        /// <summary>
        /// 驗證產品
        /// </summary>
        /// <param name="product">產品</param>
        /// <returns>驗證結果</returns>
        ValidationResult ValidateProduct(Product product);

        /// <summary>
        /// 驗證庫存
        /// </summary>
        /// <param name="inventory">庫存</param>
        /// <returns>驗證結果</returns>
        ValidationResult ValidateInventory(Inventory inventory);

        /// <summary>
        /// 驗證採購
        /// </summary>
        /// <param name="purchase">採購</param>
        /// <returns>驗證結果</returns>
        ValidationResult ValidatePurchase(Purchase purchase);

        /// <summary>
        /// 驗證銷售
        /// </summary>
        /// <param name="sale">銷售</param>
        /// <returns>驗證結果</returns>
        ValidationResult ValidateSale(Sale sale);

        /// <summary>
        /// 驗證電子郵件格式
        /// </summary>
        /// <param name="email">電子郵件</param>
        /// <returns>是否有效</returns>
        bool IsValidEmail(string email);

        /// <summary>
        /// 驗證電話號碼格式
        /// </summary>
        /// <param name="phone">電話號碼</param>
        /// <returns>是否有效</returns>
        bool IsValidPhone(string phone);

        /// <summary>
        /// 驗證 SKU 格式
        /// </summary>
        /// <param name="sku">SKU</param>
        /// <returns>是否有效</returns>
        bool IsValidSKU(string sku);

        /// <summary>
        /// 驗證數量
        /// </summary>
        /// <param name="quantity">數量</param>
        /// <param name="minValue">最小值</param>
        /// <param name="maxValue">最大值</param>
        /// <returns>是否有效</returns>
        bool IsValidQuantity(int quantity, int minValue = 0, int maxValue = int.MaxValue);

        /// <summary>
        /// 驗證價格
        /// </summary>
        /// <param name="price">價格</param>
        /// <param name="minValue">最小值</param>
        /// <param name="maxValue">最大值</param>
        /// <returns>是否有效</returns>
        bool IsValidPrice(decimal price, decimal minValue = 0, decimal maxValue = decimal.MaxValue);
    }

    /// <summary>
    /// 驗證服務實現
    /// </summary>
    public class ValidationService : IValidationService
    {
        private readonly ILogger<ValidationService> _logger;

        public ValidationService(ILogger<ValidationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ValidationResult ValidateModel<T>(T model) where T : class
        {
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var validationContext = new ValidationContext(model);

            var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

            var result = new ValidationResult();
            if (!isValid)
            {
                result.AddErrors(validationResults.Select(vr => vr.ErrorMessage ?? "驗證失敗"));
            }

            return result;
        }

        public ValidationResult ValidateProduct(Product product)
        {
            var result = new ValidationResult();

            if (product == null)
            {
                result.AddError("產品不能為空");
                return result;
            }

            // 基本驗證
            if (string.IsNullOrWhiteSpace(product.Name))
                result.AddError("產品名稱不能為空");

            if (string.IsNullOrWhiteSpace(product.SKU))
                result.AddError("產品編號不能為空");
            else if (!IsValidSKU(product.SKU))
                result.AddError("產品編號格式無效");

            if (product.Price < 0)
                result.AddError("產品價格不能為負數");

            if (product.CostPrice < 0)
                result.AddError("產品成本不能為負數");

            if (product.Weight < 0)
                result.AddError("產品重量不能為負數");

            // 長度驗證
            if (product.Name?.Length > 200)
                result.AddError("產品名稱長度不能超過200個字元");

            if (product.SKU?.Length > 50)
                result.AddError("產品編號長度不能超過50個字元");

            if (product.Description?.Length > 1000)
                result.AddError("產品描述長度不能超過1000個字元");

            // 業務邏輯驗證
            if (product.Price > 0 && product.CostPrice > product.Price)
                result.AddError("產品成本不能高於售價");

            return result;
        }

        public ValidationResult ValidateInventory(Inventory inventory)
        {
            var result = new ValidationResult();

            if (inventory == null)
            {
                result.AddError("庫存不能為空");
                return result;
            }

            // 基本驗證
            if (inventory.ProductId <= 0)
                result.AddError("產品ID必須大於0");

            if (!IsValidQuantity(inventory.Quantity))
                result.AddError("庫存數量必須為非負整數");

            if (!IsValidQuantity(inventory.SafetyStock))
                result.AddError("安全庫存必須為非負整數");

            if (!IsValidQuantity(inventory.ReservedQuantity))
                result.AddError("預留數量必須為非負整數");

            // 業務邏輯驗證
            if (inventory.ReservedQuantity > inventory.Quantity)
                result.AddError("預留數量不能超過總庫存數量");

            if (inventory.SafetyStock > inventory.Quantity * 10) // 安全庫存不應超過總庫存的10倍
                result.AddError("安全庫存設置過高，請檢查是否合理");

            return result;
        }

        public ValidationResult ValidatePurchase(Purchase purchase)
        {
            var result = new ValidationResult();

            if (purchase == null)
            {
                result.AddError("採購記錄不能為空");
                return result;
            }

            // 基本驗證
            if (purchase.ProductId <= 0)
                result.AddError("產品ID必須大於0");

            if (!IsValidQuantity(purchase.Quantity))
                result.AddError("採購數量必須為正整數");

            if (!IsValidPrice(purchase.UnitPrice))
                result.AddError("單價必須為正數");

            // 日期驗證（如果 Purchase 模型有日期屬性的話）
            // if (purchase.PurchaseDate > DateTime.Now)
            //     result.AddError("採購日期不能是未來日期");

            // 業務邏輯驗證
            if (purchase.Quantity > 10000)
                result.AddError("單次採購數量不能超過10000");

            if (purchase.UnitPrice > 1000000)
                result.AddError("單價不能超過1,000,000");

            return result;
        }

        public ValidationResult ValidateSale(Sale sale)
        {
            var result = new ValidationResult();

            if (sale == null)
            {
                result.AddError("銷售記錄不能為空");
                return result;
            }

            // 基本驗證
            if (sale.ProductId <= 0)
                result.AddError("產品ID必須大於0");

            if (!IsValidQuantity(sale.Quantity))
                result.AddError("銷售數量必須為正整數");

            if (!IsValidPrice(sale.UnitPrice))
                result.AddError("單價必須為正數");

            // 日期驗證（如果 Sale 模型有日期屬性的話）
            // if (sale.SaleDate > DateTime.Now)
            //     result.AddError("銷售日期不能是未來日期");

            // 業務邏輯驗證
            if (sale.Quantity > 1000)
                result.AddError("單次銷售數量不能超過1000");

            if (sale.UnitPrice > 100000)
                result.AddError("單價不能超過100,000");

            return result;
        }

        public bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            // 台灣手機號碼格式：09xxxxxxxx
            var phonePattern = @"^09\d{8}$";
            return Regex.IsMatch(phone, phonePattern);
        }

        public bool IsValidSKU(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return false;

            // SKU 格式：至少3個字元，只能包含字母、數字、連字符和底線
            var skuPattern = @"^[A-Za-z0-9_-]{3,50}$";
            return Regex.IsMatch(sku, skuPattern);
        }

        public bool IsValidQuantity(int quantity, int minValue = 0, int maxValue = int.MaxValue)
        {
            return quantity >= minValue && quantity <= maxValue;
        }

        public bool IsValidPrice(decimal price, decimal minValue = 0, decimal maxValue = decimal.MaxValue)
        {
            return price >= minValue && price <= maxValue;
        }
    }

}