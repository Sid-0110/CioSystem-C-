-- 修復 Inventory 表中 EmployeeRetention 字段的 NOT NULL 約束問題
-- 如果字段是必填的，需要將其改為可空或設置默認值

-- 方法 1: 如果字段不存在，添加它（可空）
-- ALTER TABLE Inventory ADD COLUMN EmployeeRetention TEXT;

-- 方法 2: 如果字段存在但為必填，需要重建表（SQLite 不支持直接修改列）
-- 步驟：
-- 1. 創建新表
CREATE TABLE Inventory_new (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductId INTEGER NOT NULL,
    Quantity INTEGER NOT NULL,
    ProductSKU TEXT,
    Type INTEGER NOT NULL DEFAULT 1,
    Status INTEGER NOT NULL DEFAULT 1,
    ReservedQuantity INTEGER NOT NULL DEFAULT 0,
    SafetyStock INTEGER NOT NULL DEFAULT 0,
    ProductionDate TEXT,
    WarningLevel INTEGER,
    Notes TEXT,
    EmployeeRetention TEXT,  -- 改為可空
    LastCountDate TEXT,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    UpdatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    CreatedBy TEXT NOT NULL,
    UpdatedBy TEXT NOT NULL,
    FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE
);

-- 2. 複製數據
INSERT INTO Inventory_new 
SELECT 
    Id, ProductId, Quantity, ProductSKU, Type, Status, 
    ReservedQuantity, SafetyStock, ProductionDate, WarningLevel, 
    Notes, COALESCE(EmployeeRetention, '') as EmployeeRetention,  -- 將 NULL 轉為空字符串
    LastCountDate, CreatedAt, UpdatedAt, IsDeleted, CreatedBy, UpdatedBy
FROM Inventory;

-- 3. 刪除舊表
DROP TABLE Inventory;

-- 4. 重命名新表
ALTER TABLE Inventory_new RENAME TO Inventory;

-- 5. 重建索引
CREATE INDEX IX_Inventory_ProductId ON Inventory(ProductId);
CREATE INDEX IX_Inventory_ProductSKU ON Inventory(ProductSKU);
CREATE INDEX IX_Inventory_Status ON Inventory(Status);
CREATE INDEX IX_Inventory_Type ON Inventory(Type);

-- 注意：執行此腳本前請先備份數據庫！

