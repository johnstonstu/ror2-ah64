[CmdletBinding()]
param(
    [string]$Dll, [string]$Bundle, [string]$Bank, [string]$Config,
    [string]$DependencyLock, [string]$AccessResult, [string]$AssetProvenance,
    [string]$ProfileName = 'AH64 1.3 Dev',
    [string]$GameDirectory = 'C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2',
    [string]$RuntimeReservation, [string]$RuntimeOwner, [string]$RuntimeLockPath, [switch]$AllowDirty
)
$ErrorActionPreference = 'Stop'

function Get-Identity([string]$Path) {
    if (!(Test-Path -LiteralPath $Path -PathType Leaf)) { throw "Required file missing: $Path" }
    $file = Get-Item -LiteralPath $Path
    if ($file.Length -eq 0) { throw "Required file empty: $Path" }
    [pscustomobject]@{ path = $file.FullName; sha256 = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash; bytes = $file.Length }
}

function Get-TreeIdentity([string]$Root, [string]$Filter) {
    if (!(Test-Path -LiteralPath $Root -PathType Container)) { throw "Required directory missing: $Root" }
    @(Get-ChildItem -LiteralPath $Root -Recurse -File -Filter $Filter | Sort-Object FullName | ForEach-Object { Get-Identity $_.FullName })
}

function Get-ProfilePath([string]$Name) {
    # Intentionally one disposable profile. Renaming the production profile is not authorization.
    if ($Name -ne 'AH64 1.3 Dev') { throw 'Only the reserved AH64 1.3 Dev profile is accepted.' }
    $path = Join-Path $env:APPDATA ('r2modmanPlus-local/RiskOfRain2/profiles/' + $Name)
    $marker = Join-Path $path '.ah64-disposable-test-profile'
    if (!(Test-Path -LiteralPath $marker -PathType Leaf) -or (Get-Content -LiteralPath $marker -Raw).Trim() -ne 'AH64 1.3 Dev') {
        throw 'Profile must be explicitly prepared and marked disposable by the runtime owner.'
    }
    if ((Get-Item -LiteralPath $path).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Disposable profile may not be a junction/symlink.' }
    (Resolve-Path -LiteralPath $path).Path
}

function Assert-ProfileTarget([string]$Profile, [string]$Target) {
    $resolved = [IO.Path]::GetFullPath($Target)
    if (!$resolved.StartsWith($Profile + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Target escaped disposable profile.' }
    for ($current = $resolved; $current.Length -gt $Profile.Length; $current = Split-Path -Parent $current) {
        if ((Test-Path -LiteralPath $current) -and ((Get-Item -LiteralPath $current).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw "Refusing linked target: $current" }
    }
}

function Assert-NoGame {
    if (Get-Process -Name 'Risk of Rain 2' -ErrorAction SilentlyContinue) { throw 'Game is running; close it before this operation.' }
}

function New-RuntimeLease([string]$Path, [string]$Owner, [string]$Token, [string]$Profile, [string]$Sha) {
    if (!$Owner -or !$Token -or !$Path -or ![IO.Path]::IsPathRooted($Path)) { throw 'Explicit canonical lock path, owner and run token required.' }
    if ([IO.Path]::GetFileName($Path) -ne 'runtime.lock' -or !(Test-Path -LiteralPath (Split-Path -Parent $Path) -PathType Container)) { throw 'Integrator must prepare the canonical coordination directory.' }
    $stream = [IO.File]::Open($Path, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try {
        $lease = @{ schema = 1; owner = $Owner; runToken = $Token; resource = 'RoR2-stage-and-solo'; profile = $Profile; sourceSha = $Sha; createdUtc = [DateTime]::UtcNow.ToString('o'); processIds = @() }
        $bytes = [Text.Encoding]::UTF8.GetBytes(($lease | ConvertTo-Json))
        $stream.Write($bytes, 0, $bytes.Length); $stream.Flush()
    } finally { $stream.Dispose() }
}

function Assert-RuntimeLease([string]$Path, [string]$Owner, [string]$Token) {
    # Existing unreadable/partly written files are occupied. No time-based stale takeover.
    $lease = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    if ($lease.schema -ne 1 -or $lease.owner -cne $Owner -or $lease.runToken -cne $Token) { throw 'Runtime lease belongs to another owner/run.' }
    $lease
}

function Remove-RuntimeLease([string]$Path, [string]$Owner, [string]$Token) {
    Assert-NoGame
    Assert-RuntimeLease $Path $Owner $Token | Out-Null
    Remove-Item -LiteralPath $Path
}

function Assert-Identity($Identity) {
    $now = Get-Identity $Identity.path
    if ($now.sha256 -ne $Identity.sha256 -or $now.bytes -ne $Identity.bytes) { throw "Identity changed: $($Identity.path)" }
}

function Get-CandidateSnapshot([string]$Repo) {
    $sha = (& git -C $Repo rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Cannot identify source SHA.' }
    $dirty = @(& git -C $Repo status --porcelain=v1 --untracked-files=all)
    if ($LASTEXITCODE -ne 0) { throw 'Cannot identify dirty status.' }
    $diff = @(& git -C $Repo diff --binary HEAD)
    $untracked = @(& git -C $Repo -c core.quotepath=false ls-files --others --exclude-standard | ForEach-Object {
        if ($_ -match '^"') { throw 'Quoted/unusual untracked path requires manual provenance review.' }; Get-Identity (Join-Path $Repo $_)
    })
    [ordered]@{ sourceSha = $sha; status = $dirty; diff = $diff; untracked = $untracked }
}

function Assert-DependencyLock($Lock, $Manifest, [string]$Profile) {
    foreach ($dependency in $Manifest.dependencies) {
        $entries = @($Lock.packages | Where-Object { $_.package -ceq $dependency })
        if ($entries.Count -ne 1) { throw "Dependency lock must identify exactly once: $dependency" }
    }
    foreach ($entry in $Lock.packages) {
        if (@($entry.directories).Count -eq 0) { throw "No directories for $($entry.package)" }
        foreach ($relative in $entry.directories) {
            $directory = [IO.Path]::GetFullPath((Join-Path $Profile $relative))
            if (!$directory.StartsWith($Profile + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Dependency escaped profile.' }
            $files = @(Get-TreeIdentity $directory '*.dll')
            if ($files.Count -eq 0) { throw "No DLLs in declared dependency directory: $directory" }
        }
    }
}

function Assert-AssetProvenance($Provenance, [string]$RecordPath, [string]$BundlePath, [string]$BankPath, [string]$Repo) {
    if ($Provenance.SchemaVersion -ne 1 -or $Provenance.BankReadOnlyValidation -ne 'passed' -or $Provenance.ExpectedUnity -ne '2021.3.33f1') { throw 'Reviewed baseline asset provenance/validation required.' }
    $inputManifest = Join-Path (Split-Path -Parent $RecordPath) $Provenance.SourceInputHashManifest
    if ((Get-Identity $inputManifest).sha256 -ne $Provenance.SourceInputHashManifestSHA256) { throw 'Source input manifest hash mismatch.' }
    foreach ($scope in @('AH64UnityProject','Art/Wwise/AH64Audio','Art/Audio')) {
        if (@($Provenance.SourceTrees | Where-Object Scope -CEQ $scope).Count -ne 1) { throw "Missing/duplicate asset source scope: $scope" }
    }
    foreach ($tree in $Provenance.SourceTrees) {
        $actual = (& git -C $Repo rev-parse ('HEAD:' + $tree.Scope)).Trim()
        if ($LASTEXITCODE -ne 0 -or $actual -ne $tree.Tree) { throw "Generated asset source tree changed: $($tree.Scope)" }
        & git -C $Repo diff --quiet HEAD -- $tree.Scope
        if ($LASTEXITCODE -ne 0) { throw "Uncommitted generated asset input changed: $($tree.Scope)" }
        if (@(& git -C $Repo ls-files --others --exclude-standard -- $tree.Scope).Count -gt 0) { throw "Untracked generated asset input: $($tree.Scope)" }
    }
    if (@($Provenance.SourceTrees).Count -lt 3) { throw 'Incomplete asset source tree provenance.' }
    foreach ($pair in @(@{ name = 'ah64'; path = $BundlePath }, @{ name = 'AH64Rotor.bnk'; path = $BankPath })) {
        $entry = @($Provenance.Artifacts | Where-Object SnapshotFile -CEQ $pair.name)
        $identity = Get-Identity $pair.path
        if ($entry.Count -ne 1 -or $entry[0].SHA256 -ne $identity.sha256 -or $entry[0].Bytes -ne $identity.bytes) { throw "Verified artifact identity mismatch: $($pair.name)" }
    }
    $license = Get-Identity (Join-Path $Repo 'AH64UnityProject/Assets/AH64/Bundle/AH64Audio/LICENSE_SOURCE.txt')
    $entry = @($Provenance.Artifacts | Where-Object SnapshotFile -CEQ 'LICENSE_SOURCE.txt')
    if ($entry.Count -ne 1 -or $entry[0].SHA256 -ne $license.sha256) { throw 'Audio source license identity mismatch.' }
    $license
}

if ($MyInvocation.InvocationName -eq '.') { return }
foreach ($required in @('Dll','Bundle','Bank','Config','DependencyLock','AccessResult','AssetProvenance','RuntimeReservation','RuntimeOwner','RuntimeLockPath')) {
    if (!(Get-Variable $required -ValueOnly)) { throw "Supply -$required explicitly; no generated assets or runtime reservation are inferred." }
}
Assert-NoGame
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$profile = Get-ProfilePath $ProfileName
$plugin = Join-Path $profile 'BepInEx/plugins/JohnstonStu-AH64'
if (!(Test-Path -LiteralPath $plugin -PathType Container)) { throw 'Install the candidate mod/dependencies into the disposable profile first.' }
$lock = Get-Content -LiteralPath $DependencyLock -Raw | ConvertFrom-Json
$manifestPath = Join-Path $repo 'Build/manifest.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$versionMatch = [regex]::Match((Get-Content -LiteralPath (Join-Path $repo 'AH64Mod/AH64Plugin.cs') -Raw), 'public const string MODVERSION\s*=\s*"([0-9.]+)"')
if (!$versionMatch.Success -or $versionMatch.Groups[1].Value -ne $manifest.version_number) { throw 'Source/manifest version mismatch or nonnumeric plugin version.' }
[version]::Parse($manifest.version_number) | Out-Null
Assert-DependencyLock $lock $manifest $profile
$snapshot = Get-CandidateSnapshot $repo
$sha = $snapshot.sourceSha; $dirty = $snapshot.status
if ($dirty.Count -gt 0 -and !$AllowDirty) { throw 'Candidate is dirty; commit it or explicitly request -AllowDirty.' }
$builtPath = Join-Path $repo 'AH64Mod/bin/Release/netstandard2.1/AH64.dll'
if ([IO.Path]::GetFullPath($Dll) -ne [IO.Path]::GetFullPath($builtPath)) { throw 'DLL must be this candidate worktree build output; arbitrary binaries cannot claim its source SHA.' }
$buildLog = Join-Path $repo ('dist/stage-build-' + [guid]::NewGuid().ToString('N') + '.log')
New-Item -ItemType Directory -Path (Split-Path -Parent $buildLog) -Force | Out-Null
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
& dotnet build (Join-Path $repo 'AH64Mod/AH64.csproj') -c Release --no-restore /p:AH64DeployToProfiles=false /p:UseSharedCompilation=false /nodeReuse:false *> $buildLog
if ($LASTEXITCODE -ne 0) { throw "Candidate build failed; nothing staged. $buildLog" }
if (($snapshot | ConvertTo-Json -Depth 6 -Compress) -cne ((Get-CandidateSnapshot $repo) | ConvertTo-Json -Depth 6 -Compress)) { throw 'Source changed during compilation; nothing staged.' }
$access = Get-Content -LiteralPath $AccessResult -Raw | ConvertFrom-Json
$dllIdentity = Get-Identity $Dll
if ($access.status -ne 'passed' -or $access.checkedMembers -le 0 -or @($access.unresolved).Count -gt 0 -or @($access.inaccessible).Count -gt 0 -or $access.candidateSha256 -ne $dllIdentity.sha256) {
    throw 'Access scan must pass for these exact DLL bytes, with no unresolved references.'
}
$managed = Join-Path $GameDirectory 'Risk of Rain 2_Data/Managed'
if ([IO.Path]::GetFullPath($access.gameManaged) -ne [IO.Path]::GetFullPath($managed)) { throw 'Access scan game path mismatch.' }
foreach ($assembly in $access.assemblies.PSObject.Properties) {
    if ((Get-Identity $assembly.Name).sha256 -ne $assembly.Value) { throw "Scanned assembly changed: $($assembly.Name)" }
}
$provenance = Get-Content -LiteralPath $AssetProvenance -Raw | ConvertFrom-Json
$license = Assert-AssetProvenance $provenance $AssetProvenance $Bundle $Bank $repo
if ([IO.Path]::GetFileName($Bank) -cne 'AH64Rotor.bnk') { throw 'Only AH64Rotor.bnk is accepted; no Init.bnk.' }
$inputs = @(
    @{ source = Get-Identity $Dll; target = Join-Path $plugin 'AH64.dll' },
    @{ source = Get-Identity $Bundle; target = Join-Path $plugin 'AssetBundles/ah64' },
    @{ source = Get-Identity $Bank; target = Join-Path $plugin 'SoundBanks/AH64Rotor.bnk' },
    @{ source = $license; target = Join-Path $plugin 'SoundBanks/LICENSE_SOURCE.txt' },
    @{ source = Get-Identity $manifestPath; target = Join-Path $plugin 'manifest.json' },
    @{ source = Get-Identity $Config; target = Join-Path $profile 'BepInEx/config/com.JohnstonStu.AH64.cfg' }
)
foreach ($input in $inputs) { Assert-ProfileTarget $profile $input.target }
$support = @('core','plugins','patchers') | ForEach-Object { Get-TreeIdentity (Join-Path $profile "BepInEx/$_") '*.dll' }
$game = @(Get-Identity (Join-Path $GameDirectory 'Risk of Rain 2.exe')) + @(Get-TreeIdentity $managed '*.dll')
$configBefore = @(Get-TreeIdentity (Join-Path $profile 'BepInEx/config') '*.cfg')
if (Get-ChildItem -LiteralPath $plugin -Recurse -File -Filter Init.bnk) { throw 'Existing plugin contains forbidden Init.bnk; integrator must inspect it.' }
$run = Join-Path $repo ('dist/autopilot/' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path (Join-Path $run 'backup') -Force | Out-Null
$record = [ordered]@{
    schema = 1; status = 'preparing'; reservation = $RuntimeReservation; owner = $RuntimeOwner; runtimeLockPath = $RuntimeLockPath; runDirectory = $run;
    sourceSha = $sha; dirty = $dirty; pluginVersion = $manifest.version_number; profile = $profile; profileName = $ProfileName;
    gameDirectory = $GameDirectory; game = $game; inputs = $inputs;
    dependencies = $lock; assetProvenance = $provenance; buildEvidence = Get-Identity $buildLog; supportBefore = @($support); configBefore = $configBefore; replacements = @()
}
$recordPath = Join-Path $run 'stage.json'
Copy-Item -LiteralPath $DependencyLock -Destination (Join-Path $run 'dependencies.json')
Copy-Item -LiteralPath $AccessResult -Destination (Join-Path $run 'access.json')
$snapshot | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $run 'dirty.json')
$record.dirtyWorkspaceFingerprint = (Get-Identity (Join-Path $run 'dirty.json')).sha256
if (($snapshot | ConvertTo-Json -Depth 6 -Compress) -cne ((Get-CandidateSnapshot $repo) | ConvertTo-Json -Depth 6 -Compress)) { throw 'Source changed before staging; nothing replaced.' }
New-RuntimeLease $RuntimeLockPath $RuntimeOwner $RuntimeReservation $profile $sha
try {
    # Back up every target before replacing any target. Record absent originals for manual rollback.
    $index = 0
    foreach ($input in $inputs) {
        $backup = Join-Path $run ('backup/' + $index++)
        $previous = $null
        if (Test-Path -LiteralPath $input.target -PathType Leaf) {
            $previous = Get-Identity $input.target
            Copy-Item -LiteralPath $input.target -Destination $backup
        }
        $record.replacements += @{ target = $input.target; previous = $previous; backup = $backup; installed = $null; attempted = $false }
    }
    foreach ($relative in @('mods.yml','BepInEx/LogOutput.log')) {
        $existing = Join-Path $profile $relative
        if (Test-Path -LiteralPath $existing -PathType Leaf) { Copy-Item -LiteralPath $existing -Destination (Join-Path $run ('backup/' + [IO.Path]::GetFileName($relative))) }
    }
    $record | ConvertTo-Json -Depth 15 | Set-Content -LiteralPath $recordPath
    Assert-NoGame
    for ($i = 0; $i -lt $inputs.Count; $i++) {
        Assert-Identity $inputs[$i].source
        $record.replacements[$i].attempted = $true
        New-Item -ItemType Directory -Path (Split-Path -Parent $inputs[$i].target) -Force | Out-Null
        Copy-Item -LiteralPath $inputs[$i].source.path -Destination $inputs[$i].target -Force
        $record.replacements[$i].installed = Get-Identity $inputs[$i].target
        if ($record.replacements[$i].installed.sha256 -ne $inputs[$i].source.sha256) { throw 'Staged copy hash mismatch.' }
    }
    $record.support = @(@('core','plugins','patchers') | ForEach-Object { Get-TreeIdentity (Join-Path $profile "BepInEx/$_") '*.dll' })
    $record.config = @(Get-TreeIdentity (Join-Path $profile 'BepInEx/config') '*.cfg')
    $record.status = 'staged'
} catch {
    $record.status = 'failed'; $record.failure = $_.Exception.Message
    # Restore only this explicit replacement allowlist; retain all backup/evidence files.
    try {
        Assert-NoGame
        foreach ($replacement in $record.replacements) {
            if ($replacement.previous) {
                if ((Get-Identity $replacement.backup).sha256 -ne $replacement.previous.sha256) { throw 'Rollback backup changed.' }
                Copy-Item -LiteralPath $replacement.backup -Destination $replacement.target -Force
                Assert-Identity $replacement.previous
            } elseif ($replacement.attempted -and (Test-Path -LiteralPath $replacement.target -PathType Leaf)) { Remove-Item -LiteralPath $replacement.target }
        }
        $record.rollback = 'restored'
        Remove-RuntimeLease $RuntimeLockPath $RuntimeOwner $RuntimeReservation
    } catch { $record.rollback = 'blocked: ' + $_.Exception.Message }
    throw
} finally { $record | ConvertTo-Json -Depth 15 | Set-Content -LiteralPath $recordPath }
Write-Output "Staged one disposable profile. Record/rollback map: $recordPath"
