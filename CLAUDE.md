# SEAGit — Claude Code Guide

## What This Project Is

SEAGit (Super-Easy and Accessible Git) is a **.NET 8 WinForms desktop app for Windows** that lets non-technical users publish a local folder to GitHub with one click — no branching, staging, or command line. It drives the user's installed **Git for Windows** in the background; it does not bundle git.

See `README.md` for the end-user walkthrough.

## Solution Structure

```
SEAGit.sln
├── SEAGit/                — WinForms app (net8.0-windows)
│   ├── MainForm.cs        — the single application window
│   ├── Models/            — GitRepository (a tracked local folder + its remote)
│   ├── Services/
│   │   ├── GitProcessService.cs   — runs git.exe and captures output
│   │   └── StorageService.cs      — persists the user's repo list
│   └── Program.cs         — entry point
└── WapProjTemplate/       — Windows Application Packaging Project (MSIX)
```

MSIX packaging and Store-deploy tooling live in `msix/`; the runbook is `DEPLOY_TO_WIN_APP_STORE.md`.

## Build & Run

Open `SEAGit.sln` in **Visual Studio 2022**, or:

```bash
dotnet build SEAGit.sln
dotnet run --project SEAGit/SEAGit.csproj
```

No test project exists — verification is manual through the UI. SEAGit requires Git for Windows to be installed on the machine.

## Architecture Notes

- **No server, no database.** The only persisted state is the user's tracked-folder list, written locally by `StorageService`.
- **All git work shells out** to the installed `git.exe` via `GitProcessService`. SEAGit does not implement git itself.
- **GitHub authentication is delegated to Git Credential Manager** (the browser sign-in flow). SEAGit never stores GitHub tokens or passwords itself — keep it that way.

## Repository Hygiene — Keep Secrets Out of Git

Treat anything that grants access or proves identity as a secret, and keep it out of version control.

- **Never commit:** GitHub personal access tokens, passwords, OAuth client secrets, or code-signing `.pfx` / `.p12` private keys.
- **GitHub auth is delegated to Git Credential Manager.** SEAGit must never write tokens or credentials to disk or into the repo — do not add code that does.
- **MSIX code signing:** the signing certificate's private `.pfx` is git-ignored; only the public `.cer` may be committed. The `.pfx` password **must** be supplied through an environment variable (a `*_CERT_PASSWORD` variable) — never hard-coded in `build-msix.ps1`, written into a doc, or committed.
- **`.gitignore` must cover** build outputs (`bin/`, `obj/`, `AppPackages/`, `BundleArtifacts/`, `*.msix*`) and `*.pfx`.
- **Before committing** new scripts or docs, scan them for embedded credentials.
- **If a secret is ever committed, rotate it** — regenerate the key or certificate so the exposed value becomes worthless. Rotation is preferred over rewriting git history.

## High-DPI Display Support

SEAGit can run on high-resolution laptops (for example 2880×1920 at 200% scale). WinForms does **not** scale correctly by default — windows and controls can render too small or get clipped.

- Confirm the app is per-monitor DPI aware: `Program.cs` should call `Application.SetHighDpiMode(HighDpiMode.PerMonitorV2)` (or the app manifest should declare it).
- If forms or controls render too small on a high-DPI display, apply the `DpiAwareForm` / `DpiAwareUserControl` pattern: a base class that establishes the 96-DPI baseline and scales in `OnLoad`, after the controls have been added.
- Lay out forms at the 96-DPI design baseline. Pixel `Size`/`Location` values need scaling — point-based fonts already render at the correct physical size at any DPI.
- The full rationale and a reusable drop-in recipe are documented in `DISPLAY_FIX.md` in the sibling **CIATLE** repository.
- **Always test on a high-DPI display above 100% scale before release.**
