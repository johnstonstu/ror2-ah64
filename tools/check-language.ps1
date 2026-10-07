$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$output = Join-Path $repo 'dist/language-checks'
New-Item -ItemType Directory -Force -Path $output | Out-Null

$json = Join-Path $repo 'AH64Mod/Modules/LanguageJson.cs'
$fallback = Join-Path $repo 'AH64Mod/Modules/LanguageFallback.cs'
$recoveryChecks = Join-Path $PSScriptRoot 'LanguageFallbackChecks.cs'
$checks = Join-Path $PSScriptRoot 'LanguageChecks.cs'
if (-not (Test-Path -LiteralPath $json)) { throw "LanguageJson.cs not found at $json" }
if (-not (Test-Path -LiteralPath $checks)) { throw "LanguageChecks.cs not found at $checks" }

$includes = @(
    '<Compile Include="' + [System.Security.SecurityElement]::Escape($json) + '" />',
    '<Compile Include="' + [System.Security.SecurityElement]::Escape($fallback) + '" />',
    '<Compile Include="' + [System.Security.SecurityElement]::Escape($recoveryChecks) + '" />',
    '<Compile Include="' + [System.Security.SecurityElement]::Escape($checks) + '" />'
) -join "`n"
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>disable</Nullable>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup>
    $includes
  </ItemGroup>
</Project>
"@
$projectPath = Join-Path $output 'LanguageChecks.csproj'
Set-Content -LiteralPath $projectPath -Value $project -Encoding utf8
dotnet run --project $projectPath --configuration Release -- $repo
if ($LASTEXITCODE -ne 0) { throw 'Language checks failed.' }
