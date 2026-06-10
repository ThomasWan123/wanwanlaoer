# 从真机验收截屏与游戏资源生成 Play 商店图形
param(
    [string]$ProjectRoot = (Resolve-Path "$PSScriptRoot\..").Path,
    [int]$ScreenshotMaxWidth = 1920
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$Out = Join-Path $ProjectRoot "docs\play-store"
$ShotsOut = Join-Path $Out "phone-screenshots"
$Acceptance = Join-Path $ProjectRoot "tools\acceptance-captures"
$Ult = Join-Path $ProjectRoot "tools\ult-vfx-captures"
$MenuBg = Join-Path $ProjectRoot "app\src\main\assets\www\assets\ui\menu_bg.jpg"

New-Item -ItemType Directory -Force -Path $Out, $ShotsOut | Out-Null

function Save-Jpeg([System.Drawing.Image]$img, [string]$path, [long]$quality = 88) {
    $enc = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() |
        Where-Object { $_.FormatID -eq [System.Drawing.Imaging.ImageFormat]::Jpeg.Guid } | Select-Object -First 1
    $ep = New-Object System.Drawing.Imaging.EncoderParameters(1)
    $ep.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter(
        [System.Drawing.Imaging.Encoder]::Quality, $quality)
    $img.Save($path, $enc, $ep)
}

function Resize-Width([System.Drawing.Image]$src, [int]$maxW) {
    if ($src.Width -le $maxW) { return $src.Clone() }
    $scale = $maxW / $src.Width
    $w = [int]$maxW
    $h = [int][Math]::Round($src.Height * $scale)
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.DrawImage($src, 0, 0, $w, $h)
    $g.Dispose()
    return $bmp
}

# --- 512 icon (match ic_launcher star) ---
$icon = New-Object System.Drawing.Bitmap 512, 512
$g = [System.Drawing.Graphics]::FromImage($icon)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::FromArgb(255, 42, 26, 16))
$pts = @(
    [System.Drawing.Point]::new(256, 95),
    [System.Drawing.Point]::new(303, 190),
    [System.Drawing.Point]::new(398, 209),
    [System.Drawing.Point]::new(331, 275),
    [System.Drawing.Point]::new(352, 379),
    [System.Drawing.Point]::new(256, 332),
    [System.Drawing.Point]::new(160, 379),
    [System.Drawing.Point]::new(181, 275),
    [System.Drawing.Point]::new(114, 209),
    [System.Drawing.Point]::new(209, 190)
)
$inner = @(
    [System.Drawing.Point]::new(256, 142),
    [System.Drawing.Point]::new(284, 199),
    [System.Drawing.Point]::new(350, 213),
    [System.Drawing.Point]::new(303, 260),
    [System.Drawing.Point]::new(317, 332),
    [System.Drawing.Point]::new(256, 298),
    [System.Drawing.Point]::new(195, 332),
    [System.Drawing.Point]::new(209, 260),
    [System.Drawing.Point]::new(162, 213),
    [System.Drawing.Point]::new(228, 199)
)
$g.FillPolygon([System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 247, 215, 116)), $pts)
$g.FillPolygon([System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 201, 137, 43)), $inner)
$g.Dispose()
$icon.Save((Join-Path $Out "icon-512.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$icon.Dispose()
Write-Host "OK icon-512.png"

# --- Feature graphic 1024x500 ---
$fgW, $fgH = 1024, 500
$fg = New-Object System.Drawing.Bitmap $fgW, $fgH
$g = [System.Drawing.Graphics]::FromImage($fg)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
if (Test-Path $MenuBg) {
    $bg = [System.Drawing.Image]::FromFile($MenuBg)
    $scale = [Math]::Max($fgW / $bg.Width, $fgH / $bg.Height)
    $sw = [int]($bg.Width * $scale)
    $sh = [int]($bg.Height * $scale)
    $x = ($fgW - $sw) / 2
    $y = ($fgH - $sh) / 2
    $g.DrawImage($bg, $x, $y, $sw, $sh)
    $bg.Dispose()
    $overlay = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(120, 20, 12, 8))
    $g.FillRectangle($overlay, 0, 0, $fgW, $fgH)
} else {
    $g.Clear([System.Drawing.Color]::FromArgb(255, 42, 26, 16))
}
$font = New-Object System.Drawing.Font("Microsoft YaHei", 52, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$sub = New-Object System.Drawing.Font("Microsoft YaHei", 22, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$gold = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 247, 215, 116))
$white = [System.Drawing.Brushes]::White
$g.DrawString("三国塔防", $font, $gold, 48, 160)
$g.DrawString("30关战役 · 布武将 · 放大招 · 单机离线", $sub, $white, 52, 250)
$gold.Dispose()
$g.Dispose()
Save-Jpeg $fg (Join-Path $Out "feature-graphic-1024x500.jpg") 90
$fg.Dispose()
Write-Host "OK feature-graphic-1024x500.jpg"

# --- Phone screenshots (landscape) ---
$pick = @(
    @{ src = "01_menu.png"; dst = "01-main-menu.jpg" },
    @{ src = "02_level_select.png"; dst = "02-level-select.jpg" },
    @{ src = "04_battle_chibi.png"; dst = "03-battle-chibi.jpg" },
    @{ src = "05_battle_huluguan.png"; dst = "04-battle-huluguan.jpg" },
    @{ src = "03_codex.png"; dst = "05-codex.jpg" },
    @{ src = "10_level5_xiaopei_battle.png"; dst = "06-battle-xiaopei.jpg" }
)
$n = 0
foreach ($p in $pick) {
    $path = Join-Path $Acceptance $p.src
    if (-not (Test-Path $path)) { Write-Warning "skip missing $path"; continue }
    $img = [System.Drawing.Image]::FromFile($path)
    $scaled = Resize-Width $img $ScreenshotMaxWidth
    Save-Jpeg $scaled (Join-Path $ShotsOut $p.dst)
    $scaled.Dispose(); $img.Dispose()
    $n++; Write-Host "OK $($p.dst)"
}
$ultFlood = Join-Path $Ult "flood.png"
if (Test-Path $ultFlood) {
    $img = [System.Drawing.Image]::FromFile($ultFlood)
    $scaled = Resize-Width $img $ScreenshotMaxWidth
    Save-Jpeg $scaled (Join-Path $ShotsOut "07-ultimate-flood.jpg")
    $scaled.Dispose(); $img.Dispose()
    $n++; Write-Host "OK 07-ultimate-flood.jpg"
}
Write-Host "`nDone: $n screenshots -> $ShotsOut"
Write-Host "Store assets folder: $Out"
