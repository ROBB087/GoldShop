# GoldShop Delivery Guide

## Client Package

Publish a client-only package with:

```powershell
.\publish-client.bat
```

Deliver only this folder to the client:

```text
publish\client\GoldShop
```

Do not deliver `GoldShopLicenseTool` to the client.

## Activation Workflow

On the client machine:

1. Run `GoldShopWpf.exe`
2. Copy the `Machine ID` from the activation window
3. Send that ID to you

On your machine:

```powershell
.\generate-license.bat MACHINE-ID "Abo Emad"
```

Or directly:

```powershell
dotnet run --project GoldShopLicenseTool -- "MACHINE-ID" "Abo Emad"
```

Send the generated key back to the client.

## Security Note

`GoldShopLicenseTool` contains the private signing key and must remain owner-only.

If this repository is public or shared, you should rotate the key pair and keep the private key outside the client repository. The current setup is suitable only if the private key never leaves your control after this point.

## Optional Obfuscation

To obfuscate the core business assembly before release:

```powershell
.\obfuscate-app.bat
```

This currently targets `GoldShopCore.dll` only, which is safer for WPF apps than obfuscating the whole UI assembly.
