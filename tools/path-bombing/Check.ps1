$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Set-Environment.ps1')
$checksOutput = Join-Path $bombOutput 'checks'
New-Item -ItemType Directory -Force -Path $checksOutput | Out-Null
$sources = @('tools/path-bombing/Checks.cs', 'tools/path-bombing/Stubs.cs',
    'tools/path-bombing/CompletionChecks.cs', 'tools/path-bombing/ImpactChecks.cs',
    'tools/path-bombing/TerminalLifecycleChecks.cs',
    'tools/path-bombing/EntryChecks.cs',
    'AH64Mod/Characters/Survivors/AH64/SkillStates/BombingRun.cs',
    'AH64Mod/Characters/Survivors/AH64/Content/AH64BombingRunStaticValues.cs')
$sources += Get-ChildItem (Join-Path $bombRepo 'AH64Mod/Characters/Survivors/AH64/Components/AH64BombingRun*.cs') |
    ForEach-Object { [IO.Path]::GetRelativePath($bombRepo, $_.FullName) }
$includes = ($sources | ForEach-Object { '<Compile Include="' + [Security.SecurityElement]::Escape((Join-Path $bombRepo $_)) + '" />' }) -join "`n"
$project = Join-Path $checksOutput 'Checks.csproj'
@"
<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems><LangVersion>7.3</LangVersion></PropertyGroup>
<ItemGroup>$includes</ItemGroup>
</Project>
"@ | Set-Content -LiteralPath $project
dotnet run --project $project -c Release -p:NuGetAudit=false -p:UseSharedCompilation=false -p:AH64DeployToProfiles=false -nodeReuse:false
if ($LASTEXITCODE -ne 0) { throw 'Path bombing API-double checks failed.' }
