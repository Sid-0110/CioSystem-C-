using CioSystem.Services;
using CioSystem.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CioSystem.Tests
{
    /// <summary>
    /// 驗證服務單元測試
    /// </summary>
    public class ValidationServiceTests
    {
        private readonly Mock<ILogger<ValidationService>> _mockLogger;
        private readonly ValidationService _validationService;

        public ValidationServiceTests()
        {
            _mockLogger = new Mock<ILogger<ValidationService>>();
            _validationService = new ValidationService(_mockLogger.Object);
        }

        [Fact]
        public void ValidateProduct_WithValidProduct_ReturnsValid()
        {
            // Arrange
            var product = new Product
            {
                Name = "測試產品",
                SKU = "TEST-001",
                Price = 100.00m,
                CostPrice = 80.00m,
                Weight = 1.5m
            };

            // Act
            var result = _validationService.ValidateProduct(product);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ValidateProduct_WithEmptyName_ReturnsInvalid()
        {
            // Arrange
            var product = new Product
            {
                Name = "",
                SKU = "TEST-001",
                Price = 100.00m
            };

            // Act
            var result = _validationService.ValidateProduct(product);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("產品名稱不能為空", result.Errors);
        }

        [Fact]
        public void ValidateProduct_WithInvalidSKU_ReturnsInvalid()
        {
            // Arrange
            var product = new Product
            {
                Name = "測試產品",
                SKU = "T", // 太短
                Price = 100.00m
            };

            // Act
            var result = _validationService.ValidateProduct(product);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("產品編號格式無效", result.Errors);
        }

        [Fact]
        public void ValidateProduct_WithNegativePrice_ReturnsInvalid()
        {
            // Arrange
            var product = new Product
            {
                Name = "測試產品",
                SKU = "TEST-001",
                Price = -100.00m
            };

            // Act
            var result = _validationService.ValidateProduct(product);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("產品價格不能為負數", result.Errors);
        }

        [Fact]
        public void ValidateProduct_WithCostHigherThanPrice_ReturnsInvalid()
        {
            // Arrange
            var product = new Product
            {
                Name = "測試產品",
                SKU = "TEST-001",
                Price = 100.00m,
                CostPrice = 150.00m
            };

            // Act
            var result = _validationService.ValidateProduct(product);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("產品成本不能高於售價", result.Errors);
        }

        [Fact]
        public void ValidateInventory_WithValidInventory_ReturnsValid()
        {
            // Arrange
            var inventory = new Inventory
            {
                ProductId = 1,
                Quantity = 100,
                SafetyStock = 10,
                ReservedQuantity = 5
            };

            // Act
            var result = _validationService.ValidateInventory(inventory);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ValidateInventory_WithReservedQuantityExceedingQuantity_ReturnsInvalid()
        {
            // Arrange
            var inventory = new Inventory
            {
                ProductId = 1,
                Quantity = 100,
                SafetyStock = 10,
                ReservedQuantity = 150 // 超過總庫存
            };

            // Act
            var result = _validationService.ValidateInventory(inventory);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("預留數量不能超過總庫存數量", result.Errors);
        }

        [Fact]
        public void ValidateInventory_WithNegativeQuantity_ReturnsInvalid()
        {
            // Arrange
            var inventory = new Inventory
            {
                ProductId = 1,
                Quantity = -10,
                SafetyStock = 10,
                ReservedQuantity = 5
            };

            // Act
            var result = _validationService.ValidateInventory(inventory);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("庫存數量必須為非負整數", result.Errors);
        }

        [Fact]
        public void IsValidEmail_WithValidEmail_ReturnsTrue()
        {
            // Arrange
            var email = "test@example.com";

            // Act
            var result = _validationService.IsValidEmail(email);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidEmail_WithInvalidEmail_ReturnsFalse()
        {
            // Arrange
            var email = "invalid-email";

            // Act
            var result = _validationService.IsValidEmail(email);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidPhone_WithValidPhone_ReturnsTrue()
        {
            // Arrange
            var phone = "0912345678";

            // Act
            var result = _validationService.IsValidPhone(phone);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidPhone_WithInvalidPhone_ReturnsFalse()
        {
            // Arrange
            var phone = "123456789";

            // Act
            var result = _validationService.IsValidPhone(phone);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidSKU_WithValidSKU_ReturnsTrue()
        {
            // Arrange
            var sku = "TEST-001";

            // Act
            var result = _validationService.IsValidSKU(sku);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidSKU_WithInvalidSKU_ReturnsFalse()
        {
            // Arrange
            var sku = "T"; // 太短

            // Act
            var result = _validationService.IsValidSKU(sku);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(100, true)]
        [InlineData(-1, false)]
        [InlineData(1001, false)]
        public void IsValidQuantity_WithDifferentValues_ReturnsExpectedResult(int quantity, bool expected)
        {
            // Act
            var result = _validationService.IsValidQuantity(quantity, 0, 1000);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0.0, true)]
        [InlineData(100.50, true)]
        [InlineData(-1.0, false)]
        [InlineData(1001.0, false)]
        public void IsValidPrice_WithDifferentValues_ReturnsExpectedResult(decimal price, bool expected)
        {
            // Act
            var result = _validationService.IsValidPrice(price, 0, 1000);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}