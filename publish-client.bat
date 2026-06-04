@echo off
setlocal

set PROJECT=GoldShopWpf\GoldShopWpf.csproj
set RUNTIME=win-x64
set OUTDIR=%~dp0publish\client\GoldShop

if exist "%OUTDIR%" rmdir /S /Q "%OUTDIR%"
if not exist "%~dp0publish\client" mkdir "%~dp0publish\client"

echo Restoring client package dependencies...
dotnet restore "%PROJECT%" -r %RUNTIME% -p:Platform=x64
if errorlevel 1 (
    echo.
    echo Client restore failed.
    exit /b 1
)

echo Building client package...
dotnet build "%PROJECT%" -c Release -r %RUNTIME% --self-contained true --nologo --no-restore -p:Platform=x64
if errorlevel 1 (
    echo.
    echo Client build failed.
    exit /b 1
)

echo Publishing client package...
dotnet publish "%PROJECT%" -c Release -r %RUNTIME% --self-contained true --nologo --no-restore --no-build -p:Platform=x64 -p:PublishSingleFile=false -p:PublishReadyToRun=false -p:DebugType=None -p:DebugSymbols=false -o "%OUTDIR%"
if errorlevel 1 (
    echo.
    echo Client publish failed.
    exit /b 1
)

copy /Y "%~dp0CLIENT-DELIVERY.txt" "%OUTDIR%\README.txt" >nul

if exist "%OUTDIR%\Data" (
    echo Runtime data folder was copied into the publish output, which is not allowed.
    exit /b 1
)

if exist "%OUTDIR%\Backups" (
    echo Backup data folder was copied into the publish output, which is not allowed.
    exit /b 1
)

if exist "%OUTDIR%\Logs" (
    echo Log data folder was copied into the publish output, which is not allowed.
    exit /b 1
)

if exist "%OUTDIR%\Security" (
    echo Security data folder was copied into the publish output, which is not allowed.
    exit /b 1
)

echo.
echo Client package completed successfully.
echo Deliver this folder to the client:
echo %OUTDIR%
echo.
echo Main file:
echo %OUTDIR%\GoldShop.exe

endlocal
