$ErrorActionPreference = 'Stop'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo ('dist/capture-tests/' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output | Out-Null
$capture = [Security.SecurityElement]::Escape((Join-Path $repo 'AH64Mod/DevAutopilot.Capture.cs'))
$fixtures = [Security.SecurityElement]::Escape((Join-Path $PSScriptRoot 'CaptureChecks.cs'))
$project = Join-Path $output 'CaptureChecks.csproj'
@"
<Project Sdk="Microsoft.NET.Sdk">
 <PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems><LangVersion>7.3</LangVersion></PropertyGroup>
 <ItemGroup><Compile Include="$capture"/><Compile Include="$fixtures"/></ItemGroup>
</Project>
"@ | Set-Content -LiteralPath $project
& dotnet restore $project --ignore-failed-sources -p:NuGetAudit=false *> (Join-Path $output 'restore.log')
if ($LASTEXITCODE -ne 0) { throw "Capture checks restore failed: $output" }
& dotnet build $project -c Release --no-restore -p:UseSharedCompilation=false -nodeReuse:false *> (Join-Path $output 'build.log')
if ($LASTEXITCODE -ne 0) { throw "Capture checks build failed: $output" }
& dotnet (Join-Path $output 'bin/Release/net8.0/CaptureChecks.dll') *> (Join-Path $output 'checks.log')
$code = $LASTEXITCODE
Get-Content -LiteralPath (Join-Path $output 'checks.log')
Write-Output "capture-evidence=$output"
if ($code -ne 0) { throw "Capture checks failed: $output" }
