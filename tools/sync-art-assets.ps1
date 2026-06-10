# Sync generated art from Cursor assets folder into www/assets and rebuild manifest.json
param(
    [string]$Src = "$env:USERPROFILE\.cursor\projects\c-GT-123-game-AndroidVer\assets",
    [string]$Dst = "$PSScriptRoot\..\app\src\main\assets\www\assets"
)

$ErrorActionPreference = "Stop"
$data = Get-Content "$PSScriptRoot\art-prompts-data.json" -Raw | ConvertFrom-Json

# Ensure dirs
@("ui", "portraits", "maps") | ForEach-Object {
    New-Item -ItemType Directory -Force -Path (Join-Path $Dst $_) | Out-Null
}
foreach ($theme in $data.maps.PSObject.Properties.Name) {
    New-Item -ItemType Directory -Force -Path (Join-Path $Dst "maps\$theme") | Out-Null
}

# Copy maps: {theme}_bg.jpg -> maps/{theme}/bg.jpg
foreach ($theme in $data.maps.PSObject.Properties.Name) {
    $srcFile = Join-Path $Src "${theme}_bg.jpg"
    if (Test-Path $srcFile) {
        Copy-Item $srcFile (Join-Path $Dst "maps\$theme\bg.jpg") -Force
    }
}

# Copy portraits
$playable = @("guanyu","zhangfei","zhaoyun","zhugeliang","liubei","lvbu","zhouyu","caocao","sunquan")
$allPortraits = $playable + @($data.portraits.PSObject.Properties.Name)
foreach ($id in $allPortraits) {
    $srcFile = Join-Path $Src "$id.png"
    if (Test-Path $srcFile) {
        Copy-Item $srcFile (Join-Path $Dst "portraits\$id.png") -Force
    }
}

if (Test-Path (Join-Path $Src "menu_bg.jpg")) {
    Copy-Item (Join-Path $Src "menu_bg.jpg") (Join-Path $Dst "ui\menu_bg.jpg") -Force
}

# Build manifest
$manifest = @{
    version = 2
    style = "hand-painted-bright-guofeng"
    ui = @{ menu_bg = "ui/menu_bg.jpg" }
    portraits = @{}
    maps = @{}
}

foreach ($id in $allPortraits) {
    $rel = "portraits/$id.png"
    if (Test-Path (Join-Path $Dst $rel)) { $manifest.portraits[$id] = $rel }
}
foreach ($theme in $data.maps.PSObject.Properties.Name) {
    $rel = "maps/$theme/bg.jpg"
    if (Test-Path (Join-Path $Dst $rel)) { $manifest.maps[$theme] = $rel }
}
# Pilot + existing 3
@("central_fort","east_fire","central_winter") | ForEach-Object {
    $rel = "maps/$_/bg.jpg"
    if (Test-Path (Join-Path $Dst $rel)) { $manifest.maps[$_] = $rel }
}

$json = $manifest | ConvertTo-Json -Depth 5
[System.IO.File]::WriteAllText((Join-Path $Dst "manifest.json"), $json, (New-Object System.Text.UTF8Encoding $false))

Write-Host "Maps:" $manifest.maps.Count " Portraits:" $manifest.portraits.Count
Write-Host "Next: .\tools\compress-art.ps1  then  node tools\verify-art-manifest.mjs"
Get-ChildItem -Recurse $Dst -File | Measure-Object -Property Length -Sum | ForEach-Object {
    Write-Host ("Total assets: {0:N1} MB" -f ($_.Sum / 1MB))
}
