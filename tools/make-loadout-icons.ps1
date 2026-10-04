# Reproduce DLL icon assets and the review sheet from approved source artwork.
# No Unity, assetbundle build, profile installation, or game launch is involved.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$output = Join-Path $root 'AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$bundle = Join-Path $root 'AH64UnityProject/Assets/AH64/Bundle/Icons'
$sheet = [Drawing.Image]::FromFile((Join-Path $root 'Art/Icons/loadout-attachments-source.png'))
$entries = @(
    @('AH64Chaingun', 'M230', 'texAH64PrimaryIcon.png'),
    @('AH64Gatling', 'Gatling', 'texAH64GatlingIcon.png'),
    @('AH64Cannon', 'HE cannon', 'texAH64CannonIcon.png'),
    @('AH64RocketPods', 'Hydra', 'texAH64SecondaryIcon.png'),
    @('AH64EvasiveJink', 'Evasive Jink', 3),
    @('AH64SmokeBackflip', 'Smoke Backflip', 4),
    @('AH64BrakingTurn', 'Banked Break', 5),
    @('AH64Longbow', 'Longbow', 0),
    @('AH64Hellfire', 'Hellfire', 1),
    @('AH64BombingRun', 'Bombing Run', 2)
)
$preview = New-Object Drawing.Bitmap 1200, 640
$pg = [Drawing.Graphics]::FromImage($preview)
$pg.Clear([Drawing.Color]::FromArgb(19, 20, 20))
$font = New-Object Drawing.Font 'Segoe UI', 12
try {
    for ($i = 0; $i -lt $entries.Count; $i++) {
        $entry = $entries[$i]
        $image = $sheet
        $ownsSource = $entry[2] -is [string]
        if ($ownsSource) {
            $image = [Drawing.Image]::FromFile((Join-Path $bundle $entry[2]))
            $rect = New-Object Drawing.Rectangle 0, 0, $image.Width, $image.Height
        } else {
            $cell = [int]$entry[2]
            $size = [int]($sheet.Width / 3)
            if ($sheet.Height -ne 2 * $size) { throw 'Expected a 3 by 2 square icon sheet.' }
            $rect = New-Object Drawing.Rectangle (($cell % 3) * $size), ([int][Math]::Floor($cell / 3) * $size), $size, $size
        }
        $icon = New-Object Drawing.Bitmap 512, 512
        $g = [Drawing.Graphics]::FromImage($icon)
        try {
            $g.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $g.DrawImage($image, (New-Object Drawing.Rectangle 0, 0, 512, 512), $rect, [Drawing.GraphicsUnit]::Pixel)
            $icon.Save((Join-Path $output ($entry[0] + '.png')), [Drawing.Imaging.ImageFormat]::Png)
            $x = ($i % 5) * 240 + 8
            $y = [int][Math]::Floor($i / 5) * 320 + 6
            $pg.DrawImage($icon, $x, $y, 224, 224)
            $pg.DrawString($entry[1], $font, [Drawing.Brushes]::WhiteSmoke, $x, ($y + 230))
            # HUD-size thumbnail beside a 64px loadout thumbnail exposes readability problems.
            $pg.DrawImage($icon, $x, ($y + 253), 48, 48)
            $pg.DrawImage($icon, ($x + 58), ($y + 249), 64, 64)
        } finally {
            $g.Dispose()
            $icon.Dispose()
            if ($ownsSource) { $image.Dispose() }
        }
    }
    $preview.Save((Join-Path $root 'Art/Icons/loadout-contact-sheet.png'), [Drawing.Imaging.ImageFormat]::Png)
} finally {
    $font.Dispose()
    $pg.Dispose()
    $preview.Dispose()
    $sheet.Dispose()
}
Write-Host 'Wrote ten 512px embedded icons and Art/Icons/loadout-contact-sheet.png.'
