@echo off
setlocal

if "%~1"=="" (
    echo Usage:
    echo   generate-license.bat MACHINE-ID "Licensed To"
    echo.
    echo Example:
    echo   generate-license.bat F006C-E35FE-37FF2-807A1-EB733 "Abo Emad"
    exit /b 1
)

set MACHINE_ID=%~1
set LICENSED_TO=%~2

if "%LICENSED_TO%"=="" set LICENSED_TO=Abo Emad

echo Generating license for:
echo   Machine ID : %MACHINE_ID%
echo   Licensed To: %LICENSED_TO%
echo.

dotnet run --project GoldShopLicenseTool -- "%MACHINE_ID%" "%LICENSED_TO%"
if errorlevel 1 (
    echo.
    echo License generation failed.
    exit /b 1
)

endlocal
