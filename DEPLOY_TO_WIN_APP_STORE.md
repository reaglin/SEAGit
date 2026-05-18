# Deploying SEAGit to the Microsoft Store

Step-by-step guide for building the MSIX package and publishing new versions of
SEAGit to the **Microsoft Store** through Partner Center.

SEAGit is distributed three ways, independently of each other:

| Channel | Artifact | Built by |
|---|---|---|
| Microsoft Store | unsigned `.msixupload` | `build-msix.ps1 -Mode Store` |
| Direct download (sideload) | signed `.msix` | `build-msix.ps1 -Mode Sideload` |
| Direct download (classic) | installer on GitHub Releases | existing release process |

This guide covers the **Microsoft Store** channel. Companion tooling and notes
live in `msix/` (`build-msix.ps1`, `README.md`).

---

## 1. Tools used

| Tool | Purpose | Source |
|---|---|---|
| Visual Studio 2022 | Builds the app and the packaging project | visualstudio.microsoft.com |
| · ".NET desktop development" workload | Builds SEAGit (.NET 8 WinForms) | VS Installer |
| · Windows Application Packaging support | `.wapproj` build / MSIX | VS Installer (see §3.2) |
| .NET 8 SDK | App build/publish | Ships with VS |
| MSBuild | Command-line package build | Ships with VS |
| PowerShell 5.1+ | Runs `build-msix.ps1` | Built into Windows |
| Partner Center | Store account and submissions | partner.microsoft.com |
| Windows App Certification Kit (optional) | Local pre-submission validation | Windows SDK (`appcert.exe`) |

---

## 2. Store identity

Reserved in Partner Center and baked into `WapProjTemplate/Package.appxmanifest`.
**Do not change these** — they must match the Partner Center registration.

| Field | Value |
|---|---|
| Package/Identity/Name | `DeanEaglin.SEAGit` |
| Package/Identity/Publisher | `CN=E88392BA-A722-4B3A-8372-04403A55AA63` |
| Publisher display name | `Dean Eaglin` |

> Always sign in to Partner Center with the **personal Microsoft account** that
> owns the developer registration. If a work account auto-signs, use an
> InPrivate browser window or a separate browser profile.

---

## 3. One-time setup

Skip to §4 if your machine is already set up.

### 3.1 Partner Center
- A Microsoft Store developer account (identity verified) at partner.microsoft.com.
- The app name **SEAGit** reserved. Partner Center → SEAGit →
  *Product management → Product identity* lists the values in §2.

### 3.2 Visual Studio components
VS Installer → Modify → install:
- The **".NET desktop development"** workload.
- **Windows Application Packaging Project** support — confirm that *Windows
  Application Packaging Project* appears under **Add → New Project**. Depending
  on the VS version this comes from the "Universal Windows Platform
  development" workload or is otherwise available as a component.

### 3.3 Packaging project (already in the repository)
`WapProjTemplate/Seagit.Package.wapproj` is the Windows Application Packaging
Project; it is part of `SEAGit.sln` and references the `SEAGit` app project. It
is already configured:
- `SEAGit/SEAGit.csproj` — `<RuntimeIdentifiers>win-x64</RuntimeIdentifiers>`.
- `WapProjTemplate/Seagit.Package.wapproj` — `<SelfContained>true</SelfContained>`
  and `<RuntimeIdentifier>win-x64</RuntimeIdentifier>`. The package carries the
  .NET runtime, so end users need no .NET prerequisite.
- `WapProjTemplate/Package.appxmanifest` — the §2 identity, the `runFullTrust`
  and `internetClient` capabilities, and the visual assets in
  `WapProjTemplate/Images/`.

(The packaging project's folder is named `WapProjTemplate` — the Visual Studio
template default. This is cosmetic; package identity comes from the manifest.)

### 3.4 Signing certificate (only for the optional sideload build)
The signed sideload package uses a self-signed code-signing certificate:
- `msix/SEAGit-SelfSigned.cer` — public cert (committed).
- `msix/SEAGit-SelfSigned.pfx` — private key, **git-ignored, never commit or
  share**. Expires May 2029. Its password is **not stored in the repo** —
  supply it via the `SEAGIT_CERT_PASSWORD` environment variable.

To recreate it, see `msix/README.md`.

---

## 4. Release a new version — step by step

### Step 1 — Bump the version
Edit `WapProjTemplate/Package.appxmanifest`:
```xml
<Identity Name="DeanEaglin.SEAGit"
          Publisher="CN=E88392BA-A722-4B3A-8372-04403A55AA63"
          Version="X.Y.Z.0" />
```
The version is 4-part; the **final digit must be `0`** (reserved by the Store),
and the version must be **higher than the last one submitted**.

### Step 2 — Build the Store package
Close any running SEAGit instance, then from a PowerShell prompt:
```powershell
cd msix
.\build-msix.ps1 -Mode Store
```
This produces:
```
WapProjTemplate\AppPackages\Store\Seagit.Package_<version>_x64_bundle.msixupload
```

> Build with the script, **not** Visual Studio's "Create App Packages" wizard —
> the wizard fails to restore the self-contained publish for this project. The
> script runs MSBuild with `/restore`, which works.

### Step 3 — (Optional) Validate locally with WACK
The Microsoft Store certifies the package on submission, so this is optional.
To check first, run the **Windows App Cert Kit** GUI from the Start Menu, or
`appcert.exe` from an Administrator prompt, against the `.msixbundle` in
`WapProjTemplate\AppPackages\Sideload\Seagit.Package_<version>_x64_Test\`.

### Step 4 — Upload the package to Partner Center
1. partner.microsoft.com → **SEAGit → Submissions** → start a new submission
   (or *Update* for an existing app).
2. Open the **Packages** section and upload the
   `Seagit.Package_<version>_x64_bundle.msixupload` file.
3. Do **not** sign the package yourself — the Store signs it under your identity.

### Step 5 — Complete the submission listing
For the **first** submission, fill in all required sections:
- **Properties** — category (Developer tools is a good fit).
- **Age ratings** — complete the questionnaire.
- **Pricing and availability** — markets and price. Setting the price to
  **Free** avoids the payout/tax setup; you can switch to paid later.
- **Store listing** — description, at least one screenshot.
- **Privacy policy URL** — **required**, because SEAGit makes network calls.
  Host the text of `PRIVACY_POLICY.txt` at a public URL and enter it here.

### Step 6 — Restricted capability justification
SEAGit declares `runFullTrust`. Partner Center asks why; this is routine for
any packaged desktop app. A suitable justification:

> SEAGit is a full-trust Win32 desktop application built with .NET 8 and
> Windows Forms, packaged as MSIX. The runFullTrust capability is required for
> any packaged classic desktop application to run. SEAGit uses it for standard
> desktop behavior: invoking the locally installed Git command-line tool,
> reading and writing the user's selected folders, and making HTTPS requests
> to GitHub.

### Step 7 — Submit
Submit for certification. Certification typically completes within a few hours
to a day; Partner Center emails you the result.

### Step 8 — (Optional) Signed sideload build
For direct distribution outside the Store:
```powershell
cd msix
.\build-msix.ps1 -Mode Sideload
```
Produces a **signed** `.msix`. Recipients install `SEAGit-SelfSigned.cer` into
**Local Machine → Trusted People** once, then double-click the `.msix`.

---

## 5. Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `project.assets.json not found` under `obj\wappublish\...` | Visual Studio's packaging wizard skips the per-RID restore. Use `build-msix.ps1` (MSBuild with `/restore`). |
| `APPX0702 Payload file ... does not exist` | The `.wapproj` lists an image file that is not present. The asset list uses a wildcard (`Images\*.png`); regenerate assets or check the `Images\` folder. |
| `APPX3207 ... image ... larger than 204,800 bytes` | A manifest-referenced logo exceeds the size limit. Shrink it or drop the optional tile reference from `Package.appxmanifest`. |
| Partner Center shows an access/permission error | Signed in with the wrong Microsoft account. Use the personal account that owns the developer registration. |
| Build cannot overwrite `SEAGit.exe` | A copy of SEAGit is running. Close it and rebuild. |

---

## 6. Privacy policy

The Microsoft Store requires a public **privacy policy URL** for any app that
accesses the network. The policy text is in `PRIVACY_POLICY.txt` in the
repository root. Before submitting, host it at a public URL and enter that URL
in the submission. Keep the hosted copy and `PRIVACY_POLICY.txt` in sync.

---

## 7. Path reference

| Item | Path |
|---|---|
| Packaging project | `WapProjTemplate/Seagit.Package.wapproj` |
| Package manifest | `WapProjTemplate/Package.appxmanifest` |
| Visual assets | `WapProjTemplate/Images/` |
| Build script | `msix/build-msix.ps1` |
| In-repo MSIX notes | `msix/README.md` |
| Build output | `WapProjTemplate/AppPackages/Store/` and `/Sideload/` |
| Signing certificate | `msix/SEAGit-SelfSigned.{cer,pfx}` |
| Privacy policy text | `PRIVACY_POLICY.txt` |
