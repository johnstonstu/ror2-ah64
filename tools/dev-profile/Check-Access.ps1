[CmdletBinding()]
param(
    [string]$Dll,
    [string]$GameManaged = 'C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed',
    [string[]]$DependencyDirectories = @(),
    [string]$CecilPath,
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if (!$Dll) { $Dll = Join-Path $repo 'AH64Mod/bin/Release/netstandard2.1/AH64.dll' }
if (!$OutputDirectory) { $OutputDirectory = Join-Path $repo ('dist/access/' + [guid]::NewGuid().ToString('N')) }
foreach ($path in @($Dll, (Join-Path $GameManaged 'RoR2.dll'))) {
    if (!(Test-Path -LiteralPath $path -PathType Leaf)) { throw "Required input missing: $path" }
}
foreach ($path in $DependencyDirectories) {
    if (!(Test-Path -LiteralPath $path -PathType Container)) { throw "Dependency directory missing: $path" }
    if ($path -match 'riskofrain2\.gamelibs') { throw 'Publicized GameLibs is forbidden in an actual-game scan.' }
}
if ($GameManaged -match 'riskofrain2\.gamelibs') { throw 'Scan requires installed game assemblies.' }
if (!$CecilPath) {
    $assets = Get-Content (Join-Path $repo 'AH64Mod/obj/project.assets.json') -Raw | ConvertFrom-Json
    $package = $assets.libraries.PSObject.Properties | Where-Object Name -Like 'Mono.Cecil/*' | Select-Object -First 1
    if (!$package) { throw 'Restore first or supply -CecilPath.' }
    $CecilPath = Join-Path ($assets.packageFolders.PSObject.Properties.Name | Select-Object -First 1) ($package.Value.path + '/lib/netstandard2.0/Mono.Cecil.dll')
}
if (!(Test-Path -LiteralPath $CecilPath -PathType Leaf)) { throw "Cecil missing: $CecilPath" }
if (Test-Path -LiteralPath $OutputDirectory) { throw 'Access output must be new.' }
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$source = [Security.SecurityElement]::Escape((Join-Path $PSScriptRoot 'AccessScan.cs'))
$cecil = [Security.SecurityElement]::Escape((Resolve-Path $CecilPath).Path)
$project = Join-Path $OutputDirectory 'AccessScan.csproj'
@"
<Project Sdk="Microsoft.NET.Sdk">
 <PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup>
 <ItemGroup><Compile Include="$source"/><Reference Include="Mono.Cecil"><HintPath>$cecil</HintPath></Reference></ItemGroup>
</Project>
"@ | Set-Content -LiteralPath $project
# No package downloads: this helper only references the already restored Cecil assembly.
& dotnet restore $project --ignore-failed-sources -p:NuGetAudit=false *> (Join-Path $OutputDirectory 'restore.log')
if ($LASTEXITCODE -ne 0) { throw 'Access scanner restore failed; see restore.log.' }
& dotnet build $project -c Release --no-restore -p:UseSharedCompilation=false -nodeReuse:false *> (Join-Path $OutputDirectory 'build.log')
if ($LASTEXITCODE -ne 0) { throw 'Access scanner build failed; see build.log.' }
& dotnet (Join-Path $OutputDirectory 'bin/Release/net8.0/AccessScan.dll') $Dll $GameManaged (Join-Path $OutputDirectory 'access.json') @DependencyDirectories
$code = $LASTEXITCODE
Write-Output "access-result=$(Join-Path $OutputDirectory 'access.json') exit=$code"
exit $code
