#!/bin/bash

# Visual Studio 清理腳本
echo "🧹 清理 Visual Studio 快取和臨時文件..."

# 刪除 Visual Studio 隱藏資料夾
echo "刪除 .vs 資料夾..."
rm -rf .vs

# 清理 NuGet 快取
echo "清理 NuGet 快取..."
find . -name "project.nuget.cache" -delete
find . -name "*.assets.cache" -delete

# 清理 bin 和 obj 資料夾
echo "清理 bin 和 obj 資料夾..."
find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true
find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true

# 強制還原和重建
echo "強制還原 NuGet 套件..."
dotnet restore --force

echo "重建專案..."
dotnet build

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Visual Studio 清理完成！"
    echo ""
    echo "📋 建議："
    echo "   1. 關閉 Visual Studio 2022 for Mac"
    echo "   2. 重新開啟 CioSystem.sln"
    echo "   3. 清理問題應該已解決"
else
    echo ""
    echo "❌ 重建失敗，請檢查錯誤訊息"
fi
