$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$output = Join-Path $repo 'dist/movement-checks'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$sources = @('tools/movement/MovementStubs.cs', 'tools/movement/VisualStubs.cs', 'tools/movement/MovementChecks.cs',
    'AH64Mod/Characters/Survivors/AH64/Content/AH64StaticValues.cs',
    'AH64Mod/Characters/Survivors/AH64/Components/AH64ManeuverCapture.cs',
    'AH64Mod/Characters/Survivors/AH64/Components/AH64ManeuverMotor.cs',
    'AH64Mod/Characters/Survivors/AH64/SkillStates/ServoDash.cs',
    'AH64Mod/Characters/Survivors/AH64/SkillStates/SmokeBackflip.cs')
$includes = ($sources | ForEach-Object { '<Compile Include="' + [System.Security.SecurityElement]::Escape((Join-Path $repo $_)) + '" />' }) -join "`n"
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseSharedCompilation>false</UseSharedCompilation></PropertyGroup>
<ItemGroup>$includes</ItemGroup>
</Project>
"@
$path = Join-Path $output 'MovementChecks.csproj'
if (Test-Path $path) { Get-Content $path | Out-Null }
Set-Content -LiteralPath $path -Value $project
dotnet run --project $path -c Release --disable-build-servers /p:UseSharedCompilation=false /nodeReuse:false
if ($LASTEXITCODE -ne 0) { throw 'Movement offline checks failed.' }

$sources = @('tools/movement/MovementStubs.cs', 'tools/movement/VisualStubs.cs', 'tools/movement/VisualChecks.cs',
    'AH64Mod/Characters/Survivors/AH64/Content/AH64StaticValues.cs',
    'AH64Mod/Characters/Survivors/AH64/Components/AH64ManeuverCapture.cs',
    'AH64Mod/Characters/Survivors/AH64/Components/AH64FlightVisuals.cs')
$includes = ($sources | ForEach-Object { '<Compile Include="' + [System.Security.SecurityElement]::Escape((Join-Path $repo $_)) + '" />' }) -join "`n"
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseSharedCompilation>false</UseSharedCompilation><DefineConstants>VISUALS</DefineConstants></PropertyGroup>
<ItemGroup>$includes</ItemGroup>
</Project>
"@
$path = Join-Path $output 'VisualChecks.csproj'
if (Test-Path $path) { Get-Content $path | Out-Null }
Set-Content -LiteralPath $path -Value $project
dotnet run --project $path -c Release --disable-build-servers /p:UseSharedCompilation=false /nodeReuse:false
if ($LASTEXITCODE -ne 0) { throw 'Movement visual offline checks failed.' }
