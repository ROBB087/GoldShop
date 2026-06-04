#ifndef AppName
  #define AppName "GoldShop"
#endif
#ifndef AppPublisher
  #define AppPublisher "GoldShop"
#endif
#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef SourceDir
  #error SourceDir must be defined when compiling the installer.
#endif
#ifndef OutputDir
  #error OutputDir must be defined when compiling the installer.
#endif
#ifndef OutputBaseFilename
  #define OutputBaseFilename "GoldShop-Setup"
#endif
#ifndef AppExeName
  #define AppExeName "GoldShop.exe"
#endif
#ifndef SetupIconFile
  #define SetupIconFile "Resources\appicon.ico"
#endif

[Setup]
AppId={{A246FF3A-7D8B-4F44-812F-80FEEEA9BF7E}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#OutputDir}
OutputBaseFilename={#OutputBaseFilename}
SetupIconFile={#SetupIconFile}
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
CloseApplicationsFilter={#AppExeName}
RestartApplications=no
VersionInfoVersion={#AppVersion}
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} Setup
VersionInfoProductName={#AppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[InstallDelete]
Type: files; Name: "{app}\GoldShopWpf.exe"
Type: files; Name: "{app}\GoldShopWpf.dll"
Type: files; Name: "{app}\GoldShopWpf.deps.json"
Type: files; Name: "{app}\GoldShopWpf.runtimeconfig.json"
Type: files; Name: "{app}\GoldShopWpf.runtimeconfig.dev.json"
Type: files; Name: "{app}\uninstall.cmd"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.db,*.db-wal,*.db-shm,*.db-journal,*.sqlite,*.sqlite3,*.bak,*.backup"

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autoprograms}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent
