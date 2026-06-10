# 构建 Google Play 用 Release AAB（含 truststore 检查）
$ErrorActionPreference = "Stop"
$Root = (Resolve-Path "$PSScriptRoot\..").Path
Set-Location $Root

$truststore = Join-Path $Root "tools\gradle-truststore.jks"
if (-not (Test-Path $truststore)) {
    Write-Host ">>> 首次运行：配置 Java 企业证书 truststore"
    & "$PSScriptRoot\setup-java-truststore.ps1"
}

$ksProps = Join-Path $Root "keystore.properties"
if (-not (Test-Path $ksProps)) {
    Write-Warning @"
未找到 keystore.properties — Release 将使用 debug 证书（不可正式上架）。

上架前请执行：
  copy keystore.properties.example keystore.properties
  keytool -genkeypair -v -keystore release.keystore -alias release -keyalg RSA -keysize 2048 -validity 10000

"@ 
}

$gradle = "$env:USERPROFILE\.gradle\wrapper\dists\gradle-8.13-bin\5xuhj0ry160q40clulazy9h7d\gradle-8.13\bin\gradle.bat"
if (-not (Test-Path $gradle)) { $gradle = Join-Path $Root "gradlew.bat" }

Write-Host ">>> bundleRelease"
& $gradle bundleRelease --no-daemon

$aab = Join-Path $Root "app\build\outputs\bundle\release\app-release.aab"
if (Test-Path $aab) {
    $mb = (Get-Item $aab).Length / 1MB
    Write-Host "`nAAB ready: $aab ($([math]::Round($mb, 1)) MB)"
    Write-Host "Upload this file to Play Console (Internal testing or Production)."
} else {
    throw "AAB not found after build"
}
