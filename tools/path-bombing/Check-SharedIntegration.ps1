[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$Dll,
      [Parameter(Mandatory=$true)][string]$BaselineDll,
      [Parameter(Mandatory=$true)][string]$CecilPath,
      [Parameter(Mandatory=$true)][string]$Report,
      [string]$GameManaged='C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed')
$ErrorActionPreference='Stop'
if(Test-Path $Report){throw 'Refuse compiled integration report overwrite'}
[Reflection.Assembly]::LoadFrom($CecilPath)|Out-Null
$module=[Mono.Cecil.ModuleDefinition]::ReadModule($Dll)
$baseline=[Mono.Cecil.ModuleDefinition]::ReadModule($BaselineDll)
$native=[Mono.Cecil.ModuleDefinition]::ReadModule((Join-Path $GameManaged 'RoR2.dll'))
function Method($m,[string]$type,[string]$name){@($m.GetType($type).Methods|Where-Object Name -CEQ $name)[0]}
function Calls($method,[string]$target){@($method.Body.Instructions|Where-Object {$_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.FullName -like $target})}
function Once($method,[string]$target){$calls=Calls $method $target;if($calls.Count -ne 1){throw ('Expected one compiled call: '+$target)};$calls[0]}
try {
 $priority=$native.GetType('EntityStates.InterruptPriority')
 foreach($pair in @(@('Skill',1),@('PrioritySkill',2),@('Pain',3))){
  $field=@($priority.Fields|Where-Object Name -CEQ $pair[0])[0]
  if(!$field -or [int]$field.Constant -ne $pair[1]){throw 'Installed-native interrupt enum differs'}
 }
 $awake=Method $module 'AH64.AH64Plugin' 'Awake'
 $hellfire=Once $awake '*AH64HellfireNetwork::Init*';$bombing=Once $awake '*AH64BombingRunNetwork::Init*'
 if($bombing.Offset -le $hellfire.Offset){throw 'Bombing Init must follow existing Hellfire Init'}
 [void](Once (Method $module 'AH64.AH64Plugin' 'OnDestroy') '*AH64BombingRunNetwork::Shutdown*')
 [void](Once (Method $module 'AH64.AH64Plugin' 'OnDestroy') '*AH64HellfireNetwork::Shutdown*')
 $create=Method $module 'AH64.Survivors.AH64Assets' 'CreateProjectiles'
 $after=Once $create '*CreateBombingRunProjectile*';$before=Once $create '*CreateHellfireProjectile*'
 if($after.Offset -le $before.Offset){throw 'Bombing construction precedes Hellfire presentation'}
 $prefab=Method $module 'AH64.Survivors.AH64Assets' 'CreateBombingRunProjectile'
 $build=Once $prefab '*AH64BombingRunProjectiles::Build*'
 $network=Once $prefab '*PrefabAPI::RegisterNetworkPrefab*';$catalog=Once $prefab '*Content::AddProjectilePrefab*'
 if($network.Offset -le $build.Offset -or $catalog.Offset -le $network.Offset){throw 'Prefab construction/network/catalog order changed'}
 # Release compilation duplicates the Build result on the stack; each void call
 # consumes one copy. Both registrations therefore receive the same prefab.
 if($build.Next.OpCode.Code.ToString() -cne 'Dup' -or $network.Previous -ne $build.Next -or $catalog.Previous -ne $network){throw 'Network and projectile catalog arguments differ'}
 $states=Method $module 'AH64.Survivors.AH64States' 'Init'
 foreach($state in @('BrakingTurn','BombingRun')){
  if(@($states.Body.Instructions|Where-Object {$_.OpCode.Code.ToString() -ceq 'Ldtoken' -and $_.Operand.FullName -ceq ('AH64.Survivors.SkillStates.'+$state)}).Count -ne 1){throw ('State catalog registration differs: '+$state)}
 }
 [void](Once (Method $module 'AH64.DevAutopilot' 'InstallHooks') '*AH64BombingRunTrace::add_Emitted*')
 foreach($name in @('OnDestroy','Finish')){[void](Once (Method $module 'AH64.DevAutopilot' $name) '*AH64BombingRunTrace::remove_Emitted*')}
 [void](Once (Method $module 'AH64.DevAutopilot' 'OnBombingTrace') '*AH64.DevAutopilot::Event*')
 [void](Once (Method $module 'AH64.DevAutopilot' 'OnBombingTrace') '*NetworkBehaviour::get_netId*')
 if((Calls (Method $module 'AH64.DevAutopilot' 'OnBombingTrace') '*StreamWriter*').Count){throw 'Bombing introduces another evidence writer'}
 $id=@($module.GetType('AH64.Survivors.Components.AH64BombingRunNetwork').Fields|Where-Object Name -CEQ 'TerminalMessageId')[0]
 if([int]$id.Constant -ne 28066){throw 'Bombing message ID changed'}
 $unchanged=@('AH64.Survivors.Components.AH64HoverController','AH64.Survivors.Components.AH64FlightVisuals',
  'AH64.Survivors.Components.AH64BrakingTurnMotor','AH64.Survivors.Components.AH64HellfireOwner',
  'AH64.Survivors.Components.AH64HellfireNetwork','AH64.Survivors.Components.AH64HellfireGuidance',
  'AH64.Survivors.Components.AH64PylonMissiles','AH64.Survivors.SkillStates.AH64Main',
  'AH64.Survivors.SkillStates.ServoDash','AH64.Survivors.SkillStates.SmokeBackflip',
  'AH64.Survivors.SkillStates.FireChaingun','AH64.Survivors.SkillStates.FireRocketPods',
  'AH64.Survivors.SkillStates.FireHellfire','AH64.Survivors.SkillStates.PaintLongbow','AH64.Survivors.SkillStates.FireLongbow')
 $methods=0
 foreach($type in $unchanged){
  $old=$baseline.GetType($type);$now=$module.GetType($type)
  if(!$old -or !$now -or $old.Methods.Count -ne $now.Methods.Count){throw ('Existing type topology changed: '+$type)}
  foreach($m in $old.Methods){
   $match=@($now.Methods|Where-Object FullName -CEQ $m.FullName)
   if($match.Count -ne 1 -or ((@($m.Body.Instructions|ForEach-Object ToString))-join "`n") -cne ((@($match[0].Body.Instructions|ForEach-Object ToString))-join "`n")){throw ('Existing compiled method changed: '+$m.FullName)}
   $methods++
  }
 }
 @{schema=1;status='passed';dllSha256=(Get-FileHash $Dll -Algorithm SHA256).Hash;baselineDllSha256=(Get-FileHash $BaselineDll -Algorithm SHA256).Hash;
  installedNativePriority=@{Skill=1;PrioritySkill=2;Pain=3};pluginInitShutdown=$true;prefabBuildNetworkCatalogOnce=$true;stateRegistrationOnce=$true;
  ownedTraceSubscribeAndCleanup=$true;terminalMessageId=28066;unchangedTypes=$unchanged;unchangedMethods=$methods;
  limitation='Compiled IL structure and exact unchanged method comparison only. Does not execute Unity prefab/catalog/network lifecycle, native stock/damage or simultaneous controls.'}|ConvertTo-Json -Depth 8|Set-Content $Report
 Write-Output ('SHARED_INTEGRATION_COMPILED_PASS unchangedMethods='+$methods+' report='+$Report)
}finally{$module.Dispose();$baseline.Dispose();$native.Dispose()}
