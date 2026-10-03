# Offline protocol tests only: synthetic evidence and a local fixture lease, no profiles/game.
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Run-Autopilot.ps1')
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$root = Join-Path $repo ('dist/foundation-tests/' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root -Force | Out-Null
$script:count = 0
function Check([bool]$condition, [string]$name) {
    if (!$condition) { throw "FOUNDATION_CHECK_FAIL: $name" }
    $script:count++
}
function MustThrow([scriptblock]$body, [string]$name) {
    $threw = $false; try { & $body | Out-Null } catch { $threw = $true }
    Check $threw $name
}
$ids = @('roll.enter','roll.travel','roll.exit','roll.cleanup','backflip.enter','backflip.travel','backflip.exit','backflip.cleanup','hellfire.enter','hellfire.launch','hellfire.exit','hellfire.cleanup')
function Fixture {
    $dir = Join-Path $root ([guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $dir | Out-Null
    $checks = @($ids | ForEach-Object { @{ id = $_; passed = $true } })
    $summary = @{ schemaVersion = 1; schema = 1; recordType = 'summary'; runId = [IO.Path]::GetFileName($dir); scenario = 'baseline-v1'; suite = 'baseline-v1'; status = 'completed'; expectedAssertions = 12; assertions = 12; failedAssertions = 0; executed = 12; passed = 12; failed = 0; skipped = 0; errors = 0; warnings = 0; visualFlags = 0; samples = 100; checks = $checks }
    $summary.captures = 5
    $summary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $dir 'result.json')
    $header = @{ schemaVersion = 1; recordType = 'header'; runId = $summary.runId; scenario = 'baseline-v1'; sourceSha = ('a' * 40); dirtyWorkspaceFingerprint = ('b' * 64); pluginVersion = '1.2.1'; profile = 'fixture'; requestedAssertionCount = 12; dllSha256 = 'DLL'; bundleSha256 = 'BUNDLE'; bankSha256 = 'BANK'; configSha256 = 'CONFIG'; gameBuild = 'GAME' }
    $identity = @{ sourceSha = $header.sourceSha; dirtyWorkspaceFingerprint = $header.dirtyWorkspaceFingerprint; pluginVersion = '1.2.1'; profile = 'fixture'; game = @(@{ path = 'RoR2.dll'; sha256 = 'GAME' }); replacements = @(
        @{ target = 'AH64.dll'; installed = @{ sha256 = 'DLL' } }, @{ target = 'ah64'; installed = @{ sha256 = 'BUNDLE' } },
        @{ target = 'AH64Rotor.bnk'; installed = @{ sha256 = 'BANK' } }, @{ target = 'com.JohnstonStu.AH64.cfg'; installed = @{ sha256 = 'CONFIG' } }) }
    $identity | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $dir 'identity.json')
    $records = @($header)
    $records += @{ schemaVersion = 1; recordType = 'event'; runId = $summary.runId; eventName = 'state-enter' }
    $records += @($ids | ForEach-Object { @{ schemaVersion = 1; recordType = 'assertion'; runId = $summary.runId; assertionId = $_; passed = $true } })
    $records += @(1..100 | ForEach-Object { @{ schemaVersion = 1; recordType = 'sample'; runId = $summary.runId; tick = $_ } })
    $records += $summary
    $records | ForEach-Object { $_ | ConvertTo-Json -Depth 5 -Compress } | Set-Content -LiteralPath (Join-Path $dir 'telemetry.jsonl')
    @('tick,time,scenario,px,py,pz,vx,vy,vz,fx,fy,fz,ax,ay,az,qx,qy,qz,qw') + @(1..100 | ForEach-Object { "$_,0.02,roll,0,0,0,0,0,0,0,0,1,0,0,1,0,0,0,1" }) | Set-Content -LiteralPath (Join-Path $dir 'telemetry.csv')
    $ids | ForEach-Object { "ASSERT $_ PASS" } | Set-Content -LiteralPath (Join-Path $dir 'trace.txt')
    foreach ($name in @('runtime.log','LogOutput.log','Player.log')) { 'clean fixture log' | Set-Content -LiteralPath (Join-Path $dir $name) }
    $bridge = Join-Path $dir 'window-captures'; New-Item -ItemType Directory -Path $bridge | Out-Null
    $fixturePixelTag=0
    foreach ($name in @('roll-entered','roll-cleanup','backflip-entered','backflip-cleanup','hellfire-launched')) {
        $token = [guid]::NewGuid().ToString('N'); $now = [DateTime]::UtcNow
        $request = @{schema=1;runId=$summary.runId;token=$token;name=$name;pid=17;executable='fixture.exe';sourceSha=$header.sourceSha;owner='fixture';reservation='fixture';bodyId=42;phase=($name -split '-')[0];bodyState='fixture';weaponState='fixture';requestedUtc=$now.ToString('o')}
        $request | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $bridge ($token+'.request.json'))
        $fixturePixelTag++
        $png = Join-Path $dir ($name+'.png'); [IO.File]::WriteAllBytes($png,[byte[]](137,80,78,71,13,10,26,10,0,0,0,13,73,72,68,82,0,0,0,2,0,0,0,1,$fixturePixelTag)) # Distinct synthetic PNG headers, not image evidence.
        $ack = @{}; foreach ($key in $request.Keys) { $ack[$key]=$request[$key] }
        $ack.status='captured';$ack.source='Pillow.ImageGrab.grab(window=observed-owned-HWND)';$ack.nonflat=$true;$ack.hwnd=99;$ack.width=2;$ack.height=1;$ack.startedUtc=$now.AddMilliseconds(1).ToString('o');$ack.finishedUtc=$now.AddMilliseconds(2).ToString('o');$ack.png=$name+'.png';$ack.pngSha256=(Get-FileHash -LiteralPath $png -Algorithm SHA256).Hash
        $ack | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $bridge ($token+'.ack.json'))
        @{runId=$summary.runId;token=$token;phaseValid=$true;phaseChecks=2;confirmedUtc=$now.AddMilliseconds(3).ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $bridge ($token+'.phase.json'))
    }
    $dir
}
function RejectFixture([scriptblock]$mutation, [string]$name) {
    $dir = Fixture; & $mutation $dir
    Check ((Test-AutopilotEvidence $dir 0 $false).status -eq 'failed') $name
}
$good = Fixture
Check ((Get-CaptureEvidenceTime ([DateTime]::Parse('2026-10-03T12:44:07.254347Z'))).UtcDateTime.Ticks -eq (Get-CaptureEvidenceTime '2026-10-03T12:44:07.254347+00:00').UtcDateTime.Ticks) 'typed JSON dates preserve capture interval precision'
Check ((Test-AutopilotEvidence $good 0 $false).status -eq 'passed') 'complete valid fixture passes'
Check ((Test-AutopilotEvidence $good 0 $true).status -eq 'failed') 'timeout cannot pass'
Check ((Test-AutopilotEvidence $good 1 $false).status -eq 'failed') 'process failure cannot pass'
RejectFixture { param($d) Remove-Item -LiteralPath (Join-Path $d 'result.json') } 'exit0 without result fails'
RejectFixture { param($d) Remove-Item -LiteralPath (Join-Path $d 'trace.txt') } 'missing trace fails'
RejectFixture { param($d) Remove-Item -LiteralPath (Join-Path $d 'Player.log') } 'missing full player log fails'
RejectFixture { param($d) '{invalid' | Set-Content -LiteralPath (Join-Path $d 'result.json') } 'malformed result fails'
RejectFixture { param($d) '[Warning : preloader] startup warning' | Add-Content -LiteralPath (Join-Path $d 'LogOutput.log') } 'loading warnings fail'
RejectFixture { param($d) 'NullReferenceException during loading' | Add-Content -LiteralPath (Join-Path $d 'Player.log') } 'loading exception fails'
RejectFixture { param($d) (Get-Content (Join-Path $d 'telemetry.csv'))[0..50] | Set-Content (Join-Path $d 'telemetry.csv') } 'missing physics trace rows fail'
RejectFixture { param($d) (Get-Content (Join-Path $d 'telemetry.csv') -Raw).Replace('10,0.02','11,0.02') | Set-Content (Join-Path $d 'telemetry.csv') } 'duplicate physics tick fails'
RejectFixture { param($d) (Get-Content (Join-Path $d 'telemetry.csv') -Raw).Replace(',0.02,',',NaN,') | Set-Content (Join-Path $d 'telemetry.csv') } 'nonfinite telemetry fails'
foreach ($change in @(@{ status = 'incomplete' },@{ assertions = 11 },@{ expectedAssertions = 11 },@{ errors = 1 },@{ warnings = 1 },@{ visualFlags = 1 },@{ runId = 'wrong' })) {
    $dir = Fixture; $result = Get-Content (Join-Path $dir 'result.json') -Raw | ConvertFrom-Json
    foreach ($key in $change.Keys) { $result.$key = $change[$key] }
    $result | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $dir 'result.json')
    Check ((Test-AutopilotEvidence $dir 0 $false).status -eq 'failed') ('reject ' + ($change.Keys -join ','))
}
RejectFixture { param($d) $r = Get-Content (Join-Path $d 'result.json') -Raw | ConvertFrom-Json; $r.checks[1].id = $r.checks[0].id; $r | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $d 'result.json') } 'duplicate assertion ID fails'
RejectFixture { param($d) $r = Get-Content (Join-Path $d 'identity.json') -Raw | ConvertFrom-Json; $r.replacements[0].installed.sha256 = 'CHANGED'; $r | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $d 'identity.json') } 'artifact drift fails'
RejectFixture { param($d) $r = Get-Content (Join-Path $d 'identity.json') -Raw | ConvertFrom-Json; $r.sourceSha = 'CHANGED'; $r | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $d 'identity.json') } 'source identity drift fails'
RejectFixture { param($d) Get-Content (Join-Path $d 'telemetry.jsonl') | Where-Object { $_ -notmatch '"recordType":"summary"' } | Set-Content (Join-Path $d 'telemetry.bad'); Move-Item (Join-Path $d 'telemetry.bad') (Join-Path $d 'telemetry.jsonl') -Force } 'missing JSONL terminal summary fails'
MustThrow { Get-ProfilePath 'demo time' } 'production profile rejected'
MustThrow { Get-TreeIdentity (Join-Path $root 'absent') '*.dll' } 'missing dependency directory rejected'
MustThrow { Assert-DependencyLock @{ packages = @() } @{ dependencies = @('Required-Package-1.0.0') } $root } 'missing dependency package rejected'
$invalidStage = Join-Path $root 'invalid-stage.json'
'{}' | Set-Content -LiteralPath $invalidStage
$launcherFailure = ''
try {
    & (Join-Path $PSScriptRoot 'Run-Autopilot.ps1') -StageRecord $invalidStage -RuntimeOwner 'fixture-owner' -RuntimeReservation 'fixture-token' -RuntimeLockPath (Join-Path $root 'never-created.lock') -ValidateOnly
} catch { $launcherFailure = $_.Exception.Message }
Check ($launcherFailure -eq 'Stage/reservation mismatch.') 'launcher preserves lease arguments when importing staging helpers'
$lock = Join-Path $root 'runtime.lock' # LOCAL FIXTURE, never the canonical integration lease
New-RuntimeLease $lock 'fixture-owner' 'fixture-token' 'fixture-profile' ('a' * 40)
Check ((Assert-RuntimeLease $lock 'fixture-owner' 'fixture-token').owner -eq 'fixture-owner') 'lease identity roundtrip'
MustThrow { New-RuntimeLease $lock 'other' 'other' 'fixture' 'sha' } 'atomic occupied lease rejects'
MustThrow { Assert-RuntimeLease $lock 'other' 'fixture-token' } 'wrong owner cannot release'
MustThrow { Assert-RuntimeLease $lock 'fixture-owner' 'other' } 'wrong token cannot release'
# Do not use Remove-RuntimeLease here: that consults live game process state. Fixture deletion only.
Remove-Item -LiteralPath $lock
'partial' | Set-Content -LiteralPath $lock
MustThrow { New-RuntimeLease $lock 'fixture-owner' 'fixture-token' 'fixture' 'sha' } 'partly written lease stays occupied'
MustThrow { Assert-RuntimeLease $lock 'fixture-owner' 'fixture-token' } 'malformed lease cannot pass'
RejectFixture { param($d) Remove-Item -LiteralPath (Join-Path $d 'roll-entered.png') } 'missing checkpoint pixels fail'
RejectFixture { param($d) $p=Get-ChildItem (Join-Path $d 'window-captures') -Filter '*.ack.json' | Select-Object -First 1; $a=Get-Content $p.FullName -Raw | ConvertFrom-Json; $a.token='old'; $a | ConvertTo-Json | Set-Content $p.FullName } 'stale capture token fails'
RejectFixture { param($d) $p=Get-ChildItem (Join-Path $d 'window-captures') -Filter '*.ack.json' | Select-Object -First 1; $a=Get-Content $p.FullName -Raw | ConvertFrom-Json; $a.pid=18; $a | ConvertTo-Json | Set-Content $p.FullName } 'wrong capture process fails'
RejectFixture { param($d) $p=Get-ChildItem (Join-Path $d 'window-captures') -Filter '*.ack.json' | Select-Object -First 1; $a=Get-Content $p.FullName -Raw | ConvertFrom-Json; $a.nonflat=$false; $a | ConvertTo-Json | Set-Content $p.FullName } 'flat checkpoint fails'
RejectFixture { param($d) $p=Get-ChildItem (Join-Path $d 'window-captures') -Filter '*.phase.json' | Select-Object -First 1; $a=Get-Content $p.FullName -Raw | ConvertFrom-Json; $a.phaseValid=$false; $a | ConvertTo-Json | Set-Content $p.FullName } 'expired checkpoint phase fails'
RejectFixture { param($d) $first=Join-Path $d 'roll-entered.png'; Get-ChildItem (Join-Path $d 'window-captures') -Filter '*.ack.json' | ForEach-Object { $a=Get-Content $_.FullName -Raw | ConvertFrom-Json; $target=Join-Path $d $a.png; if ($target -ne $first) { Copy-Item -LiteralPath $first -Destination $target -Force }; $a.pngSha256=(Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash; $a | ConvertTo-Json | Set-Content $_.FullName } } 'identical presentation at all moving phases fails'
$result = @{ status = 'passed'; checks = $script:count; evidence = $root; runtime = 'not run'; profiles = 'untouched' }
$result | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root 'checks.json')
Write-Output "FOUNDATION_CHECK_PASS checks=$script:count evidence=$root"
