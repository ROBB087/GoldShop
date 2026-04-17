@echo off
setlocal

set PROJECT=GoldShopWpf\GoldShopWpf.csproj
set RUNTIME=win-x64
set OUTDIR=%~dp0publish\client\GoldShop

echo Publishing client package...
dotnet publish "%PROJECT%" -c Release -r %RUNTIME% --self-contained true -p:PublishSingleFile=false -p:PublishReadyToRun=false -p:DebugType=None -p:DebugSymbols=false -o "%OUTDIR%"
if errorlevel 1 (
    echo.
    echo Client publish failed.
    exit /b 1
)

copy /Y "%~dp0CLIENT-DELIVERY.txt" "%OUTDIR%\README.txt" >nul

echo.
echo Client package completed successfully.
echo Deliver this folder to the client:
echo %OUTDIR%
echo.
echo Main file:
echo %OUTDIR%\GoldShopWpf.exe

endlocal
