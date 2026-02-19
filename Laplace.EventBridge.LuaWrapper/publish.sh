#!/bin/bash

echo "===================================="
echo " Laplace Event Bridge Lua Wrapper"
echo " Native AOT 全平台发布脚本"
echo "===================================="
echo

cd "$(dirname "$0")"

OUTPUT_DIR="publish_output"

# 所有平台的架构
ALL_ARCHITECTURES="win-x64 win-x86 win-arm64 linux-x64 linux-arm64 osx-x64 osx-arm64"

echo "[1/4] 清理旧文件..."
if [ -d "$OUTPUT_DIR" ]; then
    rm -rf "$OUTPUT_DIR"
fi
mkdir -p "$OUTPUT_DIR"
echo

echo "[2/4] 发布全平台多架构 Native AOT 版本（这可能需要几分钟）..."
echo

BUILD_SUCCESS=1

for ARCH in $ALL_ARCHITECTURES; do
    echo "正在编译 $ARCH ..."
    ERROR_OUTPUT=$(dotnet publish -c Release -r "$ARCH" 2>&1)
    if [ $? -ne 0 ]; then
        echo "  ❌ $ARCH 编译失败："
        echo "  ----------------------------------------"
        echo "$ERROR_OUTPUT"
        echo "  ----------------------------------------"
        BUILD_SUCCESS=0
    else
        echo "  ✅ $ARCH 编译成功"
    fi
    echo
done

echo "[3/4] 组织输出文件..."
echo

# 创建平台和架构目录
mkdir -p "$OUTPUT_DIR/windows/x64"
mkdir -p "$OUTPUT_DIR/windows/x86"
mkdir -p "$OUTPUT_DIR/windows/arm64"
mkdir -p "$OUTPUT_DIR/linux/x64"
mkdir -p "$OUTPUT_DIR/linux/arm64"
mkdir -p "$OUTPUT_DIR/osx/x64"
mkdir -p "$OUTPUT_DIR/osx/arm64"

# 复制 Windows DLL
if [ -f "bin/Release/net10.0/win-x64/publish/Laplace.EventBridge.LuaWrapper.dll" ]; then
    cp "bin/Release/net10.0/win-x64/publish/Laplace.EventBridge.LuaWrapper.dll" "$OUTPUT_DIR/windows/x64/"
    echo "✅ Windows x64 DLL 已复制"
fi

if [ -f "bin/Release/net10.0/win-x86/publish/Laplace.EventBridge.LuaWrapper.dll" ]; then
    cp "bin/Release/net10.0/win-x86/publish/Laplace.EventBridge.LuaWrapper.dll" "$OUTPUT_DIR/windows/x86/"
    echo "✅ Windows x86 DLL 已复制"
fi

if [ -f "bin/Release/net10.0/win-arm64/publish/Laplace.EventBridge.LuaWrapper.dll" ]; then
    cp "bin/Release/net10.0/win-arm64/publish/Laplace.EventBridge.LuaWrapper.dll" "$OUTPUT_DIR/windows/arm64/"
    echo "✅ Windows arm64 DLL 已复制"
fi

# 复制 Linux SO
if [ -f "bin/Release/net10.0/linux-x64/publish/libLaplace.EventBridge.LuaWrapper.so" ]; then
    cp "bin/Release/net10.0/linux-x64/publish/libLaplace.EventBridge.LuaWrapper.so" "$OUTPUT_DIR/linux/x64/"
    chmod +x "$OUTPUT_DIR/linux/x64/libLaplace.EventBridge.LuaWrapper.so"
    echo "✅ Linux x64 SO 已复制"
fi

if [ -f "bin/Release/net10.0/linux-arm64/publish/libLaplace.EventBridge.LuaWrapper.so" ]; then
    cp "bin/Release/net10.0/linux-arm64/publish/libLaplace.EventBridge.LuaWrapper.so" "$OUTPUT_DIR/linux/arm64/"
    chmod +x "$OUTPUT_DIR/linux/arm64/libLaplace.EventBridge.LuaWrapper.so"
    echo "✅ Linux arm64 SO 已复制"
fi

# 复制 macOS DYLIB
if [ -f "bin/Release/net10.0/osx-x64/publish/libLaplace.EventBridge.LuaWrapper.dylib" ]; then
    cp "bin/Release/net10.0/osx-x64/publish/libLaplace.EventBridge.LuaWrapper.dylib" "$OUTPUT_DIR/osx/x64/"
    chmod +x "$OUTPUT_DIR/osx/x64/libLaplace.EventBridge.LuaWrapper.dylib"
    echo "✅ macOS x64 DYLIB 已复制"
fi

if [ -f "bin/Release/net10.0/osx-arm64/publish/libLaplace.EventBridge.LuaWrapper.dylib" ]; then
    cp "bin/Release/net10.0/osx-arm64/publish/libLaplace.EventBridge.LuaWrapper.dylib" "$OUTPUT_DIR/osx/arm64/"
    chmod +x "$OUTPUT_DIR/osx/arm64/libLaplace.EventBridge.LuaWrapper.dylib"
    echo "✅ macOS arm64 DYLIB 已复制"
fi

# 复制 Lua 文件
cp "laplace_event_bridge.lua" "$OUTPUT_DIR/"
cp "example.lua" "$OUTPUT_DIR/"
echo "✅ Lua 文件已复制"
echo

echo "[4/4] 生成文件清单..."
echo
echo "输出目录: $(pwd)/$OUTPUT_DIR"
echo
echo "========== Windows 平台 =========="
echo "windows/"
echo "  ├─ x64/"
if [ -f "$OUTPUT_DIR/windows/x64/Laplace.EventBridge.LuaWrapper.dll" ]; then
    SIZE=$(stat -f%z "$OUTPUT_DIR/windows/x64/Laplace.EventBridge.LuaWrapper.dll" 2>/dev/null || stat -c%s "$OUTPUT_DIR/windows/x64/Laplace.EventBridge.LuaWrapper.dll" 2>/dev/null)
    echo "  │   └─ Laplace.EventBridge.LuaWrapper.dll ($SIZE bytes)"
else
    echo "  │   └─ (未生成)"
fi
echo "  ├─ x86/"
if [ -f "$OUTPUT_DIR/windows/x86/Laplace.EventBridge.LuaWrapper.dll" ]; then
    SIZE=$(stat -f%z "$OUTPUT_DIR/windows/x86/Laplace.EventBridge.LuaWrapper.dll" 2>/dev/null || stat -c%s "$OUTPUT_DIR/windows/x86/Laplace.EventBridge.LuaWrapper.dll" 2>/dev/null)
    echo "  │   └─ Laplace.EventBridge.LuaWrapper.dll ($SIZE bytes)"
else
    echo "  │   └─ (未生成)"
fi
echo "  └─ arm64/"
if [ -f "$OUTPUT_DIR/windows/arm64/Laplace.EventBridge.LuaWrapper.dll" ]; then
    SIZE=$(stat -f%z "$OUTPUT_DIR/windows/arm64/Laplace.EventBridge.LuaWrapper.dll" 2>/dev/null || stat -c%s "$OUTPUT_DIR/windows/arm64/Laplace.EventBridge.LuaWrapper.dll" 2>/dev/null)
    echo "      └─ Laplace.EventBridge.LuaWrapper.dll ($SIZE bytes)"
else
    echo "      └─ (未生成)"
fi
echo
echo "========== Linux 平台 =========="
echo "linux/"
echo "  ├─ x64/"
if [ -f "$OUTPUT_DIR/linux/x64/libLaplace.EventBridge.LuaWrapper.so" ]; then
    SIZE=$(stat -f%z "$OUTPUT_DIR/linux/x64/libLaplace.EventBridge.LuaWrapper.so" 2>/dev/null || stat -c%s "$OUTPUT_DIR/linux/x64/libLaplace.EventBridge.LuaWrapper.so" 2>/dev/null)
    echo "  │   └─ libLaplace.EventBridge.LuaWrapper.so ($SIZE bytes)"
else
    echo "  │   └─ (未生成)"
fi
echo "  └─ arm64/"
if [ -f "$OUTPUT_DIR/linux/arm64/libLaplace.EventBridge.LuaWrapper.so" ]; then
    SIZE=$(stat -f%z "$OUTPUT_DIR/linux/arm64/libLaplace.EventBridge.LuaWrapper.so" 2>/dev/null || stat -c%s "$OUTPUT_DIR/linux/arm64/libLaplace.EventBridge.LuaWrapper.so" 2>/dev/null)
    echo "      └─ libLaplace.EventBridge.LuaWrapper.so ($SIZE bytes)"
else
    echo "      └─ (未生成)"
fi
echo
echo "========== macOS 平台 =========="
echo "osx/"
echo "  ├─ x64/"
if [ -f "$OUTPUT_DIR/osx/x64/libLaplace.EventBridge.LuaWrapper.dylib" ]; then
    SIZE=$(stat -f%z "$OUTPUT_DIR/osx/x64/libLaplace.EventBridge.LuaWrapper.dylib" 2>/dev/null || stat -c%s "$OUTPUT_DIR/osx/x64/libLaplace.EventBridge.LuaWrapper.dylib" 2>/dev/null)
    echo "  │   └─ libLaplace.EventBridge.LuaWrapper.dylib ($SIZE bytes)"
else
    echo "  │   └─ (未生成)"
fi
echo "  └─ arm64/"
if [ -f "$OUTPUT_DIR/osx/arm64/libLaplace.EventBridge.LuaWrapper.dylib" ]; then
    SIZE=$(stat -f%z "$OUTPUT_DIR/osx/arm64/libLaplace.EventBridge.LuaWrapper.dylib" 2>/dev/null || stat -c%s "$OUTPUT_DIR/osx/arm64/libLaplace.EventBridge.LuaWrapper.dylib" 2>/dev/null)
    echo "      └─ libLaplace.EventBridge.LuaWrapper.dylib ($SIZE bytes)"
else
    echo "      └─ (未生成)"
fi
echo
echo "========== Lua 文件 =========="
echo "laplace_event_bridge.lua"
echo "example.lua"
echo

echo "===================================="
echo " 发布完成！"
echo "===================================="
echo
echo "下一步操作:"
echo "1. 将整个 $OUTPUT_DIR 目录复制到你的 Lua 项目"
echo "2. 确保保持目录结构 (windows/linux/osx 及其子目录)"
echo "3. Lua 脚本会自动根据系统平台和架构加载对应的库文件"
echo "4. 参考 example.lua 使用"
echo
