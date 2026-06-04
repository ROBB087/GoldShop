@echo off
setlocal

set "INSTALL_DIR=%LocalAppData%\Programs\GoldShop"
set "START_MENU_DIR=%AppData%\Microsoft\Windows\Start Menu\Programs\GoldShop"
set "DESKTOP_SHORTCUT=%USERPROFILE%\Desktop\GoldShop.lnk"
set "UNINSTALL_KEY=HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\GoldShop"

echo Uninstalling GoldShop from:
echo   %INSTALL_DIR%

taskkill /IM GoldShop.exe /F >nul 2>&1

if exist "%DESKTOP_SHORTCUT%" del /F /Q "%DESKTOP_SHORTCUT%" >nul 2>&1
if exist "%START_MENU_DIR%\GoldShop.lnk" del /F /Q "%START_MENU_DIR%\GoldShop.lnk" >nul 2>&1
if exist "%START_MENU_DIR%\Uninstall GoldShop.lnk" del /F /Q "%START_MENU_DIR%\Uninstall GoldShop.lnk" >nul 2>&1
if exist "%START_MENU_DIR%" rmdir "%START_MENU_DIR%" >nul 2>&1

reg delete "%UNINSTALL_KEY%" /f >nul 2>&1

if exist "%INSTALL_DIR%" (
    powershell -NoProfile -ExecutionPolicy Bypass -Command "if (Test-Path '%INSTALL_DIR%') { Get-ChildItem -LiteralPath '%INSTALL_DIR%' -Force | Remove-Item -Recurse -Force }"
    if errorlevel 1 (
        echo Uninstall failed while removing application files.
        exit /b 1
    )

    rmdir "%INSTALL_DIR%" >nul 2>&1
)

echo GoldShop has been removed. Local application data in %%LocalAppData%%\GoldShop was preserved.
exit /b 0
