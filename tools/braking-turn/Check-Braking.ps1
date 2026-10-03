[CmdletBinding()]
param([string]$SourceRoot, [string]$OutputDirectory)
. (Join-Path $PSScriptRoot 'Set-Environment.ps1')
if (!$SourceRoot) { $SourceRoot = $brakingRoot }
if (!$OutputDirectory) { $OutputDirectory = Join-Path $brakingEvidence ('checks-' + [guid]::NewGuid().ToString('N')) }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$sources = @(
    (Join-Path $PSScriptRoot 'BrakingStubs.cs'),
    (Join-Path $PSScriptRoot 'BrakingChecks.cs'),
    (Join-Path $SourceRoot 'AH64Mod/Characters/Survivors/AH64/SkillStates/AH64Main.cs'),
    (Join-Path $SourceRoot 'AH64Mod/Characters/Survivors/AH64/SkillStates/BrakingTurn.cs'),
    (Join-Path $SourceRoot 'AH64Mod/Characters/Survivors/AH64/Content/AH64BrakingTurnStaticValues.cs')
)
$sources += Get-ChildItem (Join-Path $SourceRoot 'AH64Mod/Characters/Survivors/AH64/Components/AH64BrakingTurn*.cs') | ForEach-Object FullName
$includes = ($sources | ForEach-Object { '<Compile Include="' + [Security.SecurityElement]::Escape($_) + '"/>' }) -join "`n"
$project = Join-Path $OutputDirectory 'BrakingChecks.csproj'
@"
<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseSharedCompilation>false</UseSharedCompilation></PropertyGroup>
<ItemGroup>$includes</ItemGroup>
</Project>
"@ | Set-Content -LiteralPath $project
& dotnet run --project $project -c Release --disable-build-servers /p:AH64DeployToProfiles=false /p:UseSharedCompilation=false /nodeReuse:false
if ($LASTEXITCODE -ne 0) { throw 'Braking checks failed.' }
