# CioSystem v1 (C#) - 學習建構模式專案

## 專案概述

這是一個完整的 C# 庫存管理系統學習專案，展示了現代 .NET 應用程式的最佳實踐和設計模式。

## 專案架構

### 分層架構 (Layered Architecture)

```
CioSystem_v1(C#)/
├── CioSystem.Core/          # 核心層 - 基礎抽象和介面
├── CioSystem.Models/        # 模型層 - 資料實體和 DTO
├── CioSystem.Data/          # 資料層 - 資料存取和儲存庫實作
├── CioSystem.Services/      # 服務層 - 業務邏輯
├── CioSystem.API/           # API層 - Web API 控制器
└── CioSystem.Web/           # 展示層 - MVC Web 應用程式
```

## 設計模式學習重點

### 1. Repository Pattern (儲存庫模式)
- **位置**: `CioSystem.Core/IRepository.cs`
- **目的**: 抽象化資料存取邏輯
- **優點**: 
  - 分離資料存取邏輯
  - 便於單元測試
  - 支援依賴注入

### 2. Unit of Work Pattern (工作單元模式)
- **位置**: `CioSystem.Core/IUnitOfWork.cs`
- **目的**: 管理資料庫交易和儲存庫
- **優點**:
  - 確保資料一致性
  - 批次操作優化
  - 交易管理

### 3. Dependency Injection (依賴注入)
- **目的**: 鬆耦合設計
- **優點**:
  - 提高可測試性
  - 便於維護和擴展
  - 符合 SOLID 原則

### 4. Base Entity Pattern (基礎實體模式)
- **位置**: `CioSystem.Core/BaseEntity.cs`
- **目的**: 統一實體基礎屬性
- **優點**:
  - 減少重複程式碼
  - 統一審計欄位
  - 軟刪除支援

## 學習路徑

### 階段一：基礎架構理解
1. 研究 `BaseEntity` 類別設計
2. 理解 `IRepository` 介面設計
3. 學習 `IUnitOfWork` 模式

### 階段二：模型設計
1. 分析 `Product` 實體模型
2. 研究 `Inventory` 庫存模型
3. 理解實體關係設計

### 階段三：資料存取層
1. 實作 Repository 模式
2. 實作 Unit of Work 模式
3. 學習 Entity Framework Core

### 階段四：業務邏輯層
1. 實作 Service 層
2. 學習業務邏輯封裝
3. 實作驗證和異常處理

### 階段五：API 層
1. 實作 Web API 控制器
2. 學習 RESTful API 設計
3. 實作 API 文件

### 階段六：展示層
1. 實作 MVC 控制器
2. 學習 Razor 視圖
3. 實作前端互動

## 技術棧

- **.NET 8**: 最新版本的 .NET 框架
- **Entity Framework Core**: ORM 框架
- **ASP.NET Core**: Web 應用程式框架
- **SQL Server**: 資料庫（可替換為其他資料庫）
- **AutoMapper**: 物件對應
- **FluentValidation**: 資料驗證

## 學習目標

1. **理解分層架構**: 學習如何組織大型應用程式
2. **掌握設計模式**: 實作常用的設計模式
3. **學習最佳實踐**: 遵循 .NET 社群的最佳實踐
4. **提升程式品質**: 寫出可維護、可測試的程式碼

## 如何使用這個專案

1. **學習**: 閱讀程式碼註解和架構說明
2. **實作**: 按照學習路徑逐步實作
3. **練習**: 在桌面的 `CioSystem_v1(C#)` 中練習
4. **比較**: 對比學習版本和練習版本

## 下一步

1. 開始實作資料存取層
2. 建立資料庫模型
3. 實作業務邏輯服務
4. 建立 API 端點
5. 實作 Web 介面

## 注意事項

- 這是學習用的範例專案
- 程式碼包含詳細的註解說明
- 遵循 SOLID 原則和 Clean Architecture
- 適合 .NET 初學者到中級開發者學習