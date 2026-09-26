<#
.SYNOPSIS
    Builds the AH-64 mod and produces the Thunderstore release zip.

.DESCRIPTION
    Assembles dist/AH64-<version>.zip from three sources:
      - AH64.dll                 built here by dotnet
      - AssetBundles/ah64        built by Unity, mirrored in by this script
      - Build/                   manifest.json, README.md, icon.png (in git), plus the root CHANGELOG.md

    The assetbundle is NOT built by this script. Unity owns it. Build it first
    with AH64/Build AssetBundle (Ctrl+Alt+B) in the editor, or this script will
    tell you the bundle is missing or stale.

    Everything is validated before zipping, because every one of these failures
    is silent or near-silent at the other end:
      - plugin version != manifest version  -> lobby rejection for players
      - icon not exactly 256x256            -> Thunderstore rejects the upload
      - missing manifest/README/icon        -> Thunderstore rejects the upload
      - a wrapping folder inside the zip    -> Thunderstore rejects the upload

.PARAMETER SkipBuild
    Skip dotnet build and use the DLL already in Build/plugins.

.PARAMETER AllowStaleBundle
    Continue even if the Unity bundle is older than the newest Unity asset.
#>
[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [switch]$AllowStaleBundle
)

$ErrorActionPreference = 'Stop'

$RepoRoot    = Split-Path -Parent $PSScriptRoot
$Csproj      = Join-Path $RepoRoot 'AH64Mod\AH64.csproj'
$PluginCs    = Join-Path $RepoRoot 'AH64Mod\AH64Plugin.cs'
$BuildDir    = Join-Path $RepoRoot 'Build'
$Manifest    = Join-Path $BuildDir 'manifest.json'
$Icon        = Join-Path $BuildDir 'icon.png'
$Readme      = Join-Path $BuildDir 'README.md'
# Repo-root changelog, shipped at the zip root so Thunderstore's Changelog tab is populated.
$Changelog   = Join-Path $RepoRoot 'CHANGELOG.md'
$UnityBundle = Join-Path $RepoRoot 'AH64UnityProject\AssetBundles\ah64'
$StageDir    = Join-Path $RepoRoot 'dist\_stage'
$DistDir     = Join-Path $RepoRoot 'dist'

function Fail([string]$msg) { Write-Host "FAIL  $msg" -ForegroundColor Red; exit 1 }
function Ok  ([string]$msg) { Write-Host "  ok  $msg" -ForegroundColor Green }

Write-Host "`nAH-64 release packager" -ForegroundColor Cyan
Write-Host ("-" * 60)

# --- 1. version parity -------------------------------------------------------
# NetworkCompatibility is EveryoneNeedSameModVersion. If these two disagree the
# mod still builds and still loads, and then silently refuses to join lobbies.
if (-not (Test-Path $PluginCs)) { Fail "AH64Plugin.cs not found at $PluginCs" }
if (-not (Test-Path $Manifest)) { Fail "manifest.json not found at $Manifest" }

$pluginSrc = Get-Content $PluginCs -Raw
if ($pluginSrc -notmatch 'MODVERSION\s*=\s*"([^"]+)"') { Fail "Could not read MODVERSION from AH64Plugin.cs" }
$modVersion = $Matches[1]

$manifestObj = Get-Content $Manifest -Raw | ConvertFrom-Json
$manifestVersion = $manifestObj.version_number

if ($modVersion -ne $manifestVersion) {
    Fail "Version mismatch: MODVERSION='$modVersion' but manifest.json='$manifestVersion'.`n      These must match or players get a lobby rejection."
}
# Thunderstore and BepInEx both want a plain 3-part System.Version.
if ($modVersion -notmatch '^\d+\.\d+\.\d+$') {
    Fail "Version '$modVersion' is not major.minor.patch. A pre-release suffix is not System.Version-parseable and BepInEx skips the plugin silently."
}
Ok "version $modVersion (plugin and manifest agree)"

# --- 2. store assets ---------------------------------------------------------
if (-not (Test-Path $Readme)) { Fail "Build/README.md missing - Thunderstore requires it at the zip root" }
if (-not (Test-Path $Icon))   { Fail "Build/icon.png missing - Thunderstore requires it at the zip root" }
if (-not (Test-Path $Changelog)) { Fail "CHANGELOG.md missing at the repo root" }

Add-Type -AssemblyName System.Drawing
$img = [System.Drawing.Image]::FromFile($Icon)
$iconW = $img.Width; $iconH = $img.Height
$img.Dispose()
if ($iconW -ne 256 -or $iconH -ne 256) { Fail "icon.png is ${iconW}x${iconH}; Thunderstore requires exactly 256x256" }
Ok "manifest.json, README.md, icon.png (256x256), CHANGELOG.md"

# --- 3. compile --------------------------------------------------------------
if (-not $SkipBuild) {
    Write-Host "`nbuilding..." -ForegroundColor Cyan
    & dotnet build $Csproj -c Release -v minimal /p:AH64DeployToProfiles=false
    if ($LASTEXITCODE -ne 0) { Fail "dotnet build failed" }
}
$dll = Join-Path $BuildDir 'plugins\AH64.dll'
if (-not (Test-Path $dll)) { Fail "AH64.dll not found at $dll" }
Ok "AH64.dll"

# --- 4. mirror the Unity bundle ---------------------------------------------
# Unity writes to AH64UnityProject/AssetBundles. Nothing else copies it into
# Build/, so without this step the zip ships whatever bundle was there last.
if (-not (Test-Path $UnityBundle)) {
    Fail "Unity assetbundle not found at $UnityBundle.`n      Open the Unity project and run AH64/Build AssetBundle (Ctrl+Alt+B) first."
}

# Only actual bundle inputs count: the tagged bundle folder, and the FBX source whose meshes
# ship as a dependency of the model prefabs. Editor tooling under Assets/Scripts is NOT an input -
# including it made every tweak to a builder script look like a stale bundle.
$bundleInputs = @(
    (Join-Path $RepoRoot 'AH64UnityProject\Assets\AH64\Bundle'),
    (Join-Path $RepoRoot 'AH64UnityProject\Assets\AH64\Source')
) | Where-Object { Test-Path $_ }

$newestAsset = Get-ChildItem $bundleInputs -Recurse -File |
               Where-Object { $_.Extension -ne '.meta' -and $_.FullName -notmatch '~\\' } |
               Sort-Object LastWriteTime -Descending | Select-Object -First 1
$bundleTime = (Get-Item $UnityBundle).LastWriteTime
if ($newestAsset -and $newestAsset.LastWriteTime -gt $bundleTime) {
    $msg = "Bundle is older than $($newestAsset.Name). Rebuild it in Unity."
    if ($AllowStaleBundle) { Write-Host "WARN  $msg" -ForegroundColor Yellow }
    else { Fail "$msg`n      Pass -AllowStaleBundle to package anyway." }
}

$bundleDest = Join-Path $BuildDir 'plugins\AssetBundles'
New-Item -ItemType Directory -Force -Path $bundleDest | Out-Null
Copy-Item $UnityBundle (Join-Path $bundleDest 'ah64') -Force
Ok ("assetbundle ah64 ({0:N2} MB)" -f ((Get-Item $UnityBundle).Length / 1MB))

# Validate the independently authored Wwise bank before staging. Copy only this
# bank: shipping our authoring Init.bnk would conflict with the game's hierarchy.
& (Join-Path $PSScriptRoot 'build-rotor-bank.ps1') -ValidateOnly
$rotorBank = Join-Path $RepoRoot 'Art\Wwise\AH64Audio\GeneratedSoundBanks\Windows\AH64Rotor.bnk'
$bankDest = Join-Path $BuildDir 'plugins\SoundBanks'
New-Item -ItemType Directory -Force -Path $bankDest | Out-Null
Copy-Item -LiteralPath $rotorBank -Destination (Join-Path $bankDest 'AH64Rotor.bnk') -Force
if (Get-ChildItem (Join-Path $BuildDir 'plugins') -Recurse -File -Filter Init.bnk) {
    Fail 'Build/plugins contains Init.bnk. Never ship a replacement game initialization bank.'
}

# --- 5. stage ----------------------------------------------------------------
# Thunderstore requires manifest/README/icon/plugins at the ZIP ROOT with no
# enclosing directory, so stage an exact image of the zip and compress its
# contents rather than the folder itself.
if (Test-Path $StageDir) {
    if ((Resolve-Path -LiteralPath $StageDir).Path -ne [IO.Path]::GetFullPath((Join-Path $RepoRoot 'dist\_stage'))) { Fail 'Unexpected staging path' }
    Remove-Item -LiteralPath $StageDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $StageDir | Out-Null

Copy-Item $Manifest $StageDir
Copy-Item $Readme   $StageDir
Copy-Item $Icon     $StageDir
Copy-Item $Changelog $StageDir
Copy-Item (Join-Path $BuildDir 'plugins') $StageDir -Recurse

New-Item -ItemType Directory -Force -Path $DistDir | Out-Null
$zip = Join-Path $DistDir "AH64-$modVersion.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }

# Entries are added one at a time with explicitly forward-slashed names.
#
# Neither Compress-Archive nor ZipFile.CreateFromDirectory can be used here:
# both write BACKSLASH separators on .NET Framework, which is what Windows
# PowerShell runs on. That violates the ZIP spec (4.4.17.1 requires forward
# slashes), and extractors on other platforms then read "plugins\AH64.dll" as a
# single flat filename containing a backslash rather than a directory - so the
# DLL never lands in plugins/ and the mod silently does not load.
Add-Type -AssemblyName System.IO.Compression            # ZipArchive, ZipArchiveMode
Add-Type -AssemblyName System.IO.Compression.FileSystem # ZipFile, ZipFileExtensions
$archive = [System.IO.Compression.ZipFile]::Open($zip, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    $stageRoot = (Resolve-Path $StageDir).Path.TrimEnd('\') + '\'
    foreach ($file in Get-ChildItem $StageDir -Recurse -File) {
        $rel = $file.FullName.Substring($stageRoot.Length).Replace([char]0x5C, [char]0x2F)
        [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $archive, $file.FullName, $rel, [System.IO.Compression.CompressionLevel]::Optimal)
    }
} finally { $archive.Dispose() }

# --- 6. verify the produced zip ---------------------------------------------
$archive = [System.IO.Compression.ZipFile]::OpenRead($zip)
try {
    $entries = $archive.Entries | ForEach-Object { $_.FullName }
} finally { $archive.Dispose() }

foreach ($required in @('manifest.json', 'README.md', 'icon.png', 'CHANGELOG.md',
                        'plugins/AH64.dll', 'plugins/AssetBundles/ah64', 'plugins/SoundBanks/AH64Rotor.bnk')) {
    if ($entries -notcontains $required) {
        Fail "zip is missing '$required'.`n      Entries found: $($entries -join ', ')"
    }
}
# A backslash here means the archiver wrote non-conformant separators.
if ($entries | Where-Object { $_ -like '*\*' }) { Fail "zip contains backslash path separators" }

if ((Resolve-Path -LiteralPath $StageDir).Path -ne [IO.Path]::GetFullPath((Join-Path $RepoRoot 'dist\_stage'))) { Fail 'Unexpected staging path' }
Remove-Item -LiteralPath $StageDir -Recurse -Force

Write-Host ("-" * 60)
$zipMB = (Get-Item $zip).Length / 1MB
Ok ("{0}  ({1:N2} MB)" -f [System.IO.Path]::GetFileName($zip), $zipMB)
Write-Host "`n$zip`n" -ForegroundColor Cyan
Write-Host "Contents:" -ForegroundColor Cyan
$entries | Sort-Object | ForEach-Object { "    $_" }
Write-Host "`nNot uploaded. Publish to Thunderstore manually when you are ready.`n"
