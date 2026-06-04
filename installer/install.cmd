@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "INSTALL_DIR=%LocalAppData%\Programs\GoldShop"
set "START_MENU_DIR=%AppData%\Microsoft\Windows\Start Menu\Programs\GoldShop"
set "DESKTOP_SHORTCUT=%USERPROFILE%\Desktop\GoldShop.lnk"
set "APP_ARCHIVE=%SCRIPT_DIR%app.zip"
set "UNINSTALL_CMD=%INSTALL_DIR%\uninstall.cmd"
set "UNINSTALL_KEY=HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\GoldShop"
set "DISPLAY_VERSION=__VERSION__"

echo Installing GoldShop to:
echo   %INSTALL_DIR%

if exist "%INSTALL_DIR%\GoldShop.exe" (
    taskkill /IM GoldShop.exe /F >nul 2>&1
)

if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

powershell -NoProfile -ExecutionPolicy Bypass -Command "if (Test-Path '%INSTALL_DIR%') { Get-ChildItem -LiteralPath '%INSTALL_DIR%' -Force | Remove-Item -Recurse -Force }; Expand-Archive -LiteralPath '%APP_ARCHIVE%' -DestinationPath '%INSTALL_DIR%' -Force"
if errorlevel 1 (
    echo Installation failed while extracting application files.
    exit /b 1
)

copy /Y "%SCRIPT_DIR%uninstall.cmd" "%UNINSTALL_CMD%" >nul
if errorlevel 1 (
    echo Installation failed while preparing uninstall support.
    exit /b 1
)

if not exist "%START_MENU_DIR%" mkdir "%START_MENU_DIR%"

powershell -NoProfile -ExecutionPolicy Bypass -Command "$shell = New-Object -ComObject WScript.Shell; $targets = @(@{Path='%START_MENU_DIR%\GoldShop.lnk'; Target='%INSTALL_DIR%\GoldShop.exe'; Working='%INSTALL_DIR%'}, @{Path='%DESKTOP_SHORTCUT%'; Target='%INSTALL_DIR%\GoldShop.exe'; Working='%INSTALL_DIR%'}, @{Path='%START_MENU_DIR%\Uninstall GoldShop.lnk'; Target='%ComSpec%'; Arguments='/c ""%UNINSTALL_CMD%""'; Working='%INSTALL_DIR%'}); foreach($item in $targets){ $shortcut = $shell.CreateShortcut($item.Path); $shortcut.TargetPath = $item.Target; if($item.ContainsKey('Arguments')){ $shortcut.Arguments = $item.Arguments }; $shortcut.WorkingDirectory = $item.Working; $shortcut.IconLocation = '%INSTALL_DIR%\GoldShop.exe'; $shortcut.Save(); }"
if errorlevel 1 (
    echo Installation failed while creating shortcuts.
    exit /b 1
)

reg add "%UNINSTALL_KEY%" /v "DisplayName" /t REG_SZ /d "GoldShop" /f >nul
reg add "%UNINSTALL_KEY%" /v "DisplayVersion" /t REG_SZ /d "%DISPLAY_VERSION%" /f >nul
reg add "%UNINSTALL_KEY%" /v "Publisher" /t REG_SZ /d "GoldShop" /f >nul
reg add "%UNINSTALL_KEY%" /v "InstallLocation" /t REG_SZ /d "%INSTALL_DIR%" /f >nul
reg add "%UNINSTALL_KEY%" /v "DisplayIcon" /t REG_SZ /d "%INSTALL_DIR%\GoldShop.exe" /f >nul
reg add "%UNINSTALL_KEY%" /v "UninstallString" /t REG_SZ /d "\"%UNINSTALL_CMD%\"" /f >nul
reg add "%UNINSTALL_KEY%" /v "QuietUninstallString" /t REG_SZ /d "\"%UNINSTALL_CMD%\"" /f >nul
reg add "%UNINSTALL_KEY%" /v "NoModify" /t REG_DWORD /d 1 /f >nul
reg add "%UNINSTALL_KEY%" /v "NoRepair" /t REG_DWORD /d 1 /f >nul

echo GoldShop installed successfully.
start "" "%INSTALL_DIR%\GoldShop.exe"
exit /b 0
