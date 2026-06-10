# D（正式版）→ C（开发版）镜像同步
# 用途：以 D 为基准重置 C，或 D 有更新时拉取到 C
# 用法：.\tools\sync-d-to-c.ps1

$ErrorActionPreference = "Stop"

$ProdRoot = "D:\GT-123\game\AndroidVer"
$DevRoot  = "C:\GT-123\game\AndroidVer"

if (-not (Test-Path $ProdRoot)) {
    Write-Error "未找到正式版目录：$ProdRoot"
    exit 1
}

Write-Host "=== D → C 同步 ===" -ForegroundColor Cyan
Write-Host "来源（正式）：$ProdRoot"
Write-Host "目标（开发）：$DevRoot"
Write-Host "排除：build、.gradle、tools、*.apk"
Write-Host ""

$robocopyArgs = @(
    $ProdRoot, $DevRoot,
    "/MIR",
    "/XD", "build", "app\build", ".gradle", "wrapper\dists", "tools",
    "/XF", "*.apk",
    "/NFL", "/NDL", "/NJH", "/NJS", "/NC", "/NS"
)

& robocopy @robocopyArgs
$rc = $LASTEXITCODE

# robocopy: 0–7 均为成功（有/无文件变更）
if ($rc -ge 8) {
    Write-Error "robocopy 失败，退出码 $rc"
    exit $rc
}

Write-Host ""
Write-Host "同步完成（robocopy 退出码 $rc）。" -ForegroundColor Green
Write-Host "C 上 tools/ 已保留，未覆盖。"
