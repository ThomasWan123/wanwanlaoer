# 安装 Release APK 到已连接的三星/Android 手机
$adb = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
$apk = Join-Path $PSScriptRoot "dist\app-release-1.1.3.apk"

if (-not (Test-Path $adb)) {
    Write-Error "未找到 adb：$adb"
    exit 1
}
if (-not (Test-Path $apk)) {
    Write-Error "未找到 APK：$apk`n请先运行：.\gradlew.bat assembleRelease"
    exit 1
}

Write-Host "等待手机连接（请开启 USB 调试并授权此电脑）..."
& $adb wait-for-device
$serial = (& $adb devices | Select-String "device$" | Select-Object -First 1).ToString().Split("`t")[0]
if (-not $serial) {
    Write-Error "未检测到已授权设备。请在手机上允许 USB 调试。"
    exit 1
}

Write-Host "检测到设备：$serial"
Write-Host "安装 $apk ..."
& $adb -s $serial install -r $apk
if ($LASTEXITCODE -eq 0) {
    Write-Host "安装成功 · 三国塔防 1.1.3"
} else {
    Write-Error "安装失败，退出码 $LASTEXITCODE"
    exit $LASTEXITCODE
}
