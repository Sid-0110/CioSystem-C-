# CioSystem Windows 快速安裝包

## 📦 安裝包內容

本安裝包包含以下檔案：

- `install.bat` - 自動安裝程式
- `uninstall.bat` - 解除安裝程式  
- `start.bat` - 手動啟動程式
- `README_安裝說明.md` - 本說明檔案

## 🚀 快速安裝

### 方法一：自動安裝（推薦）

1. **下載安裝包**
   - 將整個 CioSystem 專案資料夾下載到您的電腦

2. **執行安裝程式**
   - 右鍵點擊 `install.bat`
   - 選擇「以系統管理員身分執行」
   - 按照提示完成安裝

3. **啟動應用程式**
   - 安裝完成後，雙擊桌面上的「CioSystem」捷徑
   - 或執行 `start.bat` 檔案

### 方法二：手動安裝

1. **安裝 .NET 8.0 Runtime**
   - 前往 [Microsoft .NET 下載頁面](https://dotnet.microsoft.com/download/dotnet/8.0)
   - 下載並安裝 .NET 8.0 Runtime

2. **複製檔案**
   - 將 CioSystem 專案複製到 `C:\CioSystem\`

3. **編譯應用程式**
   ```cmd
   cd C:\CioSystem\CioSystem.Web
   dotnet build
   ```

4. **啟動應用程式**
   ```cmd
   dotnet run --urls "http://localhost:5023"
   ```

## 🎯 首次使用

1. **開啟瀏覽器**
   - 前往 `http://localhost:5023`

2. **註冊管理員帳號**
   - 點擊「註冊新帳號」
   - 填寫必要資訊
   - 角色選擇「管理員」

3. **開始使用**
   - 使用新帳號登入系統
   - 前往「系統設定」完成基本設定

## 🛠️ 管理工具

### 啟動應用程式
- **桌面捷徑**：雙擊桌面上的 CioSystem 圖示
- **手動啟動**：執行 `start.bat` 檔案

### 停止應用程式
- 在命令視窗中按 `Ctrl+C`
- 或關閉命令視窗

### 解除安裝
- 執行 `uninstall.bat` 檔案
- 選擇是否保留資料庫檔案

## 📁 檔案結構

安裝完成後的目錄結構：

```
C:\CioSystem\
├── CioSystem.Web\          # 主要應用程式
│   ├── CioSystem.db        # 資料庫檔案
│   ├── logs\               # 日誌檔案
│   └── ...
├── start.bat              # 啟動腳本
└── ...
```

## ⚙️ 進階設定

### 變更埠號
編輯 `start.bat` 檔案，修改以下行：
```batch
dotnet run --urls "http://localhost:5024"
```

### 自動啟動
將 `start.bat` 複製到 Windows 啟動資料夾：
```
%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup
```

### 備份資料庫
定期備份 `C:\CioSystem\CioSystem.Web\CioSystem.db` 檔案

## 🔧 疑難排解

### 問題：無法啟動應用程式
**解決方案：**
1. 確認以系統管理員身分執行
2. 檢查 .NET 8.0 是否已安裝
3. 確認埠號 5023 未被其他程式佔用

### 問題：防火牆阻擋
**解決方案：**
1. 執行 `install.bat` 自動設定防火牆
2. 或手動在 Windows 防火牆中允許 dotnet.exe

### 問題：資料庫錯誤
**解決方案：**
1. 檢查 `CioSystem.db` 檔案是否存在
2. 確認檔案權限正確
3. 重新編譯應用程式

## 📞 技術支援

### 日誌檔案
- 位置：`C:\CioSystem\CioSystem.Web\logs\system_logs.json`
- 用於診斷問題和錯誤追蹤

### 系統需求
- Windows 10 或更新版本
- .NET 8.0 Runtime
- 至少 4GB RAM
- 1GB 可用磁碟空間

### 常見問題
1. **首次啟動慢**：正常現象，資料庫初始化需要時間
2. **埠號被佔用**：使用不同埠號或停止佔用程式
3. **權限不足**：以系統管理員身分執行

## 🔄 更新應用程式

1. **停止現有應用程式**
2. **備份資料庫檔案**
3. **替換應用程式檔案**
4. **重新編譯**：`dotnet build`
5. **重新啟動應用程式**

## 📋 注意事項

- 首次安裝需要網路連線下載 .NET Runtime
- 建議定期備份資料庫檔案
- 應用程式使用 SQLite 資料庫，無需額外安裝資料庫軟體
- 如需多用戶使用，建議部署到專用伺服器

---

**版本**：v1.0  
**更新日期**：2025年10月23日  
**適用系統**：Windows 10/11