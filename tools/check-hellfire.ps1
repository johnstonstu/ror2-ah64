$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$output = Join-Path $repo 'dist/hellfire-checks'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$sources = @('tools/HellfireUnityStubs.cs', 'tools/HellfireNetworkStubs.cs', 'tools/HellfireChecks.cs',
    'tools/HellfireLifecycleChecks.cs',
    'tools/HellfireRegressionChecks.cs',
    'AH64Mod/Characters/Survivors/AH64/SkillStates/FireHellfire.cs')
$sources += Get-ChildItem (Join-Path $repo 'AH64Mod/Characters/Survivors/AH64/Components') -Filter 'AH64Hellfire*.cs' |
    ForEach-Object { $_.FullName }
$includes = ($sources | ForEach-Object {
    $sourcePath = if ([IO.Path]::IsPathRooted($_)) { $_ } else { Join-Path $repo $_ }
    '<Compile Include="' + [System.Security.SecurityElement]::Escape($sourcePath) + '" />'
}) -join "`n"
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseSharedCompilation>false</UseSharedCompilation><NuGetAudit>false</NuGetAudit></PropertyGroup>
<ItemGroup>$includes</ItemGroup>
</Project>
"@
$path = Join-Path $output 'HellfireChecks.csproj'
if (Test-Path $path) { Get-Content $path | Out-Null }
Set-Content -LiteralPath $path -Value $project
dotnet run --project $path -c Release --disable-build-servers -p:AH64DeployToProfiles=false -p:UseSharedCompilation=false -nodeReuse:false
if ($LASTEXITCODE -ne 0) { throw 'Hellfire offline checks failed.' }
