@echo off
chcp 65001 >nul
echo ====================================
echo  Laplace Event Bridge Lua Wrapper
echo  Native AOT 发布脚本
echo ====================================
echo.

cd /d "%~dp0"

echo [1/3] 清理旧文件...
if exist "bin\Release\net10.0\win-x64\publish" (
    rmdir /s /q "bin\Release\net10.0\win-x64\publish"
)
echo.

echo [2/3] 发布 Native AOT 版本（这可能需要几分钟）...
dotnet publish -c Release -r win-x64
if errorlevel 1 (
    echo.
    echo ❌ 发布失败！请检查错误信息。
    pause
    exit /b 1
)
echo.

echo [3/3] 验证 DLL...
if exist "bin\Release\net10.0\win-x64\publish\Laplace.EventBridge.LuaWrapper.dll" (
    echo ✅ DLL 生成成功！
    echo.
    echo 文件位置:
    echo %cd%\bin\Release\net10.0\win-x64\publish\Laplace.EventBridge.LuaWrapper.dll
    echo.
    echo 文件大小:
    for %%F in ("bin\Release\net10.0\win-x64\publish\Laplace.EventBridge.LuaWrapper.dll") do echo %%~zF bytes
    echo.
) else (
    echo ❌ DLL 文件未生成！
    pause
    exit /b 1
)

echo ====================================
echo  发布完成！
echo ====================================
echo.
echo 下一步操作:
echo 1. 将 bin\Release\net10.0\win-x64\publish\Laplace.EventBridge.LuaWrapper.dll 复制到你的 Lua 项目
echo 2. 复制 laplace_event_bridge.lua 到你的 Lua 项目
echo 3. 参考 example.lua 使用
echo.

pause
