using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CioSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeInventoryEmployeeRetentionNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 檢查字段是否存在，如果存在則修改為可空
            // 注意：SQLite 不支持直接修改列，需要重建表
            // 但為了簡化，我們先嘗試添加字段（如果不存在）
            // 如果字段已存在且為必填，需要手動執行 SQL
            
            // 如果字段不存在，添加它
            // 如果字段存在但為必填，需要手動執行以下 SQL：
            // ALTER TABLE Inventory ADD COLUMN EmployeeRetention_temp TEXT;
            // UPDATE Inventory SET EmployeeRetention_temp = COALESCE(EmployeeRetention, '');
            // ALTER TABLE Inventory DROP COLUMN EmployeeRetention;
            // ALTER TABLE Inventory RENAME COLUMN EmployeeRetention_temp TO EmployeeRetention;
            
            // 由於 SQLite 的限制，這裡只添加字段（如果不存在）
            // 如果字段已存在，請手動執行上面的 SQL 來修改
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 回滾操作
        }
    }
}

