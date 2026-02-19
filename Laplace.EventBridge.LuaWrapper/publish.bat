@echo off
chcp 65001 >nul
echo ====================================
echo  Laplace Event Bridge Lua Wrapper
echo  Native AOT 多架构发布脚本
echo ====================================
echo.

cd /d "%~dp0"

set OUTPUT_DIR=publish_output
set ARCHITECTURES=win-x64 win-x86 win-arm64 linux-x64 linux-arm64 osx-x64 osx-arm64

echo [1/4] 清理旧文件...
if exist "%OUTPUT_DIR%" (
    rmdir /s /q "%OUTPUT_DIR%"
)
mkdir "%OUTPUT_DIR%"
echo.

echo [2/4] 发布全平台多架构 Native AOT 版本（这可能需要几分钟）...
echo.

set BUILD_SUCCESS=1

for %%A in (%ARCHITECTURES%) do (
    echo 正在编译 %%A ...
    dotnet publish -c Release -r %%A >temp_output.txt 2>&1
    if errorlevel 1 (
        echo   ❌ %%A 编译失败：
        echo   ----------------------------------------
        type temp_output.txt
        echo   ----------------------------------------
        set BUILD_SUCCESS=0
    ) else (
        echo   ✅ %%A 编译成功
    )
    if exist temp_output.txt del temp_output.txt
    echo.
)

echo [3/4] 组织输出文件...
echo.

rem 创建架构目录
mkdir "%OUTPUT_DIR%\windows" >nul 2>&1
mkdir "%OUTPUT_DIR%\windows\x64" >nul 2>&1
mkdir "%OUTPUT_DIR%\windows\x86" >nul 2>&1
mkdir "%OUTPUT_DIR%\windows\arm64" >nul 2>&1
mkdir "%OUTPUT_DIR%\linux" >nul 2>&1
mkdir "%OUTPUT_DIR%\linux\x64" >nul 2>&1
mkdir "%OUTPUT_DIR%\linux\arm64" >nul 2>&1
mkdir "%OUTPUT_DIR%\osx" >nul 2>&1
mkdir "%OUTPUT_DIR%\osx\x64" >nul 2>&1
mkdir "%OUTPUT_DIR%\osx\arm64" >nul 2>&1

rem 复制 Windows DLL 到对应架构目录
if exist "bin\Release\net10.0\win-x64\publish\Laplace.EventBridge.LuaWrapper.dll" (
    copy /y "bin\Release\net10.0\win-x64\publish\Laplace.EventBridge.LuaWrapper.dll" "%OUTPUT_DIR%\windows\x64\" >nul
    echo ✅ Windows x64 DLL 已复制
)

if exist "bin\Release\net10.0\win-x86\publish\Laplace.EventBridge.LuaWrapper.dll" (
    copy /y "bin\Release\net10.0\win-x86\publish\Laplace.EventBridge.LuaWrapper.dll" "%OUTPUT_DIR%\windows\x86\" >nul
    echo ✅ Windows x86 DLL 已复制
)

if exist "bin\Release\net10.0\win-arm64\publish\Laplace.EventBridge.LuaWrapper.dll" (
    copy /y "bin\Release\net10.0\win-arm64\publish\Laplace.EventBridge.LuaWrapper.dll" "%OUTPUT_DIR%\windows\arm64\" >nul
    echo ✅ Windows arm64 DLL 已复制
)

rem 复制 Linux SO 到对应架构目录
if exist "bin\Release\net10.0\linux-x64\publish\libLaplace.EventBridge.LuaWrapper.so" (
    copy /y "bin\Release\net10.0\linux-x64\publish\libLaplace.EventBridge.LuaWrapper.so" "%OUTPUT_DIR%\linux\x64\" >nul
    echo ✅ Linux x64 SO 已复制
)

if exist "bin\Release\net10.0\linux-arm64\publish\libLaplace.EventBridge.LuaWrapper.so" (
    copy /y "bin\Release\net10.0\linux-arm64\publish\libLaplace.EventBridge.LuaWrapper.so" "%OUTPUT_DIR%\linux\arm64\" >nul
    echo ✅ Linux arm64 SO 已复制
)

rem 复制 macOS DYLIB 到对应架构目录
if exist "bin\Release\net10.0\osx-x64\publish\libLaplace.EventBridge.LuaWrapper.dylib" (
    copy /y "bin\Release\net10.0\osx-x64\publish\libLaplace.EventBridge.LuaWrapper.dylib" "%OUTPUT_DIR%\osx\x64\" >nul
    echo ✅ macOS x64 DYLIB 已复制
)

if exist "bin\Release\net10.0\osx-arm64\publish\libLaplace.EventBridge.LuaWrapper.dylib" (
    copy /y "bin\Release\net10.0\osx-arm64\publish\libLaplace.EventBridge.LuaWrapper.dylib" "%OUTPUT_DIR%\osx\arm64\" >nul
    echo ✅ macOS arm64 DYLIB 已复制
)

rem 复制 Lua 文件
copy /y "laplace_event_bridge.lua" "%OUTPUT_DIR%\" >nul
copy /y "example.lua" "%OUTPUT_DIR%\" >nul
echo ✅ Lua 文件已复制
echo.

echo [4/4] 生成文件清单...
echo.
echo 输出目录: %cd%\%OUTPUT_DIR%
echo.
echo ========== Windows 平台 ==========
echo windows\
echo   ├─ x64\
if exist "%OUTPUT_DIR%\windows\x64\Laplace.EventBridge.LuaWrapper.dll" (
    for %%F in ("%OUTPUT_DIR%\windows\x64\Laplace.EventBridge.LuaWrapper.dll") do echo   │   └─ Laplace.EventBridge.LuaWrapper.dll (%%~zF bytes^)
) else (
    echo   │   └─ (未生成^)
)
echo   ├─ x86\
if exist "%OUTPUT_DIR%\windows\x86\Laplace.EventBridge.LuaWrapper.dll" (
    for %%F in ("%OUTPUT_DIR%\windows\x86\Laplace.EventBridge.LuaWrapper.dll") do echo   │   └─ Laplace.EventBridge.LuaWrapper.dll (%%~zF bytes^)
) else (
    echo   │   └─ (未生成^)
)
echo   └─ arm64\
if exist "%OUTPUT_DIR%\windows\arm64\Laplace.EventBridge.LuaWrapper.dll" (
    for %%F in ("%OUTPUT_DIR%\windows\arm64\Laplace.EventBridge.LuaWrapper.dll") do echo       └─ Laplace.EventBridge.LuaWrapper.dll (%%~zF bytes^)
) else (
    echo       └─ (未生成^)
)
echo.
echo ========== Linux 平台 ==========
echo linux\
echo   ├─ x64\
if exist "%OUTPUT_DIR%\linux\x64\libLaplace.EventBridge.LuaWrapper.so" (
    for %%F in ("%OUTPUT_DIR%\linux\x64\libLaplace.EventBridge.LuaWrapper.so") do echo   │   └─ libLaplace.EventBridge.LuaWrapper.so (%%~zF bytes^)
) else (
    echo   │   └─ (未生成^)
)
echo   └─ arm64\
if exist "%OUTPUT_DIR%\linux\arm64\libLaplace.EventBridge.LuaWrapper.so" (
    for %%F in ("%OUTPUT_DIR%\linux\arm64\libLaplace.EventBridge.LuaWrapper.so") do echo       └─ libLaplace.EventBridge.LuaWrapper.so (%%~zF bytes^)
) else (
    echo       └─ (未生成^)
)
echo.
echo ========== macOS 平台 ==========
echo osx\
echo   ├─ x64\
if exist "%OUTPUT_DIR%\osx\x64\libLaplace.EventBridge.LuaWrapper.dylib" (
    for %%F in ("%OUTPUT_DIR%\osx\x64\libLaplace.EventBridge.LuaWrapper.dylib") do echo   │   └─ libLaplace.EventBridge.LuaWrapper.dylib (%%~zF bytes^)
) else (
    echo   │   └─ (未生成^)
)
echo   └─ arm64\
if exist "%OUTPUT_DIR%\osx\arm64\libLaplace.EventBridge.LuaWrapper.dylib" (
    for %%F in ("%OUTPUT_DIR%\osx\arm64\libLaplace.EventBridge.LuaWrapper.dylib") do echo       └─ libLaplace.EventBridge.LuaWrapper.dylib (%%~zF bytes^)
) else (
    echo       └─ (未生成^)
)
echo.
echo ========== Lua 文件 ==========
echo laplace_event_bridge.lua
echo example.lua
echo.

echo ====================================
echo  发布完成！
echo ====================================
echo.
echo 下一步操作:
echo 1. 将整个 %OUTPUT_DIR% 目录复制到你的 Lua 项目
echo 2. 确保保持目录结构 (windows/linux/osx 及其子目录)
echo 3. Lua 脚本会自动根据系统平台和架构加载对应的库文件
echo 4. 参考 example.lua 使用
echo.

pause
