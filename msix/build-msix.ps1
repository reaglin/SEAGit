#Requires -Version 5.1
<#
.SYNOPSIS
    Builds the SEAGit MSIX package.

.DESCRIPTION
    SEAGit is packaged for MSIX by the Seagit.Package Windows Application
    Packaging Project (in WapProjTemplate\). Visual Studio's "Create App
    Packages" wizard fails to restore the self-contained publish for this kind
    of project, so packages are built from the command line via this script.

    Modes:
      Store     Unsigned .msixupload for Microsoft Store submission. Partner
                Center signs it on upload -- do NOT sign it yourself.
      Sideload  Signed .msix for direct install / testing. Signed with the
                self-signed certificate in this folder; the matching
                SEAGit-SelfSigned.cer must be trusted on the target machine.

.PARAMETER Mode
    Store (default) or Sideload.

.PARAMETER CertPath
    Path to the signing .pfx. Sideload mode only.

.PARAMETER CertPassword
    Password for the signing .pfx. Sideload mode only.

.EXAMPLE
    .\build-msix.ps1 -Mode Store

.EXAMPLE
    .\build-msix.ps1 -Mode Sideload
#>
[CmdletBinding()]
param(
    [ValidateSet('Store', 'Sideload')]
    [string] $Mode = 'Store',

    [string] $CertPath     = (Join-Path $PSScriptRoot 'SEAGit-SelfSigned.pfx'),
    [string] $CertPassword = 'SEAGit-Dev-Sign'
)

$ErrorActionPreference = 'Stop'

# -- Paths --------------------------------------------------------------------
$repo    = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$wapproj = Join-Path $repo 'WapProjTemplate\Seagit.Package.wapproj'
$pkgDir  = Join-Path $repo 'WapProjTemplate\AppPackages\'

if (-not (Test-Path $wapproj)) { throw "Packaging project not found: $wapproj" }

# -- Locate MSBuild -----------------------------------------------------------
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path $vswhere)) { throw 'vswhere.exe not found -- is Visual Studio installed?' }
$vsPath  = & $vswhere -latest -property installationPath
$msbuild = Join-Path $vsPath 'MSBuild\Current\Bin\MSBuild.exe'
if (-not (Test-Path $msbuild)) { throw "MSBuild not found: $msbuild" }

# -- Build arguments ----------------------------------------------------------
# /restore is required: it performs the per-RID restore that the VS wizard skips.
$buildArgs = @(
    $wapproj
    '/restore'
    '/t:Rebuild'
    '/p:Configuration=Release'
    '/p:Platform=x64'
    '/p:GenerateAppxPackageOnBuild=true'
    "/p:AppxPackageDir=$pkgDir"
    '/v:minimal'
    '/nologo'
)

if ($Mode -eq 'Store') {
    Write-Host 'Building UNSIGNED Store package (.msixupload)...' -ForegroundColor Cyan
    $buildArgs += @(
        '/p:UapAppxPackageBuildMode=StoreUpload'
        '/p:AppxBundle=Always'
        '/p:AppxBundlePlatforms=x64'
    )
}
else {
    if (-not (Test-Path $CertPath)) {
        throw "Signing certificate not found: $CertPath  (see README.md to recreate it)"
    }
    Write-Host 'Building SIGNED sideload package (.msix)...' -ForegroundColor Cyan
    $buildArgs += @(
        '/p:AppxBundle=Never'
        '/p:AppxPackageSigningEnabled=true'
        "/p:PackageCertificateKeyFile=$CertPath"
        "/p:PackageCertificatePassword=$CertPassword"
    )
}

# -- Build --------------------------------------------------------------------
& $msbuild $buildArgs
if ($LASTEXITCODE -ne 0) { throw "MSBuild failed (exit code $LASTEXITCODE)." }

# -- Report -------------------------------------------------------------------
Write-Host "`nBuild succeeded. Recent package output:" -ForegroundColor Green
Get-ChildItem -LiteralPath (Join-Path $repo 'WapProjTemplate\AppPackages') `
              -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Extension -in '.msix', '.msixbundle', '.msixupload' } |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 6 |
    ForEach-Object { '  {0,8:N1} MB   {1}' -f ($_.Length / 1MB), $_.FullName }
