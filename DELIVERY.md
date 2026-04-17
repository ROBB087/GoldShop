# GoldShop Delivery Guide

## Client Package

Build the full production release with:

```powershell
.\build-release.ps1
```

This produces:

```text
publish\client\GoldShop
publish\installer\GoldShop-Setup.exe
```

Deliver `publish\installer\GoldShop-Setup.exe` to the client.

Do not deliver `GoldShopLicenseTool` or `GoldShopStressTool` to the client.

## Runtime Layout

The installed application stores writable data here:

```text
%LocalAppData%\GoldShop
```

That folder contains:

- `Data\goldshop.db`
- `Backups\`
- `Logs\system.log`
- `Logs\app-errors.log`
- `Security\license.bin`
- `Security\used-tokens.bin`

## Activation Workflow

On the client machine:

1. Run `GoldShopWpf.exe`
2. Copy the `Machine ID` from the activation window
3. Send that ID to you

Send the client one of the pre-approved offline activation tokens managed outside the application source.

## Security Note

Offline activation data must remain owner-only and should never be bundled with the client release package.

## Optional Obfuscation

To obfuscate the core business assembly before release:

```powershell
.\obfuscate-app.bat
```

This currently targets `GoldShopCore.dll` only, which is safer for WPF apps than obfuscating the whole UI assembly.
