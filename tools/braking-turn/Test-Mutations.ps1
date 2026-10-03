[CmdletBinding()]
param()
. (Join-Path $PSScriptRoot 'Set-Environment.ps1')
$run = Join-Path $brakingEvidence ('mutations-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run | Out-Null
$basePath = 'AH64Mod/Characters/Survivors/AH64'
$files = @("$basePath/SkillStates/AH64Main.cs", "$basePath/SkillStates/BrakingTurn.cs",
    "$basePath/Content/AH64BrakingTurnStaticValues.cs")
$files += Get-ChildItem (Join-Path $brakingRoot "$basePath/Components/AH64BrakingTurn*.cs") |
    ForEach-Object { "$basePath/Components/" + $_.Name }
$mutants = @(
    @{ Name='discard-native-y'; File="$basePath/Components/AH64BrakingTurnMotor.cs";
       From='next.y = self.velocity.y;'; To='next.y = before.y;'; Expected='native vertical retained' },
    @{ Name='skip-post-authority'; File="$basePath/Components/AH64BrakingTurnMotor.cs";
       From='if (!CanWrite()) { Yield("post-move-handoff"); return; }'; To='// mutant: no post-native guard';
       Expected='nested authority' },
    @{ Name='remote-recapture'; File="$basePath/SkillStates/BrakingTurn.cs";
       From='if (isAuthority && !hasCapture)'; To='if (!isAuthority || !hasCapture)';
       Expected='observer no recapture' },
    @{ Name='skip-hover'; File="$basePath/SkillStates/BrakingTurn.cs";
       From='base.HandleMovements();'; To='// mutant: dropped inherited movement';
       Expected='inherited hover and collective paths' }
)
$results = @()
foreach ($mutant in $mutants) {
    $sourceRoot = Join-Path $run $mutant.Name
    foreach ($relative in $files) {
        $target = Join-Path $sourceRoot $relative
        New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $brakingRoot $relative) -Destination $target
    }
    $path = Join-Path $sourceRoot $mutant.File
    $source = Get-Content -LiteralPath $path -Raw
    if (!$source.Contains($mutant.From)) { throw "Mutation target missing: $($mutant.Name)" }
    [IO.File]::WriteAllText($path, $source.Replace($mutant.From,$mutant.To))
    $log = Join-Path $sourceRoot 'result.log'
    $failed = $false
    try { & (Join-Path $PSScriptRoot 'Check-Braking.ps1') -SourceRoot $sourceRoot -OutputDirectory (Join-Path $sourceRoot 'checks') *> $log }
    catch { $failed = $true }
    $content = Get-Content -LiteralPath $log -Raw
    if (!$failed -or !$content.Contains($mutant.Expected)) { throw "Mutant did not fail at expected assertion: $($mutant.Name). See $log" }
    $results += [pscustomobject]@{ name=$mutant.Name; expected=$mutant.Expected; rejected=$true; log=$log }
}
$results | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $run 'results.json')
Write-Output "PASS $($results.Count) negative controls; evidence=$run"
