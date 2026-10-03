[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$root=Join-Path $repo ('dist/probe-mutations/'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root|Out-Null
$relative=@('tools/movement/MovementStubs.cs','tools/movement/VisualStubs.cs','tools/movement/ProbeValidityChecks.cs',
 'AH64Mod/Characters/Survivors/AH64/Content/AH64StaticValues.cs',
 'AH64Mod/Characters/Survivors/AH64/Components/AH64ManeuverCapture.cs',
 'AH64Mod/Characters/Survivors/AH64/Components/AH64ManeuverMotor.cs',
 'AH64Mod/Characters/Survivors/AH64/SkillStates/ServoDash.cs',
 'AH64Mod/Characters/Survivors/AH64/SkillStates/SmokeBackflip.cs')
$before=@($relative|ForEach-Object {@{path=(Join-Path $repo $_);sha256=(Get-FileHash (Join-Path $repo $_) -Algorithm SHA256).Hash}})
$results=@()
foreach($variant in @('control','priority-regression','steering-disabled')) {
 $directory=Join-Path $root $variant
 New-Item -ItemType Directory -Path $directory|Out-Null
 $includes=@()
 foreach($path in $relative) {
  $text=Get-Content -LiteralPath (Join-Path $repo $path) -Raw -Encoding UTF8
  if($variant -eq 'priority-regression' -and $path -match '/(ServoDash|SmokeBackflip)\.cs$') {
   if(([regex]::Matches($text,'return InterruptPriority.Pain;')).Count -ne 1){throw 'Priority mutant anchor ambiguous'}
   $text=$text.Replace('return InterruptPriority.Pain;','return InterruptPriority.PrioritySkill;')
  }
  if($variant -eq 'steering-disabled' -and $path.EndsWith('/AH64ManeuverCapture.cs')) {
   $anchor='float side = Mathf.Clamp(Vector3.Dot(Horizontal(input), right), -1f, 1f);'
   if(!$text.Contains($anchor)){throw 'Steering mutant anchor missing'}
   $text=$text.Replace($anchor,'float side = 0f; // Isolated negative control ignores native lateral input.')
  }
  $copy=Join-Path $directory ([IO.Path]::GetFileName($path))
  Set-Content -LiteralPath $copy -Value $text -Encoding UTF8
  $includes+='<Compile Include="'+[Security.SecurityElement]::Escape($copy)+'"/>'
 }
 $project=Join-Path $directory 'ProbeValidity.csproj'
 '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseSharedCompilation>false</UseSharedCompilation><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup>'+($includes -join '')+'</ItemGroup></Project>'|Set-Content -LiteralPath $project
 & dotnet restore $project --ignore-failed-sources -p:NuGetAudit=false *> (Join-Path $directory 'restore.log')
 if($LASTEXITCODE -ne 0){throw 'Offline mutant fixture restore failed'}
 & dotnet build $project -c Release --no-restore -p:UseSharedCompilation=false -nodeReuse:false *> (Join-Path $directory 'build.log')
 if($LASTEXITCODE -ne 0){throw 'Offline mutant fixture build failed'}
 $modes=if($variant -eq 'control'){@('priority','steering')}elseif($variant -eq 'priority-regression'){@('priority')}else{@('steering')}
 foreach($mode in $modes) {
  $log=Join-Path $directory ($mode+'.log')
  & dotnet (Join-Path $directory 'bin/Release/net8.0/ProbeValidity.dll') $mode *> $log
  $code=$LASTEXITCODE;$output=Get-Content -LiteralPath $log -Raw
  $expected=if($variant -eq 'control'){0}else{1}
  $signature=if($variant -eq 'priority-regression'){'REGRESSION_PRIORITY_SPARE_STOCK'}elseif($variant -eq 'steering-disabled'){'REGRESSION_STEERING_SIGN'}else{'PROBE_VALIDITY_PASS'}
  if($code -ne $expected -or !$output.Contains($signature)){throw ('Mutant discrimination failed: '+$variant+'/'+$mode)}
  $results+=@{variant=$variant;mode=$mode;exit=$code;expectedExit=$expected;log=$log;signature=$signature}
  Write-Output $output.Trim()
 }
}
foreach($entry in $before){if((Get-FileHash -LiteralPath $entry.path -Algorithm SHA256).Hash -ne $entry.sha256){throw 'Candidate source changed during isolated mutation checks'}}
@{schema=1;status='passed';scope='Actual source copies and engine API doubles. No native mutant game build/deployment or profile changes; source hashes unchanged.';sourceHashes=$before;results=$results}|ConvertTo-Json -Depth 8|Set-Content (Join-Path $root 'result.json')
Write-Output ('MUTATION_CONTROLS_PASS evidence='+$root)
