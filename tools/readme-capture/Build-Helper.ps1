param([string]$FrozenDll = "$PSScriptRoot\..\..\..\ah64-1.3.0-integration\dist\utility-audio-refinement\f9cb6ce\AH64.dll")
$ErrorActionPreference = 'Stop'
$expected = 'DEB8659CB3D1DAE028B8525D4E6C3EADA5C88DE5DD9FA57E1AA98846BD02D266'
if ((Get-FileHash -LiteralPath $FrozenDll -Algorithm SHA256).Hash -ne $expected) { throw 'Frozen AH64 hash mismatch' }
$out = [IO.Path]::GetFullPath("$PSScriptRoot\..\..\dist\readme-capture-helper")
New-Item -ItemType Directory -Force $out | Out-Null
$cache = "$env:USERPROFILE\.nuget\packages"
$roots = @('riskofrain2.gamelibs\1.4.1-r.0\lib\netstandard2.0','unityengine.modules\2021.3.33\lib','bepinex.baselib\5.4.20\lib\netstandard2.0','mmhook.ror2\2025.12.9\lib')
$refs = foreach ($root in $roots) { Get-ChildItem -LiteralPath "$cache\$root" -Recurse -Filter '*.dll' }
$xmlrefs = ($refs | Group-Object Name | ForEach-Object { $f=$_.Group[0]; '<Reference Include="'+$f.BaseName+'"><HintPath>'+[Security.SecurityElement]::Escape($f.FullName)+'</HintPath><Private>false</Private></Reference>' }) -join "`n"
$sources = (Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' | ForEach-Object { '<Compile Include="'+[Security.SecurityElement]::Escape($_.FullName)+'" />' }) -join "`n"
@"
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>netstandard2.1</TargetFramework><LangVersion>7.3</LangVersion><AssemblyName>AH64.ReadmeCapture</AssemblyName><EnableDefaultCompileItems>false</EnableDefaultCompileItems><RestoreSources></RestoreSources></PropertyGroup><ItemGroup>$xmlrefs$sources</ItemGroup></Project>
"@ | Set-Content -LiteralPath "$out\Capture.csproj"
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content -LiteralPath "$out\NuGet.Config"
$priorAppData=$env:APPDATA; $env:APPDATA=$out
& dotnet build "$out\Capture.csproj" -c Release --configfile "$out\NuGet.Config" --nologo
$buildExit=$LASTEXITCODE; $env:APPDATA=$priorAppData
if ($buildExit -ne 0) { throw 'Capture helper build failed' }
if ((Get-FileHash -LiteralPath $FrozenDll -Algorithm SHA256).Hash -ne $expected) { throw 'Frozen AH64 changed' }
Get-FileHash -LiteralPath "$out\bin\Release\netstandard2.1\AH64.ReadmeCapture.dll" -Algorithm SHA256
