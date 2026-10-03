# Cecil-built IL fixtures exercise inaccessible and unresolved references without running game code.
param([Parameter(Mandatory=$true)][string]$ScannerDll, [Parameter(Mandatory=$true)][string]$CecilPath,
    [string]$GameManaged = 'C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed')
$ErrorActionPreference = 'Stop'
Add-Type -Path $CecilPath
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$root = Join-Path $repo ('dist/access-fixtures/' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root -Force | Out-Null
$target = [Mono.Cecil.ModuleDefinition]::CreateModule('AccessFixtureTarget', [Mono.Cecil.ModuleKind]::Dll)
$type = [Mono.Cecil.TypeDefinition]::new('Fixture', 'Target', [Mono.Cecil.TypeAttributes]::Public, $target.TypeSystem.Object)
$target.Types.Add($type)
$publicField = [Mono.Cecil.FieldDefinition]::new('Visible', ([Mono.Cecil.FieldAttributes]::Public -bor [Mono.Cecil.FieldAttributes]::Static), $target.TypeSystem.Int32)
$privateField = [Mono.Cecil.FieldDefinition]::new('Hidden', ([Mono.Cecil.FieldAttributes]::Private -bor [Mono.Cecil.FieldAttributes]::Static), $target.TypeSystem.Int32)
$type.Fields.Add($publicField); $type.Fields.Add($privateField)
$target.Write((Join-Path $root 'AccessFixtureTarget.dll'))
$checked = 0
foreach ($case in @('public','private','unresolved')) {
    $consumer = [Mono.Cecil.ModuleDefinition]::CreateModule(('Fixture-' + $case), [Mono.Cecil.ModuleKind]::Dll)
    $caller = [Mono.Cecil.TypeDefinition]::new('Fixture', 'Caller', [Mono.Cecil.TypeAttributes]::Public, $consumer.TypeSystem.Object)
    $consumer.Types.Add($caller)
    $method = [Mono.Cecil.MethodDefinition]::new('Read', ([Mono.Cecil.MethodAttributes]::Public -bor [Mono.Cecil.MethodAttributes]::Static), $consumer.TypeSystem.Int32)
    $caller.Methods.Add($method)
    $field = $publicField; if ($case -eq 'private') { $field = $privateField }
    $il = $method.Body.GetILProcessor()
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld, $consumer.ImportReference($field)); $il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
    if ($case -eq 'unresolved') {
        $consumer.AssemblyReferences.Add([Mono.Cecil.AssemblyNameReference]::new('DeliberatelyMissingDependency', [version]'1.0.0.0'))
    }
    $path = Join-Path $root ($case + '.dll'); $consumer.Write($path); $consumer.Dispose()
    $result = Join-Path $root ($case + '.json')
    & dotnet $ScannerDll $path $GameManaged $result $root
    $code = $LASTEXITCODE
    $scan = Get-Content -LiteralPath $result -Raw | ConvertFrom-Json
    if ($case -eq 'public' -and ($code -ne 0 -or $scan.status -ne 'passed' -or $scan.checkedMembers -lt 1)) { throw 'Public access fixture failed.' }
    if ($case -eq 'private' -and ($code -eq 0 -or @($scan.inaccessible | Where-Object { $_ -match 'Hidden' }).Count -ne 1)) { throw 'Private access fixture was skipped.' }
    if ($case -eq 'unresolved' -and ($code -eq 0 -or @($scan.unresolved | Where-Object { $_ -match 'DeliberatelyMissingDependency' }).Count -ne 1)) { throw 'Unresolved assembly fixture was skipped.' }
    $checked++
}
$target.Dispose()
$game = Split-Path -Parent (Split-Path -Parent $GameManaged)
$runtime = @{ unityPlayer = @{ path = Join-Path $game 'UnityPlayer.dll'; sha256 = (Get-FileHash (Join-Path $game 'UnityPlayer.dll')).Hash };
    mono = @{ path = Join-Path $game 'MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll'; sha256 = (Get-FileHash (Join-Path $game 'MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll')).Hash } }
$evidence = Join-Path $root 'review-evidence.txt'; 'Synthetic reviewed private fixture; not a runtime test.' | Set-Content -LiteralPath $evidence
$reviewed = @((Get-Content (Join-Path $root 'private.json') -Raw | ConvertFrom-Json).inaccessible)
function Add-FixtureDeclaration($Module, [bool]$Skip) {
    $attributeType = [Mono.Cecil.TypeReference]::new('System.Security.Permissions','SecurityPermissionAttribute',$Module,$Module.TypeSystem.CoreLibrary)
    $attribute = [Mono.Cecil.SecurityAttribute]::new($attributeType)
    $argument = [Mono.Cecil.CustomAttributeArgument]::new($Module.TypeSystem.Boolean, $Skip)
    $attribute.Properties.Add([Mono.Cecil.CustomAttributeNamedArgument]::new('SkipVerification', $argument))
    $declaration = [Mono.Cecil.SecurityDeclaration]::new([Mono.Cecil.SecurityAction]::RequestMinimum)
    $declaration.SecurityAttributes.Add($attribute); $Module.Assembly.SecurityDeclarations.Add($declaration)
    $unverifiable = [Mono.Cecil.TypeReference]::new('System.Security','UnverifiableCodeAttribute',$Module,$Module.TypeSystem.CoreLibrary)
    $ctor = [Mono.Cecil.MethodReference]::new('.ctor',$Module.TypeSystem.Void,$unverifiable); $ctor.HasThis = $true
    $Module.CustomAttributes.Add([Mono.Cecil.CustomAttribute]::new($ctor))
}
foreach ($case in @('supported','declared-strict','absent-declaration','false-declaration','absent-module','wrong-runtime','wrong-candidate','unreviewed-site','policy-unresolved')) {
    $source = Join-Path $root 'private.dll'
    $module = [Mono.Cecil.ModuleDefinition]::ReadModule($source)
    if ($case -eq 'policy-unresolved') { $module.AssemblyReferences.Add([Mono.Cecil.AssemblyNameReference]::new('DeliberatelyMissingDependency', [version]'1.0.0.0')) }
    if ($case -ne 'absent-declaration') { Add-FixtureDeclaration $module ($case -ne 'false-declaration') }
    if ($case -eq 'absent-module') { $module.CustomAttributes.Clear() }
    $path = Join-Path $root ($case + '.dll'); $module.Write($path); $module.Dispose()
    $sites = $reviewed
    if ($case -eq 'unreviewed-site') { $sites = @('unreviewed placeholder') }
    $policy = @{ schema = 1; policy = 'unity-mono-requestminimum-skipverification-v1'; candidateSha256 = (Get-FileHash $path).Hash;
        reviewEvidence = @{ path = $evidence; sha256 = (Get-FileHash $evidence).Hash }; runtime = $runtime; reviewedSites = $sites }
    $policyPath = Join-Path $root ($case + '-policy.json')
    if ($case -eq 'wrong-runtime') { $policy.runtime = $runtime | ConvertTo-Json -Depth 5 | ConvertFrom-Json; $policy.runtime.mono.sha256 = ('0' * 64) }
    if ($case -eq 'wrong-candidate') { $policy.candidateSha256 = ('0' * 64) }
    $policy | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $policyPath
    $result = Join-Path $root ($case + '.json')
    $policyArgs = @('--policy',$policyPath); if ($case -eq 'declared-strict') { $policyArgs = @() }
    & dotnet $ScannerDll $path $GameManaged $result $root @policyArgs
    $code = $LASTEXITCODE; $scan = Get-Content -LiteralPath $result -Raw | ConvertFrom-Json
    if ($case -eq 'supported') {
        if ($code -ne 0 -or @($scan.inaccessible).Count -ne 1 -or @($scan.supported).Count -ne 1 -or @($scan.unsupported).Count -ne 0) { throw 'Reviewed policy fixture failed or hid raw sites.' }
    } elseif ($code -eq 0 -or $scan.status -ne 'failed') { throw "Unsafe policy case passed: $case" }
    if ($case -eq 'policy-unresolved' -and @($scan.unresolved).Count -eq 0) { throw 'Policy hid unresolved reference.' }
    $checked++
}
Write-Output "ACCESS_FIXTURES_PASS cases=$checked evidence=$root"
