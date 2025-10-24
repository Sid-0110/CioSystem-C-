#!/bin/bash

# CioSystem v1 (C#) 建置腳本
# 用於學習建構模式的專案建置

echo "=========================================="
echo "CioSystem v1 (C#) - 學習建構模式專案"
echo "=========================================="

# 檢查 .NET 是否已安裝
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET 未安裝，請先安裝 .NET 8 SDK"
    echo "下載連結: https://dotnet.microsoft.com/download"
    exit 1
fi

echo "✅ .NET 版本: $(dotnet --version)"

# 還原 NuGet 套件
echo ""
echo "📦 還原 NuGet 套件..."
dotnet restore

if [ $? -ne 0 ]; then
    echo "❌ 套件還原失敗"
    exit 1
fi

echo "✅ 套件還原完成"

# 建置解決方案
echo ""
echo "🔨 建置解決方案..."
dotnet build --configuration Release

if [ $? -ne 0 ]; then
    echo "❌ 建置失敗"
    exit 1
fi

echo "✅ 建置成功"

# 執行測試（如果有）
echo ""
echo "🧪 執行測試..."
dotnet test --configuration Release --verbosity minimal

if [ $? -ne 0 ]; then
    echo "⚠️  測試執行失敗或沒有測試專案"
else
    echo "✅ 測試通過"
fi

echo ""
echo "🎉 建置完成！"
echo ""
echo "學習建議："
echo "1. 閱讀 README.md 了解專案架構"
echo "2. 查看 架構學習指南.md 學習設計模式"
echo "3. 按照 學習進度.md 逐步學習"
echo "4. 在桌面的 CioSystem_v1(C#) 中練習實作"
echo ""
echo "下一步："
echo "- 實作資料存取層"
echo "- 學習 Entity Framework Core"
echo "- 實作 Repository 模式"
echo ""
echo "=========================================="