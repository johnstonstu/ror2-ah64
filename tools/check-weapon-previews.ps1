$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$output = Join-Path $repo 'dist/weapon-checks'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$sources = @('tools/WeaponPreviewStubs.cs', 'tools/WeaponPreviewChecks.cs',
    'AH64Mod/Characters/Survivors/AH64/Components/AH64PrimaryWeaponVisuals.cs',
    'AH64Mod/Characters/Survivors/AH64/Components/AH64LobbyWeaponPreview.cs')
$includes = ($sources | ForEach-Object { '<Compile Include="' + [System.Security.SecurityElement]::Escape((Join-Path $repo $_)) + '" />' }) -join "`n"
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup>
<ItemGroup>$includes</ItemGroup>
</Project>
"@
$path = Join-Path $output 'WeaponPreviewChecks.csproj'
if (Test-Path $path) { Get-Content $path | Out-Null }
Set-Content -LiteralPath $path -Value $project
dotnet run --project $path -c Release
if ($LASTEXITCODE -ne 0) { throw 'Weapon preview lifecycle checks failed.' }
