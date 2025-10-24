#!/bin/bash

# CioSystem_v1 .NET 8.0 環境設定腳本
echo "🚀 設定 .NET 8.0 環境..."

# 設定環境變數
export DOTNET_ROOT=/usr/local/share/dotnet
export MSBuildSDKsPath=/usr/local/share/dotnet/sdk/8.0.414/Sdks
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

echo "✅ 環境變數已設定："
echo "   DOTNET_ROOT: $DOTNET_ROOT"
echo "   MSBuildSDKsPath: $MSBuildSDKsPath"

echo ""
echo "📋 當前 .NET 版本: $(dotnet --version)"
echo "📋 可用 SDK 版本:"
dotnet --list-sdks

echo ""
echo "🔨 測試建置專案..."
dotnet build CioSystem.sln

if [ $? -eq 0 ]; then
    echo ""
    echo "🎉 建置成功！.NET 8.0 環境設定完成。"
    echo ""
    echo "📚 下一步："
    echo "   1. 在 Visual Studio 2022 for Mac 中開啟 CioSystem.sln"
    echo "   2. 閱讀 Documentation/C# 架構學習指南.md"
    echo "   3. 開始您的 C# 架構學習之旅！"
else
    echo ""
    echo "❌ 建置失敗。請檢查錯誤訊息。"
    exit 1
fi
