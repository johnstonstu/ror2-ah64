# Compile only. Deliberately does not import AH64.csproj's staging/deployment PostBuild target.
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$out = Join-Path $repo 'dist/bombing-visuals/compile'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$env:DOTNET_CLI_HOME = Join-Path $out 'dotnet-home'
$env:NUGET_PACKAGES = Join-Path $out 'packages'
$env:NUGET_HTTP_CACHE_PATH = Join-Path $out 'http-cache'
$env:NUGET_PLUGINS_CACHE_PATH = Join-Path $out 'plugin-cache'
$env:NUGET_SCRATCH = Join-Path $out 'nuget-scratch'
$env:TEMP = Join-Path $out 'temp'
$env:TMP = $env:TEMP
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE = '1'
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
$env:MSBUILDDISABLENODEREUSE = '1'
New-Item -ItemType Directory -Force -Path $env:DOTNET_CLI_HOME,$env:NUGET_PACKAGES,$env:NUGET_HTTP_CACHE_PATH,$env:NUGET_PLUGINS_CACHE_PATH,$env:NUGET_SCRATCH,$env:TEMP | Out-Null
[xml]$source = Get-Content -LiteralPath (Join-Path $repo 'AH64Mod/AH64.csproj') -Raw
# Floating versions need a source index even when cached. Pin only the temporary compile
# project's wildcard references to the already-installed versions; production project is untouched.
$pins = @{ 'BepInEx.Analyzers'='1.0.8'; 'BepInEx.Core'='5.4.21'; 'R2API.Core'='5.3.0'; 'R2API.Prefab'='1.1.1'; 'R2API.RecalculateStats'='1.6.6'; 'R2API.Language'='1.1.0'; 'R2API.Sound'='1.0.3' }
foreach ($reference in $source.Project.ItemGroup.PackageReference) {
    if ($reference -and $pins.ContainsKey($reference.Include)) { $reference.Version = $pins[$reference.Include] }
}
$refs = ($source.Project.ItemGroup.PackageReference | Where-Object { $_ } | ForEach-Object { $_.OuterXml }) -join "`n"
$includes = (Get-ChildItem -LiteralPath (Join-Path $repo 'AH64Mod') -Recurse -File -Filter '*.cs' |
    Where-Object { $_.FullName -notmatch '[\\/](obj|bin)[\\/]' } |
    ForEach-Object { '<Compile Include="' + [Security.SecurityElement]::Escape($_.FullName) + '" />' }) -join "`n"
$fallback = [Security.SecurityElement]::Escape((Join-Path $env:USERPROFILE '.nuget/packages'))
@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework><LangVersion>7.3</LangVersion>
    <AssemblyName>AH64</AssemblyName><RootNamespace>AH64</RootNamespace>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <AH64DeployToProfiles>false</AH64DeployToProfiles><NuGetAudit>false</NuGetAudit>
    <RestoreFallbackFolders>$fallback</RestoreFallbackFolders>
  </PropertyGroup>
  <ItemGroup>$refs</ItemGroup>
  <ItemGroup>$includes</ItemGroup>
</Project>
"@ | Set-Content -LiteralPath (Join-Path $out 'Compile.csproj')
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content -LiteralPath (Join-Path $out 'NuGet.Config')
dotnet build (Join-Path $out 'Compile.csproj') -c Release --configfile (Join-Path $out 'NuGet.Config') -p:AH64DeployToProfiles=false -p:UseSharedCompilation=false -nodeReuse:false 2>&1 | Tee-Object -FilePath (Join-Path $out 'build.log')
if ($LASTEXITCODE -ne 0) { throw 'Bombing visual source compilation failed.' }
Get-FileHash -Algorithm SHA256 (Join-Path $out 'bin/Release/netstandard2.1/AH64.dll')
