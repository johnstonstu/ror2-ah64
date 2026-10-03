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
Write-Output "ACCESS_FIXTURES_PASS cases=$checked evidence=$root"
