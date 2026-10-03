$ErrorActionPreference = 'Stop'
$bombRepo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$bombOutput = Join-Path $bombRepo 'dist/path-bombing'
$env:DOTNET_CLI_HOME = Join-Path $bombOutput 'dotnet-home'
$env:NUGET_PACKAGES = Join-Path $bombOutput 'nuget-packages'
$env:NUGET_HTTP_CACHE_PATH = Join-Path $bombOutput 'nuget-http-cache'
$env:NUGET_PLUGINS_CACHE_PATH = Join-Path $bombOutput 'nuget-plugin-cache'
$env:NUGET_SCRATCH = Join-Path $bombOutput 'nuget-scratch'
$env:TEMP = Join-Path $bombOutput 'temp'
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
