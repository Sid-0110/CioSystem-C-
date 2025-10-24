using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using CioSystem.Web.Controllers;
using CioSystem.Services;
using CioSystem.Models;
using Xunit;

namespace CioSystem.Tests
{
    /// <summary>
    /// 庫存控制器單元測試
    /// </summary>
    public class InventoryControllerTests
    {
        private readonly Mock<IInventoryService> _mockInventoryService;
        private readonly Mock<IProductService> _mockProductService;
        private readonly Mock<ILogger<InventoryController>> _mockLogger;
        private readonly InventoryController _controller;

        public InventoryControllerTests()
        {
            _mockInventoryService = new Mock<IInventoryService>();
            _mockProductService = new Mock<IProductService>();
            _mockLogger = new Mock<ILogger<InventoryController>>();

            _controller = new InventoryController(
                _mockInventoryService.Object,
                _mockProductService.Object,
                _mockLogger.Object,
                null!); // HubContext 在測試中為 null
        }

        [Fact]
        public async Task Index_ReturnsViewWithInventoryList()
        {
            // Arrange
            var inventories = new List<Inventory>
            {
                new Inventory { Id = 1, ProductId = 1, Quantity = 100 },
                new Inventory { Id = 2, ProductId = 2, Quantity = 50 }
            };

            _mockInventoryService.Setup(x => x.GetAllInventoryAsync())
                .ReturnsAsync(inventories);

            _mockInventoryService.Setup(x => x.GetInventoryPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<InventoryStatus?>()))
                .ReturnsAsync((inventories, inventories.Count));

            _mockProductService.Setup(x => x.GetAllProductsAsync())
                .ReturnsAsync(new List<Product>());

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.Model);
        }

        [Fact]
        public async Task Create_WithValidModel_ReturnsRedirectToIndex()
        {
            // Arrange
            var inventory = new Inventory
            {
                ProductId = 1,
                Quantity = 100,
                SafetyStock = 10
            };

            var createdInventory = new Inventory { Id = 1, ProductId = 1, Quantity = 100 };
            _mockInventoryService.Setup(x => x.CreateInventoryAsync(It.IsAny<Inventory>()))
                .ReturnsAsync(createdInventory);

            _mockProductService.Setup(x => x.GetAllProductsAsync())
                .ReturnsAsync(new List<Product>());

            // Act
            var result = await _controller.Create(inventory);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public async Task Create_WithInvalidModel_ReturnsViewWithModel()
        {
            // Arrange
            var inventory = new Inventory { ProductId = 0 }; // 無效的 ProductId
            _controller.ModelState.AddModelError("ProductId", "產品ID必須大於0");

            _mockProductService.Setup(x => x.GetAllProductsAsync())
                .ReturnsAsync(new List<Product>());

            // Act
            var result = await _controller.Create(inventory);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(inventory, viewResult.Model);
        }

        [Fact]
        public async Task Edit_WithValidId_ReturnsViewWithInventory()
        {
            // Arrange
            var inventoryId = 1;
            var inventory = new Inventory { Id = inventoryId, ProductId = 1, Quantity = 100 };

            _mockInventoryService.Setup(x => x.GetInventoryByIdAsync(inventoryId))
                .ReturnsAsync(inventory);

            _mockProductService.Setup(x => x.GetAllProductsAsync())
                .ReturnsAsync(new List<Product>());

            // Act
            var result = await _controller.Edit(inventoryId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(inventory, viewResult.Model);
        }

        [Fact]
        public async Task Edit_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var inventoryId = 999;
            _mockInventoryService.Setup(x => x.GetInventoryByIdAsync(inventoryId))
                .ReturnsAsync((Inventory?)null);

            // Act
            var result = await _controller.Edit(inventoryId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_WithValidModel_ReturnsRedirectToIndex()
        {
            // Arrange
            var inventoryId = 1;
            var inventory = new Inventory
            {
                Id = inventoryId,
                ProductId = 1,
                Quantity = 150,
                SafetyStock = 20
            };

            _mockInventoryService.Setup(x => x.UpdateInventoryAsync(inventoryId, It.IsAny<Inventory>()))
                .ReturnsAsync(inventory);

            // Act
            var result = await _controller.Edit(inventoryId, inventory);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public async Task Delete_WithValidId_ReturnsViewWithInventory()
        {
            // Arrange
            var inventoryId = 1;
            var inventory = new Inventory { Id = inventoryId, ProductId = 1, Quantity = 100 };

            _mockInventoryService.Setup(x => x.GetInventoryByIdAsync(inventoryId))
                .ReturnsAsync(inventory);

            // Act
            var result = await _controller.Delete(inventoryId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(inventory, viewResult.Model);
        }

        [Fact]
        public async Task DeleteConfirmed_WithValidId_ReturnsRedirectToIndex()
        {
            // Arrange
            var inventoryId = 1;
            _mockInventoryService.Setup(x => x.DeleteInventoryAsync(inventoryId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteConfirmed(inventoryId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }
    }
}