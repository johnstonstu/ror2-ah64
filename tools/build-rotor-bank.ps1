<#
Build only the AH64Rotor bank. Never distribute the generated Init.bnk:
the rotor resolves SFX_BUS against RoR2's existing audio hierarchy.
#>
[CmdletBinding()]
param(
    [string]$WwiseConsole = 'C:\Audiokinetic\Wwise_2023.1.4.8496\Authoring\x64\Release\bin\WwiseConsole.exe',
    [switch]$ValidateOnly
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$projectDir = Join-Path $repo 'Art\Wwise\AH64Audio'
$project = Join-Path $projectDir 'AH64Audio.wproj'
$generated = Join-Path $projectDir 'GeneratedSoundBanks\Windows'
$bank = Join-Path $generated 'AH64Rotor.bnk'

if (-not $ValidateOnly) {
    if (-not (Test-Path -LiteralPath $WwiseConsole)) { throw "Wwise 2023.1.4.8496 not found: $WwiseConsole" }
    & $WwiseConsole generate-soundbank $project --bank AH64Rotor --platform Windows --header-file --abort-on-load-issues
    if ($LASTEXITCODE -ne 0) { throw "Wwise bank generation failed: $LASTEXITCODE" }
}
if (-not (Test-Path -LiteralPath $bank)) { throw 'AH64Rotor.bnk missing; run tools/build-rotor-bank.ps1.' }
$inputs = Get-ChildItem -LiteralPath $projectDir -Recurse -File | Where-Object {
    $_.Extension -in '.wproj', '.wwu', '.wav' -and $_.FullName -notmatch '\\(\.cache|GeneratedSoundBanks|\.backup)\\'
}
$newest = $inputs | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
if ($newest.LastWriteTimeUtc -gt (Get-Item -LiteralPath $bank).LastWriteTimeUtc) {
    throw "Rotor bank is older than $($newest.FullName). Rebuild it."
}
[xml]$info = Get-Content -LiteralPath (Join-Path $generated 'SoundbanksInfo.xml') -Raw
if ($info.SoundBanksInfo.SoundBankVersion -ne '150') { throw 'Rotor bank format must match RoR2 format 150.' }
$definition = @($info.SoundBanksInfo.SoundBanks.SoundBank | Where-Object ShortName -eq 'AH64Rotor')
if ($definition.Count -ne 1) { throw 'Expected exactly one AH64Rotor bank.' }
$events = @($definition[0].Events.Event | ForEach-Object Name)
if ($events.Count -ne 2 -or 'Play_AH64_Rotor' -notin $events -or 'Stop_AH64_Rotor' -notin $events) {
    throw 'Rotor play/stop events missing or unexpected extra events present.'
}
$media = @($definition[0].Media.File)
if ($media.Count -ne 1 -or $media[0].Streaming -ne 'false' -or $media[0].Location -ne 'Memory') {
    throw 'Rotor must have exactly one embedded, non-streaming recording.'
}
$bytes = [IO.File]::ReadAllBytes($bank)
if ([Text.Encoding]::ASCII.GetString($bytes, 0, 4) -ne 'BKHD' -or
    [BitConverter]::ToUInt32($bytes, 8) -ne 150 -or
    [BitConverter]::ToUInt32($bytes, 12) -ne 2651907208) { throw 'Rotor bank header/ID mismatch.' }
$chunks = @{}
for ($offset = 0; $offset -lt $bytes.Length;) {
    if ($offset + 8 -gt $bytes.Length) { throw 'Truncated bank chunk header.' }
    $tag = [Text.Encoding]::ASCII.GetString($bytes, $offset, 4)
    $size = [BitConverter]::ToUInt32($bytes, $offset + 4)
    $offset += 8
    if ($offset + $size -gt $bytes.Length) { throw "Truncated bank chunk: $tag" }
    $chunks[$tag] = $size
    $offset += $size
}
if ($chunks['DIDX'] -ne 12 -or $chunks['DATA'] -lt 200000 -or $chunks['HIRC'] -le 0) {
    throw 'Bank is missing the expected embedded PCM recording or event structures.'
}
if (-not $ValidateOnly) {
    $destination = Join-Path $repo 'Build\plugins\SoundBanks'
    New-Item -ItemType Directory -Force -Path $destination | Out-Null
    Copy-Item -LiteralPath $bank -Destination (Join-Path $destination 'AH64Rotor.bnk') -Force
}
Write-Host "Validated AH64Rotor: format 150, two events, one embedded recording, $($bytes.Length) bytes."
