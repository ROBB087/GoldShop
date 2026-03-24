@echo off
setlocal

set PROJECT=GoldShopWpf\GoldShopWpf.csproj

echo Building GoldShop WPF app...
dotnet build "%PROJECT%" -c Release
if errorlevel 1 (
    echo.
    echo Build failed.
    exit /b 1
)

echo.
echo Build completed successfully.
echo Output:
echo %~dp0GoldShopWpf\bin\Release\net10.0-windows\

endlocal
