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
    $dir
}
function RejectFixture([scriptblock]$mutation, [string]$name) {
    $dir = Fixture; & $mutation $dir
    Check ((Test-AutopilotEvidence $dir 0 $false).status -eq 'failed') $name
}
$good = Fixture
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
$result = @{ status = 'passed'; checks = $script:count; evidence = $root; runtime = 'not run'; profiles = 'untouched' }
$result | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root 'checks.json')
Write-Output "FOUNDATION_CHECK_PASS checks=$script:count evidence=$root"
