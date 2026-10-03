$ErrorActionPreference = 'Stop'
$brakingRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$brakingEvidence = Join-Path $brakingRoot 'dist/braking-turn'
$env:DOTNET_CLI_HOME = Join-Path $brakingEvidence 'dotnet-home'
$env:NUGET_PACKAGES = Join-Path $brakingEvidence 'nuget-packages'
$env:NUGET_HTTP_CACHE_PATH = Join-Path $brakingEvidence 'nuget-http'
$env:NUGET_PLUGINS_CACHE_PATH = Join-Path $brakingEvidence 'nuget-plugins'
$env:NUGET_SCRATCH = Join-Path $brakingEvidence 'nuget-scratch'
$env:TEMP = Join-Path $brakingEvidence 'temp'
$env:TMP = $env:TEMP
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE = '1'
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
$env:MSBUILDDISABLENODEREUSE = '1'
$env:UseSharedCompilation = 'false'
New-Item -ItemType Directory -Force -Path $env:DOTNET_CLI_HOME,$env:NUGET_PACKAGES,
    $env:NUGET_HTTP_CACHE_PATH,$env:NUGET_PLUGINS_CACHE_PATH,$env:NUGET_SCRATCH,$env:TEMP | Out-Null
