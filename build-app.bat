@echo off
setlocal

set PROJECT=GoldShopWpf\GoldShopWpf.csproj

echo Building GoldShop WPF app...
dotnet build "%PROJECT%" -c Release -p:Platform=x64
if errorlevel 1 (
    echo.
    echo Build failed.
    exit /b 1
)

echo.
echo Build completed successfully.
echo Output:
echo %~dp0GoldShopWpf\bin\Release\net8.0-windows\

endlocal
