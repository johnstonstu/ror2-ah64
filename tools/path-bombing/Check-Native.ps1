[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$BaselineScan,
      [Parameter(Mandatory=$true)][string[]]$DependencyDirectories)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Set-Environment.ps1')
$nativeOutput = Join-Path $bombOutput ('native-' + [guid]::NewGuid().ToString('N'))
$scan = & (Join-Path $bombRepo 'tools/dev-profile/Check-Access.ps1') `
    -Dll (Join-Path $bombRepo 'AH64Mod/bin/Release/netstandard2.1/AH64.dll') `
    -DependencyDirectories $DependencyDirectories -OutputDirectory $nativeOutput -PassThru
$result = Get-Content -LiteralPath $scan.resultPath -Raw | ConvertFrom-Json
$baseline = Get-Content -LiteralPath $BaselineScan -Raw | ConvertFrom-Json
$delta = @(Compare-Object -CaseSensitive @($baseline.inaccessible) @($result.inaccessible))
$summary = [ordered]@{
    schema=1; dllSha256=(Get-FileHash (Join-Path $bombRepo 'AH64Mod/bin/Release/netstandard2.1/AH64.dll')).Hash
    strictExitCode=$scan.exitCode; baseline=$BaselineScan; scan=$scan.resultPath
    unresolved=@($result.unresolved).Count; inaccessible=@($result.inaccessible).Count
    inaccessibleDelta=$delta; nativeRuntimeVerified=$false
}
$summary | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $nativeOutput 'comparison.json')
if ($summary.unresolved -ne 0 -or $delta.Count -ne 0) { throw "Native API delta failed: $nativeOutput" }
Write-Output ($summary | ConvertTo-Json -Depth 8)
# Existing inaccessible baseline sites are NOT newly authorized by this comparison.
