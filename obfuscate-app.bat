@echo off
setlocal

set PROJECT=GoldShopCore\GoldShopCore.csproj
set CONFIG=Obfuscar.xml

echo Building GoldShop core in Release mode...
dotnet build "%PROJECT%" -c Release
if errorlevel 1 (
    echo.
    echo Release build failed.
    exit /b 1
)

where obfuscar.console >nul 2>nul
if errorlevel 1 (
    echo.
    echo Obfuscar.Console is not installed or not on PATH.
    echo Install it, then run this file again:
    echo   dotnet tool install --global Obfuscar.GlobalTool
    echo.
    echo Config file prepared:
    echo   %~dp0%CONFIG%
    exit /b 1
)

echo.
echo Obfuscating GoldShopCore.dll...
obfuscar.console "%CONFIG%"
if errorlevel 1 (
    echo.
    echo Obfuscation failed.
    exit /b 1
)

echo.
echo Obfuscation completed.
echo Output:
echo %~dp0publish\obfuscated-core\

endlocal
