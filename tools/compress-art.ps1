param(
    [string]$AssetsDir = (Join-Path $PSScriptRoot "..\app\src\main\assets\www\assets"),
    [int]$PortraitMaxKb = 200,
    [int]$MapMaxKb = 400,
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"
$AssetsDir = (Resolve-Path $AssetsDir).Path

function Get-TargetMaxBytes([string]$RelPath) {
    if ($RelPath -match "^portraits/") { return $PortraitMaxKb * 1024 }
    return $MapMaxKb * 1024
}

function Compress-Portrait([string]$File, [int]$MaxBytes) {
    Add-Type -AssemblyName System.Drawing
    for ($maxSide = 640; $maxSide -ge 256; $maxSide -= 64) {
        $tmp = "$File.tmp"
        $img = [System.Drawing.Image]::FromFile($File)
        $scale = 1.0
        if ($img.Width -gt $maxSide -or $img.Height -gt $maxSide) {
            $scale = $maxSide / [Math]::Max($img.Width, $img.Height)
        }
        $w = [int][Math]::Max(64, $img.Width * $scale)
        $h = [int][Math]::Max(64, $img.Height * $scale)
        $bmp = New-Object System.Drawing.Bitmap $w, $h
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        try {
            $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $g.DrawImage($img, 0, 0, $w, $h)
        } finally {
            $g.Dispose()
            $img.Dispose()
        }
        for ($q = 88; $q -ge 50; $q -= 6) {
            $enc = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() |
                Where-Object { $_.MimeType -eq "image/png" } | Select-Object -First 1
            $ep = New-Object System.Drawing.Imaging.EncoderParameters(1)
            $ep.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter(
                [System.Drawing.Imaging.Encoder]::Compression, [long]9)
            $bmp.Save($tmp, $enc, $ep)
            if ((Test-Path $tmp) -and (Get-Item $tmp).Length -le $MaxBytes) {
                $bmp.Dispose()
                if (-not $WhatIf) {
                    Remove-Item $File -Force -ErrorAction SilentlyContinue
                    Move-Item $tmp $File -Force
                } else { Remove-Item $tmp -Force }
                return $true
            }
            Remove-Item $tmp -Force -ErrorAction SilentlyContinue
        }
        $bmp.Dispose()
    }
    return $false
}

function Test-MagickAvailable {
    return [bool](Get-Command magick -ErrorAction SilentlyContinue)
}

function Compress-WithMagick([string]$File, [int]$MaxBytes) {
    $ext = [IO.Path]::GetExtension($File).ToLower()
    $quality = if ($ext -eq ".png") { 92 } else { 85 }
    $tmp = "$File.tmp"
    do {
        if ($ext -eq ".png") {
            & magick $File -strip -define png:compression-level=9 $tmp 2>$null
        } else {
            & magick $File -strip -quality $quality $tmp 2>$null
        }
        if (-not (Test-Path $tmp)) { break }
        $size = (Get-Item $tmp).Length
        if ($size -le $MaxBytes) {
            if (-not $WhatIf) {
                Remove-Item $File -Force -ErrorAction SilentlyContinue
                Move-Item $tmp $File -Force
            } else { Remove-Item $tmp -Force }
            return $true
        }
        Remove-Item $tmp -Force -ErrorAction SilentlyContinue
        $quality -= 8
    } while ($quality -ge 45)
    return $false
}

function Compress-WithDrawing([string]$File, [int]$MaxBytes) {
    Add-Type -AssemblyName System.Drawing
    $ext = [IO.Path]::GetExtension($File).ToLower()
    for ($q = 90; $q -ge 45; $q -= 8) {
        $tmp = "$File.tmp"
        $img = [System.Drawing.Image]::FromFile($File)
        try {
            if ($ext -eq ".png") {
                $enc = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() |
                    Where-Object { $_.MimeType -eq "image/png" } | Select-Object -First 1
                $ep = New-Object System.Drawing.Imaging.EncoderParameters(1)
                $ep.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter(
                    [System.Drawing.Imaging.Encoder]::Compression, [long]9)
                $img.Save($tmp, $enc, $ep)
            } else {
                $enc = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() |
                    Where-Object { $_.FormatID -eq [System.Drawing.Imaging.ImageFormat]::Jpeg.Guid } | Select-Object -First 1
                $ep = New-Object System.Drawing.Imaging.EncoderParameters(1)
                $ep.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter(
                    [System.Drawing.Imaging.Encoder]::Quality, [long]$q)
                $img.Save($tmp, $enc, $ep)
            }
        } finally {
            $img.Dispose()
        }
        if (-not (Test-Path $tmp)) { continue }
        if ((Get-Item $tmp).Length -le $MaxBytes) {
            if (-not $WhatIf) {
                Remove-Item $File -Force -ErrorAction SilentlyContinue
                Move-Item $tmp $File -Force
            } else { Remove-Item $tmp -Force }
            return $true
        }
        Remove-Item $tmp -Force -ErrorAction SilentlyContinue
    }
    # PNG 体积过大时按比例缩小
    for ($scale = 0.85; $scale -ge 0.45; $scale -= 0.1) {
        $tmp = "$File.tmp"
        $img = [System.Drawing.Image]::FromFile($File)
        $w = [int][Math]::Max(64, $img.Width * $scale)
        $h = [int][Math]::Max(64, $img.Height * $scale)
        $bmp = New-Object System.Drawing.Bitmap $w, $h
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        try {
            $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $g.DrawImage($img, 0, 0, $w, $h)
        } finally {
            $g.Dispose()
            $img.Dispose()
        }
        try {
            if ($ext -eq ".png") {
                $bmp.Save($tmp, [System.Drawing.Imaging.ImageFormat]::Png)
            } else {
                $enc = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() |
                    Where-Object { $_.FormatID -eq [System.Drawing.Imaging.ImageFormat]::Jpeg.Guid } | Select-Object -First 1
                $ep = New-Object System.Drawing.Imaging.EncoderParameters(1)
                $ep.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter(
                    [System.Drawing.Imaging.Encoder]::Quality, [long]82)
                $bmp.Save($tmp, $enc, $ep)
            }
        } finally {
            $bmp.Dispose()
        }
        if ((Test-Path $tmp) -and (Get-Item $tmp).Length -le $MaxBytes) {
            if (-not $WhatIf) {
                Remove-Item $File -Force -ErrorAction SilentlyContinue
                Move-Item $tmp $File -Force
            } else { Remove-Item $tmp -Force }
            return $true
        }
        Remove-Item $tmp -Force -ErrorAction SilentlyContinue
    }
    return $false
}

if (-not (Test-Path $AssetsDir)) {
    throw "Assets dir not found: $AssetsDir. Run tools/sync-art-assets.ps1 first."
}

$useMagick = Test-MagickAvailable
Write-Host "Compress art in $AssetsDir (magick=$useMagick whatIf=$WhatIf)"

$stats = @{ tried = 0; shrunk = 0; skipped = 0; failed = 0 }
$rootLen = $AssetsDir.Length + 1

Get-ChildItem -Path $AssetsDir -Recurse -File -Include *.jpg, *.jpeg, *.png | ForEach-Object {
    $rel = $_.FullName.Substring($rootLen).Replace("\", "/")
    $max = Get-TargetMaxBytes $rel
    if ($_.Length -le $max) {
        $stats.skipped++
        return
    }
    $stats.tried++
    $before = $_.Length
    $ok = if ($rel -match "^portraits/") {
        Compress-Portrait $_.FullName $max
    } elseif ($useMagick) {
        Compress-WithMagick $_.FullName $max
    } else {
        Compress-WithDrawing $_.FullName $max
    }
    if ($ok) {
        $after = if ($WhatIf) { $before } else { (Get-Item $_.FullName).Length }
        $stats.shrunk++
        Write-Host ("  OK {0}: {1:N0} -> {2:N0} bytes" -f $rel, $before, $after)
    } else {
        $stats.failed++
        Write-Warning ("  FAIL {0} still > {1}KB" -f $rel, [math]::Round($max / 1KB))
    }
}

$total = (Get-ChildItem -Recurse $AssetsDir -File | Measure-Object -Property Length -Sum).Sum
Write-Host ("Done. tried={0} shrunk={1} skipped={2} failed={3} total={4:N1} MB" -f `
    $stats.tried, $stats.shrunk, $stats.skipped, $stats.failed, ($total / 1MB))

if ($stats.failed -gt 0) { exit 1 }
