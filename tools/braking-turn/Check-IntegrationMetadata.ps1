[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$Dll,
      [Parameter(Mandatory=$true)][string]$CecilPath,
      [Parameter(Mandatory=$true)][string]$Report)
$ErrorActionPreference='Stop'
if(Test-Path -LiteralPath $Report){throw 'Refuse metadata report overwrite'}
[Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $CecilPath).Path)|Out-Null
$module=[Mono.Cecil.ModuleDefinition]::ReadModule((Resolve-Path -LiteralPath $Dll).Path)
try {
 $method=@($module.GetType('AH64.Survivors.AH64Survivor').Methods|Where-Object Name -CEQ 'AddUtilitySkills')[0]
 $stack=[Collections.Generic.Stack[object]]::new();$locals=@{};$definitions=[Collections.Generic.List[object]]::new();$family=$null
 foreach($instruction in $method.Body.Instructions){
  $code=$instruction.OpCode.Code.ToString();$operand=$instruction.Operand
  switch -Regex ($code){
   '^Nop$'{}
   '^Ldarg_0$'{$stack.Push('survivor')}
   '^Ldstr$'{$stack.Push([string]$operand)}
   '^Ldtoken$'{$stack.Push($operand.FullName)}
   '^Ldc_I4_[0-8]$'{$stack.Push([int]$code.Substring(7))}
   '^Ldc_I4(_S)?$'{$stack.Push([int]$operand)}
   '^Ldc_R4$'{$stack.Push([float]$operand)}
   '^Dup$'{$stack.Push($stack.Peek())}
   '^Pop$'{[void]$stack.Pop()}
   '^Stloc_[0-3]$'{$locals[[int]$code.Substring(6)]=$stack.Pop()}
   '^Ldloc_[0-3]$'{$stack.Push($locals[[int]$code.Substring(6)])}
   '^Stfld$'{$value=$stack.Pop();$target=$stack.Pop();$target[$operand.Name]=$value}
   '^Newarr$'{$stack.Push([object]([object[]]::new([int]$stack.Pop())))}
   '^Stelem_Ref$'{$value=$stack.Pop();$index=[int]$stack.Pop();$array=$stack.Pop();$array[$index]=$value}
   '^Newobj$'{
    if($operand.DeclaringType.FullName -ceq 'AH64.Modules.SkillDefInfo'){$stack.Push(@{})}
    elseif($operand.DeclaringType.FullName -ceq 'EntityStates.SerializableEntityStateType'){$type=$stack.Pop();$stack.Push($type)}
    else{throw ('Unmodeled constructor: '+$operand.FullName)}
   }
   '^Call(virt)?$'{
    $member=$operand.FullName
    if($operand.Name -ceq 'get_bodyPrefab' -or $operand.Name -ceq 'get_assetBundle'){[void]$stack.Pop();$stack.Push($operand.Name)}
    elseif($operand.Name -ceq 'LoadAsset'){$name=$stack.Pop();[void]$stack.Pop();$stack.Push($name)}
    elseif($operand.DeclaringType.FullName -ceq 'System.Type' -and $operand.Name -ceq 'GetTypeFromHandle'){$type=$stack.Pop();$stack.Push($type)}
    elseif($operand.Name -ceq 'get_DashCooldown'){$stack.Push('existing-config-DashCooldown')}
    elseif($operand.Name -ceq 'CreateGenericSkillWithSkillFamily'){[void]$stack.Pop();[void]$stack.Pop();[void]$stack.Pop();$stack.Push('empty-family')}
    elseif($operand.Name -ceq 'CreateSkillDef'){$definition=$stack.Pop();$definitions.Add($definition);$stack.Push($definition)}
    elseif($operand.Name -ceq 'AddUtilitySkills'){$family=$stack.Pop();[void]$stack.Pop()}
    else{throw ('Unmodeled compiled call: '+$member)}
   }
   '^Ret$'{}
   default{throw ('Unmodeled instruction: '+$instruction)}
  }
 }
 if($definitions.Count -ne 3 -or $family.Count -ne 3 -or $stack.Count){throw 'Unexpected compiled utility registration topology'}
 $expected=@('AH64EvasiveJink','AH64SmokeBackflip','AH64BrakingTurn')
 for($i=0;$i -lt 3;$i++){if($family[$i].skillName -cne $expected[$i]){throw 'Default/order changed'}}
 $braking=$family[2]
 $required=@{activationState='AH64.Survivors.SkillStates.BrakingTurn';activationStateMachineName='Body';interruptPriority=2;baseRechargeInterval=4;baseMaxStock=1;rechargeStock=1;requiredStock=1;stockToConsume=1;mustKeyPress=1;fullRestockOnAssign=1;resetCooldownTimerOnUse=0;beginSkillCooldownOnSkillEnd=0;isCombatSkill=0;canceledFromSprinting=0;cancelSprintingOnActivation=0;forceSprintDuringState=0;dontAllowPastMaxStocks=0;skillIcon='texAH64UtilityIcon';skillNameToken='AH64_UTILITY_BRAKING_TURN_NAME';skillDescriptionToken='AH64_UTILITY_BRAKING_TURN_DESCRIPTION'}
 foreach($entry in $required.GetEnumerator()){if($braking[$entry.Key] -cne $entry.Value){throw ('Braking registration value mismatch: '+$entry.Key)}}
 $states=@($module.GetType('AH64.Survivors.AH64States').Methods|Where-Object Name -CEQ 'Init')[0]
 $registrations=@($states.Body.Instructions|Where-Object {$_.OpCode.Code.ToString() -ceq 'Ldtoken' -and $_.Operand.FullName -ceq 'AH64.Survivors.SkillStates.BrakingTurn'})
 if($registrations.Count -ne 1 -or $registrations[0].Next.Next.Operand.Name -cne 'AddEntityState'){throw 'State catalog registration absent or duplicated'}
 $owner=@($module.GetType('AH64.Survivors.Components.AH64HellfireOwner').Methods|Where-Object Name -CEQ 'get_Interrupted')[0]
 if(@($owner.Body.Instructions|Where-Object {$_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.FullName -like '*AH64HellfireInterruption::IsInterrupted*'}).Count -ne 1){throw 'Native owner is not wired to tested typed policy'}
 @{schema=1;dllSha256=(Get-FileHash -LiteralPath $Dll -Algorithm SHA256).Hash;status='passed';compiledUtilityFamily=$family;defaultIndex=0;stateRegisteredOnce=$true;typedPolicyWired=$true;scope='Interpret exact compiled registration IL with asset/config/type/skill construction operations modeled. No Unity/game/catalog/profile execution; no native stock/peer verification.'}|ConvertTo-Json -Depth 10|Set-Content -LiteralPath $Report
 Write-Output ('BRAKING_COMPILED_WIRING_PASS utilityOrder='+($expected -join ',')+' report='+$Report)
}finally{$module.Dispose()}
