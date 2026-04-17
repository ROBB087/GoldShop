$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$solution = Join-Path $root "GoldShop.sln"
$project = Join-Path $root "GoldShopWpf\GoldShopWpf.csproj"
$publishRoot = Join-Path $root "publish"
$clientDir = Join-Path $publishRoot "client\GoldShop"
$installerRoot = Join-Path $publishRoot "installer"
$installerBuildDir = Join-Path $installerRoot "build"
$installerPath = Join-Path $installerRoot "GoldShop-Setup.exe"
$installerArchivePath = Join-Path $installerBuildDir "app.zip"
$readmeSource = Join-Path $root "CLIENT-DELIVERY.txt"
$installScriptSource = Join-Path $root "installer\install.cmd"
$versionProps = Join-Path $root "Directory.Build.props"
$projectsToClean = @(
    (Join-Path $root "GoldShopCore\GoldShopCore.csproj"),
    (Join-Path $root "GoldShopWpf\GoldShopWpf.csproj"),
    (Join-Path $root "GoldShopStressTool\GoldShopStressTool.csproj"),
    (Join-Path $root "GoldShopLicenseTool\GoldShopLicenseTool.csproj")
)
$projectsToBuild = @(
    (Join-Path $root "GoldShopCore\GoldShopCore.csproj"),
    (Join-Path $root "GoldShopStressTool\GoldShopStressTool.csproj"),
    (Join-Path $root "GoldShopLicenseTool\GoldShopLicenseTool.csproj"),
    (Join-Path $root "GoldShopWpf\GoldShopWpf.csproj")
)

[Environment]::SetEnvironmentVariable("DOTNET_CLI_HOME", (Join-Path $root ".dotnet-cli"), "Process")
[Environment]::SetEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "1", "Process")
[Environment]::SetEnvironmentVariable("DOTNET_NOLOGO", "1", "Process")

[xml]$propsXml = Get-Content $versionProps
$version = ($propsXml.Project.PropertyGroup | Where-Object { $_.VersionPrefix } | Select-Object -First 1).VersionPrefix
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "Unable to read VersionPrefix from $versionProps."
}

Write-Host "Cleaning solution in Release mode..."
foreach ($projectToClean in $projectsToClean) {
    dotnet clean $projectToClean -c Release
    if ($LASTEXITCODE -ne 0) { throw "dotnet clean failed for $projectToClean." }
}

Write-Host "Building solution in Release mode..."
foreach ($projectToBuild in $projectsToBuild) {
    dotnet build $projectToBuild -c Release -p:Platform=x64 --no-restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed for $projectToBuild." }
}

if (Test-Path $clientDir) {
    Remove-Item -LiteralPath $clientDir -Recurse -Force
}

Write-Host "Publishing client package..."
dotnet publish $project -c Release -r win-x64 --self-contained true `
    --no-restore `
    -p:PublishSingleFile=false `
    -p:PublishReadyToRun=false `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $clientDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

Copy-Item -LiteralPath $readmeSource -Destination (Join-Path $clientDir "README.txt") -Force

if (Test-Path $installerBuildDir) {
    Remove-Item -LiteralPath $installerBuildDir -Recurse -Force
}

New-Item -ItemType Directory -Path $installerRoot -Force | Out-Null
New-Item -ItemType Directory -Path $installerBuildDir -Force | Out-Null
Compress-Archive -Path (Join-Path $clientDir "*") -DestinationPath $installerArchivePath -CompressionLevel Optimal
Copy-Item -LiteralPath $installScriptSource -Destination (Join-Path $installerBuildDir "install.cmd") -Force

$sedPath = Join-Path $installerBuildDir "GoldShopInstaller.sed"

$sedLines = @(
    "[Version]",
    "Class=IEXPRESS",
    "SEDVersion=3",
    "",
    "[Options]",
    "PackagePurpose=InstallApp",
    "ShowInstallProgramWindow=0",
    "HideExtractAnimation=1",
    "UseLongFileName=1",
    "InsideCompressed=0",
    "CAB_FixedSize=0",
    "CAB_ResvCodeSigning=0",
    "RebootMode=N",
    "InstallPrompt=",
    "DisplayLicense=",
    "FinishMessage=GoldShop installation completed successfully.",
    "TargetName=$installerPath",
    "FriendlyName=GoldShop Setup $version",
    "AppLaunched=install.cmd",
    "PostInstallCmd=<None>",
    "AdminQuietInstCmd=install.cmd",
    "UserQuietInstCmd=install.cmd",
    "SourceFiles=SourceFiles",
    "",
    "[Strings]",
    "CompressionType=MSZIP",
    "InstallPrompt=",
    "FriendlyName=GoldShop Setup $version",
    "FinishMessage=GoldShop installation completed successfully.",
    "FILE0=""app.zip""",
    "FILE1=""install.cmd""",
    "",
    "[SourceFiles]",
    "SourceFiles0=$installerBuildDir",
    "",
    "[SourceFiles0]"
)

$sedLines += "%FILE0%="
$sedLines += "%FILE1%="
Set-Content -LiteralPath $sedPath -Value $sedLines -Encoding ASCII

Write-Host "Building installer package..."
& iexpress.exe /N $sedPath | Out-Null
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $installerPath)) {
    throw "IExpress installer creation failed."
}

Write-Host ""
Write-Host "Release build completed."
Write-Host "Published client folder: $clientDir"
Write-Host "Installer package: $installerPath"
