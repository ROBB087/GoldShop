$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root "GoldShopWpf\GoldShopWpf.csproj"
$publishRoot = Join-Path $root "publish"
$clientDir = Join-Path $publishRoot "client\GoldShop"
$installerRoot = Join-Path $publishRoot "installer"
$installerScript = Join-Path $root "installer\GoldShop.iss"
$readmeSource = Join-Path $root "CLIENT-DELIVERY.txt"
$versionProps = Join-Path $root "Directory.Build.props"
$setupIcon = Join-Path $root "GoldShopWpf\Resources\appicon.ico"
$releaseBinDir = Join-Path $root "GoldShopWpf\bin\Release"
$releaseObjDir = Join-Path $root "GoldShopWpf\obj\Release"
$coreReleaseBinDir = Join-Path $root "GoldShopCore\bin\Release"
$coreReleaseObjDir = Join-Path $root "GoldShopCore\obj\Release"
$isccCandidates = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    (Join-Path $env:LocalAppData "Programs\Inno Setup 6\ISCC.exe")
)

[Environment]::SetEnvironmentVariable("DOTNET_CLI_HOME", (Join-Path $root ".dotnet-cli"), "Process")
[Environment]::SetEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "1", "Process")
[Environment]::SetEnvironmentVariable("DOTNET_NOLOGO", "1", "Process")

[xml]$propsXml = Get-Content $versionProps
$version = ($propsXml.Project.PropertyGroup | Where-Object { $_.VersionPrefix } | Select-Object -First 1).VersionPrefix
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "Unable to read VersionPrefix from $versionProps."
}

$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($iscc)) {
    throw "Inno Setup compiler (ISCC.exe) was not found. Install Inno Setup 6 first."
}

if (Test-Path $publishRoot) {
    Remove-Item -LiteralPath $publishRoot -Recurse -Force
}

foreach ($path in @($releaseBinDir, $releaseObjDir, $coreReleaseBinDir, $coreReleaseObjDir)) {
    if (Test-Path $path) {
        Remove-Item -LiteralPath $path -Recurse -Force
    }
}

New-Item -ItemType Directory -Path $clientDir -Force | Out-Null
New-Item -ItemType Directory -Path $installerRoot -Force | Out-Null

Write-Host "Restoring GoldShop dependencies..."
dotnet restore $project -r win-x64 -p:Platform=x64
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }

Write-Host "Building GoldShop release build..."
dotnet build $project -c Release -r win-x64 --self-contained true --nologo --no-restore -p:Platform=x64
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed." }

Write-Host "Publishing GoldShop release build..."
dotnet publish $project -c Release -r win-x64 --self-contained true `
    --nologo `
    --no-restore `
    --no-build `
    -p:Platform=x64 `
    -p:PublishSingleFile=false `
    -p:PublishReadyToRun=false `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $clientDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

Copy-Item -LiteralPath $readmeSource -Destination (Join-Path $clientDir "README.txt") -Force

$forbiddenDataFiles = Get-ChildItem -Path $clientDir -Recurse -File | Where-Object {
    $_.Name -match '\.db($|-)|\.db-wal$|\.db-shm$|\.db-journal$|\.sqlite$|\.sqlite3$|\.bak$|\.backup$'
}
if ($forbiddenDataFiles) {
    $artifactList = ($forbiddenDataFiles | ForEach-Object FullName) -join [Environment]::NewLine
    throw "Publish output contains forbidden database or backup artifacts, which is not allowed:`n$artifactList"
}

$runtimeDataDirectories = @("Data", "Backups", "Logs", "Security") |
    ForEach-Object { Join-Path $clientDir $_ } |
    Where-Object { Test-Path $_ }
if ($runtimeDataDirectories) {
    $directoryList = $runtimeDataDirectories -join [Environment]::NewLine
    throw "Publish output contains runtime data directories, which is not allowed:`n$directoryList"
}

$forbiddenPayloadFiles = Get-ChildItem -Path $clientDir -Recurse -File | Where-Object {
    $_.Name -in @("GoldShop.runtimeconfig.dev.json", "GoldShopWpf.runtimeconfig.dev.json")
}
if ($forbiddenPayloadFiles) {
    $fileList = ($forbiddenPayloadFiles | ForEach-Object FullName) -join [Environment]::NewLine
    throw "Publish output contains development-only payload files, which is not allowed:`n$fileList"
}

$expectedExecutable = Join-Path $clientDir "GoldShop.exe"
if (-not (Test-Path $expectedExecutable)) {
    throw "Expected published executable was not found at $expectedExecutable."
}

$outputBaseFilename = "GoldShop-Setup-v$version"

Write-Host "Building Inno Setup installer..."
& $iscc `
    "/DAppName=GoldShop" `
    "/DAppPublisher=GoldShop" `
    "/DAppVersion=$version" `
    "/DSourceDir=$clientDir" `
    "/DOutputDir=$installerRoot" `
    "/DOutputBaseFilename=$outputBaseFilename" `
    "/DAppExeName=GoldShop.exe" `
    "/DSetupIconFile=$setupIcon" `
    $installerScript
if ($LASTEXITCODE -ne 0) { throw "Inno Setup compilation failed." }

Write-Host ""
Write-Host "Release build completed."
Write-Host "Published client folder: $clientDir"
Write-Host "Installer package: $(Join-Path $installerRoot ($outputBaseFilename + '.exe'))"
