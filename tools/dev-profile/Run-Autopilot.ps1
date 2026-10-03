[CmdletBinding()]
param([string]$StageRecord, [string]$RuntimeReservation, [string]$RuntimeOwner, [string]$RuntimeLockPath, [int]$TimeoutSeconds = 300, [switch]$ValidateOnly, [switch]$VisibleWindow, [string]$WindowCapturePython)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Stage-Build.ps1') -RuntimeReservation $RuntimeReservation -RuntimeOwner $RuntimeOwner -RuntimeLockPath $RuntimeLockPath

function Test-AutopilotEvidence([string]$Directory, [int]$ProcessExit, [bool]$TimedOut) {
    $problems = [Collections.Generic.List[string]]::new()
    $ids = @('roll.enter','roll.travel','roll.exit','roll.cleanup','backflip.enter','backflip.travel','backflip.exit','backflip.cleanup','hellfire.enter','hellfire.launch','hellfire.exit','hellfire.cleanup')
    if ($TimedOut) { $problems.Add('launcher timeout') }
    if ($ProcessExit -ne 0) { $problems.Add("process exit $ProcessExit") }
    $result = $null
    try { $result = Get-Content -LiteralPath (Join-Path $Directory 'result.json') -Raw | ConvertFrom-Json -ErrorAction Stop }
    catch { $problems.Add('missing/invalid terminal result.json') }
    if ($result) {
        if ($result.schema -ne 1 -or $result.suite -ne 'baseline-v1' -or $result.status -ne 'completed') { $problems.Add('wrong schema/suite or incomplete status') }
        if ($result.runId -ne [IO.Path]::GetFileName($Directory)) { $problems.Add('run identity mismatch') }
        if ($result.errors -ne 0 -or $result.warnings -ne 0 -or $result.visualFlags -ne 0) { $problems.Add('runtime errors, warnings or untriaged visual flags') }
        if ($result.expectedAssertions -ne $ids.Count -or $result.assertions -ne $ids.Count -or $result.failedAssertions -ne 0) { $problems.Add('assertion count/failures mismatch') }
        $observed = @($result.checks | ForEach-Object id)
        if ($observed.Count -ne $ids.Count -or @($observed | Select-Object -Unique).Count -ne $ids.Count -or @(Compare-Object $ids $observed).Count -ne 0) { $problems.Add('assertion IDs missing, extra or duplicated') }
        if (@($result.checks | Where-Object { $_.passed -ne $true }).Count -gt 0) { $problems.Add('failed assertion record') }
        if ($result.samples -lt 100) { $problems.Add('too few physics samples') }
        if ($result.captures -ne 5) { $problems.Add('five accepted visual checkpoints required') }
    }
    try { Test-WindowCaptureEvidence $Directory } catch { $problems.Add('invalid window capture evidence: ' + $_.Exception.Message) }
    foreach ($name in @('trace.txt','telemetry.jsonl','telemetry.csv','runtime.log','LogOutput.log','Player.log','identity.json')) {
        if (!(Test-Path -LiteralPath (Join-Path $Directory $name) -PathType Leaf) -or (Get-Item -LiteralPath (Join-Path $Directory $name)).Length -eq 0) { $problems.Add("missing/empty $name") }
    }
    try {
        $records = @(Get-Content -LiteralPath (Join-Path $Directory 'telemetry.jsonl') | ForEach-Object { $_ | ConvertFrom-Json -ErrorAction Stop })
        if (@($records | Where-Object { $_.schemaVersion -ne 1 -or $_.runId -ne [IO.Path]::GetFileName($Directory) }).Count -gt 0) { throw 'JSONL schema/run mismatch' }
        $headers = @($records | Where-Object recordType -EQ 'header')
        $summaries = @($records | Where-Object recordType -EQ 'summary')
        $assertions = @($records | Where-Object recordType -EQ 'assertion')
        $samples = @($records | Where-Object recordType -EQ 'sample')
        if ($headers.Count -ne 1 -or $summaries.Count -ne 1 -or $records[0].recordType -ne 'header' -or $records[-1].recordType -ne 'summary') { throw 'JSONL header/terminal summary missing or duplicated' }
        if ($summaries[0].status -ne 'completed' -or $summaries[0].skipped -ne 0 -or $summaries[0].executed -ne $ids.Count -or $summaries[0].failed -ne 0 -or $summaries[0].passed -ne $ids.Count) { throw 'JSONL summary counts/outcome mismatch' }
        foreach ($field in @('status','expectedAssertions','assertions','failedAssertions','errors','warnings','visualFlags','samples','suite')) {
            if (!$result -or $summaries[0].$field -ne $result.$field) { throw "Terminal JSON/JSONL disagree: $field" }
        }
        if (@($records | Where-Object recordType -EQ 'event').Count -eq 0) { throw 'No state/launch events' }
        if ($assertions.Count -ne $ids.Count -or @(Compare-Object $ids @($assertions | ForEach-Object assertionId)).Count -ne 0 -or @($assertions | Where-Object { $_.passed -ne $true }).Count -gt 0) { throw 'JSONL assertion contract mismatch' }
        if (!$result -or $samples.Count -ne $result.samples -or $summaries[0].samples -ne $result.samples) { throw 'JSONL sample count mismatch' }
        $identity = Get-Content -LiteralPath (Join-Path $Directory 'identity.json') -Raw | ConvertFrom-Json
        if ($headers[0].sourceSha -ne $identity.sourceSha -or $headers[0].dirtyWorkspaceFingerprint -ne $identity.dirtyWorkspaceFingerprint -or $headers[0].pluginVersion -ne $identity.pluginVersion -or $headers[0].requestedAssertionCount -ne $ids.Count -or $headers[0].profile -ne $identity.profile) { throw 'JSONL candidate identity mismatch' }
        foreach ($pair in @(@('dllSha256','AH64.dll'),@('bundleSha256','ah64'),@('bankSha256','AH64Rotor.bnk'),@('configSha256','com.JohnstonStu.AH64.cfg'))) {
            $installed = @($identity.replacements | Where-Object { [IO.Path]::GetFileName($_.target) -ceq $pair[1] })
            if ($installed.Count -ne 1 -or $headers[0].($pair[0]) -ne $installed[0].installed.sha256) { throw "JSONL artifact identity mismatch: $($pair[0])" }
        }
        $gameAssembly = @($identity.game | Where-Object { [IO.Path]::GetFileName($_.path) -ceq 'RoR2.dll' })
        if ($gameAssembly.Count -ne 1 -or $headers[0].gameBuild -ne $gameAssembly[0].sha256) { throw 'JSONL installed game identity mismatch' }
    } catch { $problems.Add('invalid JSONL evidence: ' + $_.Exception.Message) }
    $telemetry = Join-Path $Directory 'telemetry.csv'
    if (Test-Path -LiteralPath $telemetry) {
        $rows = @(Import-Csv -LiteralPath $telemetry)
        if ($result -and $rows.Count -ne $result.samples) { $problems.Add('telemetry count mismatch') }
        $previous = -1; $rowIndex = 0
        foreach ($row in $rows) {
            $tick = 0
            if (![int]::TryParse($row.tick, [ref]$tick) -or ($previous -ge 0 -and $tick -ne $previous + 1)) { $problems.Add('physics ticks missing/duplicated'); break }
            $previous = $tick
            if ($samples -and $rowIndex -lt $samples.Count -and $samples[$rowIndex].tick -ne $tick) { $problems.Add('CSV/JSONL physics ticks disagree'); break }
            $rowIndex++
            foreach ($field in @('time','px','py','pz','vx','vy','vz','fx','fy','fz','ax','ay','az','qx','qy','qz','qw')) {
                $number = 0.0
                if (![double]::TryParse($row.$field, [Globalization.NumberStyles]::Float, [Globalization.CultureInfo]::InvariantCulture, [ref]$number) -or [double]::IsNaN($number) -or [double]::IsInfinity($number)) { $problems.Add("invalid telemetry $field at tick $tick"); break }
            }
        }
    }
    $trace = Join-Path $Directory 'trace.txt'
    if ($result -and (Test-Path -LiteralPath $trace)) {
        $assertionLines = @(Get-Content -LiteralPath $trace | Where-Object { $_ -match '^ASSERT ' })
        if ($assertionLines.Count -ne $ids.Count) { $problems.Add('trace assertion count mismatch') }
    }
    # Scan entire archived logs, including startup/loading/teardown; no implicit warning exclusions.
    $logFindings = @()
    foreach ($name in @('LogOutput.log','Player.log','runtime.log')) {
        $path = Join-Path $Directory $name
        if (Test-Path -LiteralPath $path) {
            $logFindings += @(Select-String -LiteralPath $path -Pattern '(?i)\[(error|fatal|warning)\s*[:\]]|\b\w*Exception\b|^Error:|^Warning:|^Assert:' | ForEach-Object { "$name`:$($_.LineNumber): $($_.Line)" })
        }
    }
    if ($logFindings.Count -gt 0) { $problems.Add('full logs contain untriaged warnings/errors/exceptions') }
    [pscustomobject]@{ schema = 1; status = $(if ($problems.Count -eq 0) { 'passed' } else { 'failed' }); processExit = $ProcessExit; timedOut = $TimedOut; problems = @($problems); logFindings = $logFindings }
}

function Get-CaptureEvidenceTime($Value) {
    # PowerShell 7 may deserialize ISO JSON dates to DateTime; preserve ticks and UTC kind.
    if ($Value -is [DateTimeOffset]) { return $Value }
    if ($Value -is [DateTime]) { return [DateTimeOffset]$Value }
    return [DateTimeOffset]::Parse([string]$Value, [Globalization.CultureInfo]::InvariantCulture)
}

function Test-WindowCaptureEvidence([string]$Directory) {
    $names = @('roll-entered','roll-cleanup','backflip-entered','backflip-cleanup','hellfire-launched')
    $bridge = Join-Path $Directory 'window-captures'
    $requests = @(Get-ChildItem -LiteralPath $bridge -Filter '*.request.json' -ErrorAction Stop)
    $acks = @(Get-ChildItem -LiteralPath $bridge -Filter '*.ack.json' -ErrorAction Stop)
    $phases = @(Get-ChildItem -LiteralPath $bridge -Filter '*.phase.json' -ErrorAction Stop)
    if ($requests.Count -ne 5 -or $acks.Count -ne 5 -or $phases.Count -ne 5) { throw 'Missing/extra bridge records.' }
    $observed = @(); $hashes = @(); $processId = $null
    foreach ($file in $requests) {
        $request = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
        $ack = Get-Content -LiteralPath (Join-Path $bridge ($request.token + '.ack.json')) -Raw | ConvertFrom-Json
        $phase = Get-Content -LiteralPath (Join-Path $bridge ($request.token + '.phase.json')) -Raw | ConvertFrom-Json
        $observed += $request.name
        if ($request.token -notmatch '^[a-f0-9]{32}$' -or $file.Name -cne ($request.token + '.request.json')) { throw 'Wrong token.' }
        foreach ($field in @('runId','token','name','pid','executable','sourceSha','owner','reservation','bodyId','phase','bodyState','weaponState')) {
            if ($null -eq $request.$field -or $request.$field -cne $ack.$field) { throw "Wrong request/ack $field." }
        }
        if ($request.runId -cne [IO.Path]::GetFileName($Directory) -or $ack.schema -ne 1 -or $ack.status -cne 'captured' -or $ack.source -cne 'Pillow.ImageGrab.grab(window=observed-owned-HWND)' -or $ack.nonflat -ne $true -or $ack.hwnd -le 0 -or $ack.pid -le 0) { throw 'Wrong capture provenance.' }
        if ($processId -and $ack.pid -ne $processId) { throw 'Capture process changed.' }; $processId = $ack.pid
        if ($phase.runId -cne $request.runId -or $phase.token -cne $request.token -or $phase.phaseValid -ne $true -or $phase.phaseChecks -lt 2) { throw 'Missing phase continuity.' }
        $requested = Get-CaptureEvidenceTime $request.requestedUtc; $start = Get-CaptureEvidenceTime $ack.startedUtc; $end = Get-CaptureEvidenceTime $ack.finishedUtc
        if ($start -lt $requested -or $end -lt $start -or ($end-$requested).TotalSeconds -gt 10 -or (Get-CaptureEvidenceTime $phase.confirmedUtc) -lt $end) { throw 'Invalid/stale capture interval.' }
        $png = Join-Path $Directory ($request.name + '.png')
        if ($ack.png -cne ($request.name + '.png') -or (Get-FileHash -LiteralPath $png -Algorithm SHA256).Hash -cne $ack.pngSha256) { throw 'PNG/hash mismatch.' }
        $hashes += $ack.pngSha256
        $bytes = [IO.File]::ReadAllBytes($png)
        if ($bytes.Length -lt 24 -or [Convert]::ToBase64String($bytes[0..7]) -cne 'iVBORw0KGgo=') { throw 'Invalid PNG.' }
        $width = ([long]$bytes[16]*16777216)+([long]$bytes[17]*65536)+([long]$bytes[18]*256)+$bytes[19]
        $height = ([long]$bytes[20]*16777216)+([long]$bytes[21]*65536)+([long]$bytes[22]*256)+$bytes[23]
        if ($width -le 0 -or $height -le 0 -or $ack.width -ne $width -or $ack.height -ne $height) { throw 'Original PNG dimensions mismatch.' }
    }
    if (@(Compare-Object $names $observed).Count -or @($observed | Select-Object -Unique).Count -ne 5) { throw 'Wrong checkpoint set.' }
    if (@($hashes | Select-Object -Unique).Count -ne 5) { throw 'Repeated window pixels across distinct moving baseline phases; stale presentation.' }
}

if ($MyInvocation.InvocationName -eq '.') { return }
if (!$StageRecord -or !$RuntimeReservation -or !$RuntimeOwner -or !$RuntimeLockPath) { throw 'Supply staged record, canonical lease path, owner and run token.' }
if ($TimeoutSeconds -lt 30 -or $TimeoutSeconds -gt 900) { throw 'Timeout must be 30..900 seconds.' }
$stage = Get-Content -LiteralPath $StageRecord -Raw | ConvertFrom-Json
if ($stage.status -ne 'staged' -or $stage.schema -ne 1 -or $stage.reservation -ne $RuntimeReservation -or $stage.owner -cne $RuntimeOwner -or $stage.runtimeLockPath -ne $RuntimeLockPath) { throw 'Stage/reservation mismatch.' }
if (!$VisibleWindow -or !$WindowCapturePython -or !(Test-Path -LiteralPath $WindowCapturePython -PathType Leaf)) { throw 'Supply -VisibleWindow and the installed Pillow/pywin32 Python executable for baseline captures.' }
Assert-RuntimeLease $RuntimeLockPath $RuntimeOwner $RuntimeReservation | Out-Null
Assert-NoGame
$profile = Get-ProfilePath $stage.profileName
if ($profile -ne $stage.profile) { throw 'Profile identity mismatch.' }
foreach ($replacement in $stage.replacements) { Assert-ProfileTarget $profile $replacement.target }
foreach ($identity in @($stage.game) + @($stage.support) + @($stage.config) + @($stage.replacements | ForEach-Object installed)) { Assert-Identity $identity }
foreach ($category in @('core','plugins','patchers')) {
    $current = @(Get-TreeIdentity (Join-Path $profile "BepInEx/$category") '*.dll' | ForEach-Object path)
    $recorded = @($stage.support | Where-Object { $_.path.StartsWith((Join-Path $profile "BepInEx/$category") + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) } | ForEach-Object path)
    if (@(Compare-Object $current $recorded).Count -ne 0) { throw "Support inventory changed: $category" }
}
$configs = @(Get-TreeIdentity (Join-Path $profile 'BepInEx/config') '*.cfg' | ForEach-Object path)
if (@(Compare-Object $configs @($stage.config | ForEach-Object path)).Count -ne 0) { throw 'Config inventory changed.' }
$preloader = Join-Path $profile 'BepInEx/core/BepInEx.Preloader.dll'
Get-Identity $preloader | Out-Null
if ($ValidateOnly) { Write-Output 'Launch preflight passed. Game was not launched.'; exit 0 }
$out = Join-Path $stage.runDirectory ('execution-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $out | Out-Null
Copy-Item -LiteralPath $StageRecord -Destination (Join-Path $out 'identity.json')
$process = $null; $timedOut = $false; $exitCode = -1; $failure = $null; $launchUtc = [DateTime]::UtcNow
$priorDir = $env:AH64_AUTOPILOT_DIR; $priorMode = $env:AH64_AUTOPILOT_MODE
$priorCompare = $env:AH64_AUTOPILOT_WINDOW_CAPTURE
$captureHelper = $null
$profileLock = $null
try {
    $profileLock = [IO.File]::Open((Join-Path $profile '.ah64-autopilot.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    Assert-NoGame
    $env:AH64_AUTOPILOT_DIR = $out; $env:AH64_AUTOPILOT_MODE = 'solo-baseline-v1'
    $env:AH64_AUTOPILOT_WINDOW_CAPTURE = '1'
    @{schema=1; runId=[IO.Path]::GetFileName($out); visibleWindow=[bool]$VisibleWindow; createdUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'launch-settings.json')
    $args = '--doorstop-enabled true --doorstop-target-assembly "' + $preloader + '" --r2profile "' + $stage.profileName + '" -logFile "' + (Join-Path $out 'Player.log') + '"'
    $windowStyle = $(if ($VisibleWindow) { 'Normal' } else { 'Hidden' })
    $helperArguments = '"' + (Join-Path $PSScriptRoot 'Capture-OwnedWindow.py') + '" "' + $StageRecord + '" "' + $out + '"'
    $captureHelper = Start-Process -FilePath $WindowCapturePython -ArgumentList $helperArguments -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $out 'window-helper.log') -RedirectStandardError (Join-Path $out 'window-helper-error.log')
    $process = Start-Process -FilePath (Join-Path $stage.gameDirectory 'Risk of Rain 2.exe') -WorkingDirectory $stage.gameDirectory -ArgumentList $args -WindowStyle $windowStyle -PassThru
    $lease = Assert-RuntimeLease $RuntimeLockPath $RuntimeOwner $RuntimeReservation
    $lease.processIds = @($process.Id)
    $lease | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $RuntimeLockPath
    $env:AH64_AUTOPILOT_DIR = $priorDir; $env:AH64_AUTOPILOT_MODE = $priorMode
    $env:AH64_AUTOPILOT_WINDOW_CAPTURE = $priorCompare
    $watch = [Diagnostics.Stopwatch]::StartNew()
    while (!$process.HasExited -and $watch.Elapsed.TotalSeconds -lt $TimeoutSeconds) { Start-Sleep -Milliseconds 500; $process.Refresh() }
    if (!$process.HasExited) {
        $timedOut = $true
        # Only the PID returned by this launch is ours. A rejected boot/other process is never killed.
        Stop-Process -Id $process.Id -Force -ErrorAction Stop
        $process.WaitForExit(10000) | Out-Null
    }
    if ($process.HasExited) { $exitCode = $process.ExitCode }
} catch { $failure = $_.Exception.Message }
finally {
    $env:AH64_AUTOPILOT_DIR = $priorDir; $env:AH64_AUTOPILOT_MODE = $priorMode
    $env:AH64_AUTOPILOT_WINDOW_CAPTURE = $priorCompare
    if ($captureHelper) {
        if (!$captureHelper.WaitForExit(5000)) { Stop-Process -Id $captureHelper.Id -Force -ErrorAction Stop; $failure = 'Owned-window helper did not finish.' }
        else {
            # Require fresh protocol completion even when the Process wrapper's ExitCode is absent.
            try {
                if ($null -ne $captureHelper.ExitCode -and $captureHelper.ExitCode -ne 0) { throw ('Helper process exit ' + $captureHelper.ExitCode) }
                $helperResult = Get-Content -LiteralPath (Join-Path $out 'window-helper-result.json') -Raw | ConvertFrom-Json
                $expected = @('roll-entered','roll-cleanup','backflip-entered','backflip-cleanup','hellfire-launched')
                if ($helperResult.schema -ne 1 -or $helperResult.status -cne 'completed' -or $helperResult.runId -cne [IO.Path]::GetFileName($out) -or $helperResult.checkpointNames.Count -ne 5 -or @(Compare-Object $expected $helperResult.checkpointNames).Count -or (Get-Item -LiteralPath (Join-Path $out 'window-helper-error.log')).Length -ne 0) { throw 'Incomplete/failed helper terminal evidence.' }
            } catch { $failure = 'Owned-window helper failed: ' + $_.Exception.Message }
        }
    }
    $log = Join-Path $profile 'BepInEx/LogOutput.log'
    if (Test-Path -LiteralPath $log -PathType Leaf) {
        Copy-Item -LiteralPath $log -Destination (Join-Path $out 'LogOutput.log')
        if ((Get-Item -LiteralPath $log).LastWriteTimeUtc -lt $launchUtc) { $failure = 'Profile log predates this launch.' }
    }
    if ($profileLock) { $profileLock.Dispose() }
}
$verdict = Test-AutopilotEvidence $out $exitCode $timedOut
if ($failure) { $verdict.status = 'failed'; $verdict.problems += $failure }
$verdict | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'launcher-result.json')
if ($process -and $process.HasExited) { Remove-RuntimeLease $RuntimeLockPath $RuntimeOwner $RuntimeReservation }
Write-Output "Autopilot $($verdict.status): $out"
if ($verdict.status -ne 'passed') { exit 1 }
exit 0
