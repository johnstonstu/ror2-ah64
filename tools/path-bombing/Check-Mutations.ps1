$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Set-Environment.ps1')
$normalProject = Join-Path $bombOutput 'checks/Checks.csproj'
if (!(Test-Path -LiteralPath $normalProject)) { throw 'Run Check.ps1 first.' }
$projectText = Get-Content -LiteralPath $normalProject -Raw
$mutations = @(
    @{name='failed-entry-stays-running';file='../SkillStates/BombingRun.cs';before='if (cast == null) Cancel();';after='if (cast == null) { }';secondBefore='if (NetworkServer.active && cast == null) Cancel();';secondAfter='';expected='Host dead entry exits after cancellation'},
    @{name='missing-actor-cannot-route';file='AH64BombingRunNetwork.cs';before='if (!NetworkServer.active || !bodyObject || request == 0 || !terminal.IsTerminal) return;';after='if (!NetworkServer.active || !bodyObject || !bodyObject.GetComponent<CharacterBody>() || request == 0 || !terminal.IsTerminal) return;';expected='Remote missing-body entry delivers cancellation'},
    @{name='entry-exception-keeps-scheduling';file='../SkillStates/BombingRun.cs';before='cast?.Policy.Stop();';after='';expected='Host first-release-throws entry releases no later drops'},
    @{name='rejected-begin-retains-old-cast';file='AH64BombingRunOwner.cs';before='active?.Stop("replaced");';after='';expected='Rejected Begin stops and releases the prior cast'},
    @{name='unbounded-overlap';file='AH64BombingRunPolicy.cs';before='HitCount(target) < AH64BombingRunStaticValues.HitsPerTarget';after='true';expected='Three-hit cap'},
    @{name='activation-position';file='AH64BombingRunCast.cs';before='Vector3 center = Owner.corePosition;';after='Vector3 center = Vector3.zero;';expected='Actual turning path sampled'},
    @{name='client-simulates';file='AH64BombingRunProjectile.cs';before='if (!NetworkServer.active || finished) return;';after='if (finished) return;';expected='Observers never simulate projectile motion'},
    @{name='client-clock-completion';file='../SkillStates/BombingRun.cs';before='if (isAuthority && terminal.IsTerminal && outer && !outer.HasPendingState())';after='if (isAuthority && fixedAge >= AH64BombingRunStaticValues.Duration + UnityEngine.Time.fixedDeltaTime)';expected='Client age cannot end an unacknowledged cast';suite='completion'},
    @{name='impact-cleanup-not-finally';file='AH64BombingRunProjectile.cs';before='finally';after='catch { throw; }';expected='effect: impact finally destroys projectile'},
    @{name='swallowed-impact-exception';file='AH64BombingRunProjectile.cs';before='finally';after='catch (System.Exception) { } finally';expected='effect: original impact exception propagates'},
    @{name='ignore-stopped-cast';file='../SkillStates/BombingRun.cs';before='if (cast.Policy.Stopped) terminal.Resolve(AH64BombingRunPhase.Cancelled);';after='if (cast.Policy.Stopped) { }';expected='Host stage cancellation exits retained Weapon2';suite='terminal'},
    @{name='recipient-blind-send-once';file='AH64BombingRunTerminal.cs';before='if (ReferenceEquals(lastRecipient, recipient) && now < nextAttempt) return false;';after='if (lastRecipient != null) return false;';expected='Authority transfer receives terminal outcome';suite='terminal'},
    @{name='disable-terminal-retry';file='AH64BombingRunTerminal.cs';before='if (ReferenceEquals(lastRecipient, recipient) && now < nextAttempt) return false;';after='if (ReferenceEquals(lastRecipient, recipient)) return false;';expected='Terminal retries recover rejected delivery to returning authority';suite='terminal'},
    @{name='cancel-as-success';file='AH64BombingRunNetwork.cs';before='outcome = terminal.Phase';after='outcome = AH64BombingRunPhase.Succeeded';expected='Remote stage cancellation is explicit';suite='terminal'}
)
$root = Join-Path $bombOutput ('mutations-' + [guid]::NewGuid().ToString('N'))
foreach ($mutation in $mutations) {
    $output = Join-Path $root $mutation.name
    New-Item -ItemType Directory -Path $output -Force | Out-Null
    $original = [IO.Path]::GetFullPath((Join-Path $bombRepo ('AH64Mod/Characters/Survivors/AH64/Components/' + $mutation.file)))
    $text = Get-Content -LiteralPath $original -Raw
    if (!$text.Contains($mutation.before)) { throw ('Mutation target missing: ' + $mutation.name) }
    $mutant = Join-Path $output ([IO.Path]::GetFileName($mutation.file))
    $text = $text.Replace($mutation.before,$mutation.after)
    if ($mutation.secondBefore) {
        if (!$text.Contains($mutation.secondBefore)) { throw ('Second mutation target missing: ' + $mutation.name) }
        $text = $text.Replace($mutation.secondBefore,$mutation.secondAfter)
    }
    $text | Set-Content -LiteralPath $mutant
    $project = Join-Path $output 'Checks.csproj'
    $projectText.Replace([Security.SecurityElement]::Escape($original),[Security.SecurityElement]::Escape($mutant)) |
        Set-Content -LiteralPath $project
    $suite = if ($mutation.suite) { $mutation.suite } else { 'all' }
    dotnet run --project $project -c Release -p:NuGetAudit=false -p:UseSharedCompilation=false `
        -p:AH64DeployToProfiles=false -nodeReuse:false -- $suite *> (Join-Path $output 'run.log')
    $log = Get-Content (Join-Path $output 'run.log') -Raw
    if ($LASTEXITCODE -eq 0 -or $log -match 'error CS|build failed' -or $log -notmatch 'Unhandled exception.*System.Exception' -or
        !$log.Contains($mutation.expected)) {
        throw ('Mutation was not rejected by an assertion: ' + $mutation.name)
    }
    Write-Output ('PASS negative control: ' + $mutation.name)
}
Write-Output ('Mutation evidence: ' + $root)
