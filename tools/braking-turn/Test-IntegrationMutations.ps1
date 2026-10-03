[CmdletBinding()]
param()
. (Join-Path $PSScriptRoot 'Set-Environment.ps1')
$baselineProject=Join-Path $brakingRoot 'dist/movement-checks/VisualChecks.csproj'
if(!(Test-Path -LiteralPath $baselineProject)){throw 'Run tools/movement/check-movement.ps1 first'}
$visualPath=Join-Path $brakingRoot 'AH64Mod/Characters/Survivors/AH64/Components/AH64FlightVisuals.cs'
$original=Get-Content -LiteralPath $visualPath -Raw
$originalHash=(Get-FileHash -LiteralPath $visualPath -Algorithm SHA256).Hash
$run=Join-Path $brakingEvidence ('integration-mutations-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run|Out-Null
$mutants=@(
 @{name='discard-entry-world';from='brakingEntryWorld = hasRenderedBaseWorld ? lastRenderedBaseWorld : model.rotation;';to='brakingEntryWorld = yaw * leanLocal;';expected='braking entry preserves displayed world attitude'},
 @{name='discard-recovery-world';from='Quaternion world = Quaternion.Slerp(lastRenderedBaseWorld, target,';to='Quaternion world = Quaternion.Slerp(yaw * leanLocal, target,';expected='interruption recovery preserves world continuity'}
)
$results=@()
foreach($mutant in $mutants){
 if(!$original.Contains($mutant.from)){throw ('Mutation target missing: '+$mutant.name)}
 $dir=Join-Path $run $mutant.name;New-Item -ItemType Directory -Path $dir|Out-Null
 $copy=Join-Path $dir 'AH64FlightVisuals.cs';[IO.File]::WriteAllText($copy,$original.Replace($mutant.from,$mutant.to))
 [xml]$project=Get-Content -LiteralPath $baselineProject -Raw
 $matches=@($project.Project.ItemGroup.Compile|Where-Object {$_.Include -ceq $visualPath})
 if($matches.Count -ne 1){throw 'Exact compiled visual source not identified'}
 $matches[0].Include=$copy;$projectPath=Join-Path $dir 'VisualChecks.csproj';$project.Save($projectPath)
 $log=Join-Path $dir 'result.log'
 & dotnet run --project $projectPath -c Release --disable-build-servers /p:UseSharedCompilation=false /nodeReuse:false *> $log
 if($LASTEXITCODE -eq 0 -or !(Get-Content -LiteralPath $log -Raw).Contains($mutant.expected)){throw ('Visual mutant did not fail at intended assertion: '+$mutant.name)}
 $results+=@{name=$mutant.name;rejected=$true;expected=$mutant.expected;sourceCopySha256=(Get-FileHash $copy -Algorithm SHA256).Hash;log=$log}
}
if((Get-FileHash -LiteralPath $visualPath -Algorithm SHA256).Hash -cne $originalHash){throw 'Production visual source changed'}
@{schema=1;originalVisualSha256=$originalHash;results=$results;scope='Generated source-copy mutations in the actual central visual component, exercised by API-double integration checks; no native build/deploy/game execution.'}|ConvertTo-Json -Depth 6|Set-Content (Join-Path $run 'result.json')
Write-Output ('PASS '+$results.Count+' braking visual negative controls; evidence='+$run)
