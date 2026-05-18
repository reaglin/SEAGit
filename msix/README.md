# SEAGit — MSIX / Microsoft Store packaging

This folder holds the MSIX release tooling for SEAGit. The packaging project
itself is `WapProjTemplate/Seagit.Package.wapproj` (a Windows Application
Packaging Project in `SEAGit.sln`); its manifest is
`WapProjTemplate/Package.appxmanifest`.

For the full release runbook, see `../DEPLOY_TO_WIN_APP_STORE.md`.

## Reserved Store identity

From Partner Center → SEAGit → **Product management → Product identity**,
baked into `WapProjTemplate/Package.appxmanifest`:

| Field | Value |
|---|---|
| Package/Identity/Name | `DeanEaglin.SEAGit` |
| Package/Identity/Publisher | `CN=E88392BA-A722-4B3A-8372-04403A55AA63` |
| Package/Properties/PublisherDisplayName | `Dean Eaglin` |

## Building packages

> **Build from the command line, not Visual Studio.** VS's *Create App
> Packages* wizard fails to restore the self-contained publish for this
> project. `build-msix.ps1` builds correctly via MSBuild with `/restore`.

```powershell
# From this folder (msix):

# Unsigned .msixupload for Microsoft Store submission:
.\build-msix.ps1 -Mode Store

# Signed .msix for direct install / testing:
.\build-msix.ps1 -Mode Sideload
```

Each mode writes to its own subfolder of `WapProjTemplate\AppPackages\`, so
Store and Sideload builds never overwrite each other:

- **Store:** `AppPackages\Store\Seagit.Package_<ver>_x64_bundle.msixupload` —
  upload this to Partner Center **unsigned**; the Store signs it.
- **Sideload:** `AppPackages\Sideload\Seagit.Package_<ver>_x64_Test\Seagit.Package_<ver>_x64.msix`
  — signed, directly installable.

## Project configuration (already applied — reference only)

- `SEAGit/SEAGit.csproj` — `<RuntimeIdentifiers>win-x64</RuntimeIdentifiers>`.
- `WapProjTemplate/Seagit.Package.wapproj` — `<SelfContained>true</SelfContained>`
  and `<RuntimeIdentifier>win-x64</RuntimeIdentifier>`, so the package carries
  the .NET runtime and needs no prerequisite. Its visual-asset list uses a
  wildcard (`Images\*.png`) so regenerating assets needs no project edit.
- `WapProjTemplate/Package.appxmanifest` — Store identity, `runFullTrust` +
  `internetClient` capabilities, token-based executable entry.
- `WapProjTemplate/Images/` — visual assets from the VS Asset Generator.

## Signing certificate (sideload only)

The signed sideload package uses a **self-signed** code-signing certificate
whose subject matches the manifest `Publisher`.

- `SEAGit-SelfSigned.cer` — public certificate (committed). Trust this to
  install the sideload package.
- `SEAGit-SelfSigned.pfx` — **private key. Git-ignored — never commit or share
  it.** Valid until **May 2029**. Its password is **not stored in the repo** —
  keep it in a password manager and supply it via the `SEAGIT_CERT_PASSWORD`
  environment variable when building.

To recreate the certificate (new machine, or after expiry), run from this
folder:

```powershell
# Choose a strong .pfx password and keep it out of the repository:
$env:SEAGIT_CERT_PASSWORD = '<choose a strong password>'

$cert = New-SelfSignedCertificate -Type CodeSigningCert `
  -Subject "CN=E88392BA-A722-4B3A-8372-04403A55AA63" `
  -KeyExportPolicy Exportable -CertStoreLocation "Cert:\CurrentUser\My" `
  -FriendlyName "SEAGit self-signed (sideload)" -NotAfter (Get-Date).AddYears(3)
$pw = ConvertTo-SecureString $env:SEAGIT_CERT_PASSWORD -AsPlainText -Force
Export-PfxCertificate -Cert "Cert:\CurrentUser\My\$($cert.Thumbprint)" -FilePath ".\SEAGit-SelfSigned.pfx" -Password $pw
Export-Certificate   -Cert "Cert:\CurrentUser\My\$($cert.Thumbprint)" -FilePath ".\SEAGit-SelfSigned.cer"
```

A self-signed certificate suits only your own / controlled test machines —
each must trust the `.cer` first. Public direct distribution needs a real CA
code-signing certificate.

## Installing the signed sideload package

1. Trust the certificate — once per machine, needs admin: right-click
   `SEAGit-SelfSigned.cer` → *Install Certificate* → **Local Machine** →
   place in **Trusted People**.
2. Double-click the `.msix` → *Install*. SEAGit appears in the Start Menu.

## Per-release checklist

1. Bump the version in `WapProjTemplate/Package.appxmanifest` →
   `Identity/@Version` (4-part, final digit `0`).
2. `.\build-msix.ps1 -Mode Store` → upload the `.msixupload` to Partner Center.
3. If distributing a signed build too: `.\build-msix.ps1 -Mode Sideload`.

## Notes

- The package is self-contained: it carries the .NET 8 runtime, so end users
  need no separate runtime install.
- SEAGit uses the separately installed **Git for Windows** at runtime; that is
  a user prerequisite, not something the MSIX bundles.
