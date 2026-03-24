@echo off
setlocal

set PROJECT=GoldShopWpf\GoldShopWpf.csproj
set RUNTIME=win-x64
set OUTDIR=%~dp0publish\GoldShopWpf

echo Publishing GoldShop WPF app...
dotnet publish "%PROJECT%" -c Release -r %RUNTIME% --self-contained true -p:PublishSingleFile=true -o "%OUTDIR%"
if errorlevel 1 (
    echo.
    echo Publish failed.
    exit /b 1
)

echo.
echo Publish completed successfully.
echo EXE:
echo %OUTDIR%\GoldShopWpf.exe

endlocal
