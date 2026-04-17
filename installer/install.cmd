@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "INSTALL_DIR=%LocalAppData%\Programs\GoldShop"
set "START_MENU_DIR=%AppData%\Microsoft\Windows\Start Menu\Programs\GoldShop"
set "DESKTOP_SHORTCUT=%USERPROFILE%\Desktop\GoldShop.lnk"
set "APP_ARCHIVE=%SCRIPT_DIR%app.zip"

echo Installing GoldShop to:
echo   %INSTALL_DIR%

if exist "%INSTALL_DIR%\GoldShopWpf.exe" (
    taskkill /IM GoldShopWpf.exe /F >nul 2>&1
)

if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

powershell -NoProfile -ExecutionPolicy Bypass -Command "if (Test-Path '%INSTALL_DIR%') { Get-ChildItem -LiteralPath '%INSTALL_DIR%' -Force | Remove-Item -Recurse -Force }; Expand-Archive -LiteralPath '%APP_ARCHIVE%' -DestinationPath '%INSTALL_DIR%' -Force"
if errorlevel 1 (
    echo Installation failed while extracting application files.
    exit /b 1
)

if not exist "%START_MENU_DIR%" mkdir "%START_MENU_DIR%"

powershell -NoProfile -ExecutionPolicy Bypass -Command "$shell = New-Object -ComObject WScript.Shell; $targets = @(@{Path='%START_MENU_DIR%\GoldShop.lnk'; Target='%INSTALL_DIR%\GoldShopWpf.exe'; Working='%INSTALL_DIR%'}, @{Path='%DESKTOP_SHORTCUT%'; Target='%INSTALL_DIR%\GoldShopWpf.exe'; Working='%INSTALL_DIR%'}); foreach($item in $targets){ $shortcut = $shell.CreateShortcut($item.Path); $shortcut.TargetPath = $item.Target; $shortcut.WorkingDirectory = $item.Working; $shortcut.IconLocation = $item.Target; $shortcut.Save(); }"
if errorlevel 1 (
    echo Installation failed while creating shortcuts.
    exit /b 1
)

echo GoldShop installed successfully.
start "" "%INSTALL_DIR%\GoldShopWpf.exe"
exit /b 0
