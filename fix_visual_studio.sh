#!/bin/bash

# Visual Studio 建置問題修復腳本
echo "🔧 修復 Visual Studio 建置問題..."

# 1. 關閉所有 Visual Studio 進程
echo "1. 關閉 Visual Studio 進程..."
pkill -f "Visual Studio" 2>/dev/null || echo "沒有找到運行中的 Visual Studio 進程"

# 2. 刪除 Visual Studio 快取
echo "2. 清理 Visual Studio 快取..."
rm -rf .vs

# 3. 清理 NuGet 快取
echo "3. 清理 NuGet 快取..."
find . -name "project.nuget.cache" -delete
find . -name "*.assets.cache" -delete
find . -name "project.assets.json" -delete

# 4. 清理 bin 和 obj 資料夾
echo "4. 清理建置輸出..."
find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true
find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true

# 5. 清理全域 NuGet 快取
echo "5. 清理全域 NuGet 快取..."
dotnet nuget locals all --clear

# 6. 強制還原套件
echo "6. 強制還原 NuGet 套件..."
dotnet restore --force --verbosity normal

# 7. 重新建置
echo "7. 重新建置專案..."
dotnet build --verbosity normal

# 8. 檢查結果
if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Visual Studio 建置問題修復完成！"
    echo ""
    echo "📋 下一步操作："
    echo "   1. 開啟 Visual Studio 2022 for Mac"
    echo "   2. 開啟 CioSystem.sln"
    echo "   3. 等待專案載入完成"
    echo "   4. 嘗試建置解決方案"
    echo "   5. 如果還有問題，請檢查 Visual Studio 的 .NET SDK 設定"
    echo ""
    echo "🎯 建置應該會成功！"
else
    echo ""
    echo "❌ 建置失敗，請檢查錯誤訊息"
    echo "   可能的問題："
    echo "   1. .NET SDK 版本不正確"
    echo "   2. 專案文件損壞"
    echo "   3. 權限問題"
fi

echo ""
echo "📚 如需更多幫助，請查看 fix_vs_build.md 文件"
