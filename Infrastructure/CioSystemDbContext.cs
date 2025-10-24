using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using CioSystem.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace CioSystem.Data
{
    /// <summary>
    /// Entity Framework Core 資料庫上下文
    /// 這是學習資料存取層的核心類別
    /// </summary>
    public class CioSystemDbContext : Dbcontext
	{
        /// <summary>
        /// 建構函式 - 使用依賴注入的 DbContextOptions
        /// </summary>
        /// <param name="options">資料庫選項</param>
		public CioSystemDbContext(DbContxtOptions<CioSystemDbContext> options) : base(options)
		{
		}

        /// <summary>
        /// 產品資料表
        /// </summary>
        public DbSet<Product> Products { get; set; }

        /// <summary>
        /// 庫存資料表
        /// </summary>
        public DbSet<Inventory> Inventory { get; set; }

        /// <summary>
        /// 庫存移動記錄資料表
        /// </summary>
        public DbSet<InventoryMovement> InventoryMovements { get; set; }

        /// <summary>
        /// 採購記錄資料表
        /// </summary>
        public DbSet<Purchase> Purchases { get; set; }

        /// <summary>
        /// 銷售記錄資料表
        /// </summary>
        public DbSet<Sale> Sales { get; set; }

        /// <summary>
        /// 模型配置 - 設定實體關係和約束
        /// </summary>
        /// <param name="modelBuilder">模型建構器</param>
        protected override void OnModelCreating(ModulBuilder modulBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置 Product 實體
            ConfigureProduct(modelBuilder);

            // 配置 Inventory 實體
            ConfigureInventory(modelBuilder);

            // 配置 InventoryMovement 實體
            ConfigureInventoryMovement(modelBuilder);

            // 配置 Purchase 實體
            ConfigurePurchase(modelBuilder);

            // 配置 Sale 實體
            ConfigureSale(modelBuilder);

            // 設定全域查詢過濾器（軟刪除）
            ConfigureGlobalQueryFilters(modelBuilder);
        }

        /// <summary>
        /// 配置 Product 實體
        /// </summary>
        private void ConfigureProduct(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>(entity =>
            {
                // 設定主鍵
                entity.HasKey(e => e.Id);

                // 設定欄位約束
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Description)
                    .HasMaxLength(1000);

                entity.Property(e => e.Price)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(e => e.Category)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.SKU)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Brand)
                    .HasMaxLength(100);

                entity.Property(e => e.Weight)
                    .HasColumnType("decimal(10,3)");

                entity.Property(e => e.Dimensions)
                    .HasMaxLength(100);

                entity.Property(e => e.Color)
                    .HasMaxLength(50);

                entity.Property(e => e.ImageUrl)
                    .HasMaxLength(500);

                entity.Property(e => e.Tags)
                    .HasMaxLength(200);

                entity.Property(e => e.Notes)
                    .HasMaxLength(500);

                // 設定索引
                entity.HasIndex(e => e.SKU)
                    .IsUnique();

                entity.HasIndex(e => e.Name);

                entity.HasIndex(e => e.Category);

                entity.HasIndex(e => e.Status);

                // 設定狀態預設值
                entity.Property(e => e.Status)
                    .HasDefaultValue(ProductStatus.Active);

                // 設定審計欄位預設值
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.IsDeleted)
                    .HasDefaultValue(false);
            });
        }
        /// <summary>
        /// 配置 Inventory 實體
        /// </summary>
        private void ConfigureInventory(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Inventory>(entity =>
            {
                // 設定主鍵
                entity.HasKey(e => e.Id);

                // 設定外鍵關係
                entity.HasOne(e => e.Product)
                    .WithMany(p => p.InventoryItems)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                // 設定欄位約束
                entity.Property(e => e.Quantity)
                    .IsRequired();

                entity.Property(e => e.Location)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.BatchNumber)
                    .HasMaxLength(50);

                entity.Property(e => e.CostPrice)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.Notes)
                    .HasMaxLength(500);

                // 設定索引
                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.Location);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.Type);

                // 設定預設值
                entity.Property(e => e.Type)
                    .HasDefaultValue(InventoryType.Stock);

                entity.Property(e => e.Status)
                    .HasDefaultValue(InventoryStatus.Available);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.IsDeleted)
                    .HasDefaultValue(false);
            });
        }

        /// <summary>
        /// 配置 InventoryMovement 實體
        /// </summary>
        private void ConfigureInventoryMovement(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InventoryMovement>(entity =>
            {
                // 設定主鍵
                entity.HasKey(e => e.Id);

                // 設定外鍵關係
                entity.HasOne(e => e.Inventory)
                    .WithMany(i => i.Movements)
                    .HasForeignKey(e => e.InventoryId)
                    .OnDelete(DeleteBehavior.Cascade);

                // 設定欄位約束
                entity.Property(e => e.Quantity)
                    .IsRequired();

                entity.Property(e => e.PreviousQuantity)
                    .IsRequired();

                entity.Property(e => e.NewQuantity)
                    .IsRequired();

                entity.Property(e => e.Reason)
                    .HasMaxLength(200);

                // 設定索引
                entity.HasIndex(e => e.InventoryId);
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.CreatedAt);

                // 設定預設值
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.IsDeleted)
                    .HasDefaultValue(false);
            });
        }

        /// <summary>
        /// 配置 Purchase 實體
        /// </summary>
        private void ConfigurePurchase(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Purchase>(entity =>
            {
                // 設定主鍵
                entity.HasKey(e => e.Id);

                // 設定外鍵關係
                entity.HasOne<Product>()
                    .WithMany(p => p.Purchases)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                // 設定欄位約束
                entity.Property(e => e.Quantity)
                    .IsRequired();

                entity.Property(e => e.UnitPrice)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                // 設定索引
                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.CreatedAt);

                // 設定預設值
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.IsDeleted)
                    .HasDefaultValue(false);
            });
        }

        /// <summary>
        /// 配置 Sale 實體
        /// </summary>
        private void ConfigureSale(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Sale>(entity =>
            {
                // 設定主鍵
                entity.HasKey(e => e.Id);

                // 設定外鍵關係
                entity.HasOne<Product>()
                    .WithMany(p => p.Sales)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                // 設定欄位約束
                entity.Property(e => e.Quantity)
                    .IsRequired();

                entity.Property(e => e.UnitPrice)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                // 設定索引
                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.CreatedAt);

                // 設定預設值
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.IsDeleted)
                    .HasDefaultValue(false);
            });
        }

        /// <summary>
        /// 設定全域查詢過濾器（軟刪除）
        /// </summary>
        private void ConfigureGlobalQueryFilters(ModelBuilder modelBuilder)
        {
            // 為所有繼承 BaseEntity 的實體設定軟刪除過濾器
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(CioSystem.Core.BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var property = Expression.Property(parameter, "IsDeleted");
                    var constant = Expression.Constant(false);
                    var equal = Expression.Equal(property, constant);
                    var lambda = Expression.Lambda(equal, parameter);

                    modelBuilder.Entity(entityType.ClrType)
                        .HasQueryFilter(lambda);
                }
            }
        }

        /// <summary>
        /// 儲存變更時自動更新審計欄位
        /// </summary>
        /// <returns>受影響的記錄數</returns>
        public override int SaveChanges()
        {
            UpdateAuditFields();
            return base.SaveChanges();
        }

        /// <summary>
        /// 儲存變更時自動更新審計欄位（非同步版本）
        /// </summary>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>受影響的記錄數</returns>
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditFields();
            return base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// 更新審計欄位
        /// </summary>
        private void UpdateAuditFields()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is CioSystem.Core.BaseEntity &&
                           (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var entity = (CioSystem.Core.BaseEntity)entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                    entity.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entity.UpdatedAt = DateTime.UtcNow;
                    // 防止修改 CreatedAt
                    entry.Property(nameof(entity.CreatedAt)).IsModified = false;
                }
            }
        }
    }
}

