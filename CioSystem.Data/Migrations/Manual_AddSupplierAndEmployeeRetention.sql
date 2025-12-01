-- 手動添加 Supplier 和 EmployeeRetention 字段到 Purchases 表
-- 如果無法使用 dotnet ef database update，可以手動執行此 SQL 腳本

-- 檢查字段是否已存在，如果不存在則添加
-- 注意：SQLite 不支持 ALTER TABLE ADD COLUMN IF NOT EXISTS，所以需要先檢查

-- 添加 Supplier 字段（如果不存在）
-- 如果字段已存在，此命令會失敗，可以忽略錯誤
ALTER TABLE Purchases ADD COLUMN Supplier TEXT;

-- 添加 EmployeeRetention 字段（如果不存在）
-- 如果字段已存在，此命令會失敗，可以忽略錯誤
ALTER TABLE Purchases ADD COLUMN EmployeeRetention TEXT;

-- 更新現有記錄的 Supplier 字段為空字符串（如果為 NULL）
UPDATE Purchases SET Supplier = '' WHERE Supplier IS NULL;

