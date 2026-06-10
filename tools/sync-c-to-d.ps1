# C（开发版）→ D（正式版）镜像同步
# 用途：C 上试验通过、测试无误后，推送到 D
# 用法：.\tools\sync-c-to-d.ps1
#       .\tools\sync-c-to-d.ps1 -Backup   # 同步前备份 D 到 D-backup-日期

param(
    [switch]$Backup,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$DevRoot  = "C:\GT-123\game\AndroidVer"
$ProdRoot = "D:\GT-123\game\AndroidVer"

if (-not (Test-Path $DevRoot)) {
    Write-Error "未找到开发版目录：$DevRoot"
    exit 1
}

Write-Host "=== C → D 同步 ===" -ForegroundColor Yellow
Write-Host "来源（开发）：$DevRoot"
Write-Host "目标（正式）：$ProdRoot"
Write-Host "排除：build、.gradle、tools、*.apk"
Write-Host ""
Write-Host "警告：/MIR 会删除 D 上有而 C 上没有的文件。" -ForegroundColor Red

if (-not $Force) {
    $answer = Read-Host "确认将 C 覆盖到 D？(y/N)"
    if ($answer -notmatch '^[yY]') {
        Write-Host "已取消。"
        exit 0
    }
}

if ($Backup -and (Test-Path $ProdRoot)) {
    $stamp = Get-Date -Format "yyyyMMdd-HHmm"
    $backupRoot = "D:\GT-123\game\AndroidVer-backup-$stamp"
    Write-Host "备份 D → $backupRoot ..." -ForegroundColor Cyan
    & robocopy $ProdRoot $backupRoot /E /XD build "app\build" .gradle "wrapper\dists" /XF *.apk /NFL /NDL /NJH /NJS /NC /NS
    if ($LASTEXITCODE -ge 8) {
        Write-Error "备份失败，已中止同步。"
        exit $LASTEXITCODE
    }
    Write-Host "备份完成。" -ForegroundColor Green
}

$robocopyArgs = @(
    $DevRoot, $ProdRoot,
    "/MIR",
    "/XD", "build", "app\build", ".gradle", "wrapper\dists", "tools",
    "/XF", "*.apk",
    "/NFL", "/NDL", "/NJH", "/NJS", "/NC", "/NS"
)

& robocopy @robocopyArgs
$rc = $LASTEXITCODE

if ($rc -ge 8) {
    Write-Error "robocopy 失败，退出码 $rc"
    exit $rc
}

Write-Host ""
Write-Host "同步完成（robocopy 退出码 $rc）。" -ForegroundColor Green
Write-Host "建议在 D 上执行：.\gradlew assembleRelease"
