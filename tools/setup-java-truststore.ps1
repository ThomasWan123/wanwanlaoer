# Import Windows "Cato Networks Root CA" (or other corp proxy root) into a project-local
# Java truststore for Gradle. Required when Maven/Gradle HTTPS fails with PKIX errors behind SSL inspection.
param(
    [string]$JavaHome = $env:JAVA_HOME,
    [string]$Out = "$PSScriptRoot\gradle-truststore.jks",
    [string]$StorePass = "changeit"
)

$ErrorActionPreference = "Stop"
if (-not $JavaHome) { throw "JAVA_HOME is not set" }
$keytool = Join-Path $JavaHome "bin\keytool.exe"
$srcCacerts = Join-Path $JavaHome "lib\security\cacerts"
if (-not (Test-Path $keytool)) { throw "keytool not found: $keytool" }

Copy-Item $srcCacerts $Out -Force

$cato = Get-ChildItem Cert:\LocalMachine\Root, Cert:\CurrentUser\Root -ErrorAction SilentlyContinue |
    Where-Object { $_.Subject -like "CN=Cato Networks Root CA*" } |
    Select-Object -First 1

if (-not $cato) {
    Write-Warning "Cato Networks Root CA not found in Windows store. Export your corp root and run:"
    Write-Warning "  keytool -importcert -alias corp-root -file your-root.cer -keystore $Out -storepass $StorePass"
    exit 1
}

$cer = Join-Path $env:TEMP "corp-root-ca.cer"
Export-Certificate -Cert $cato -FilePath $cer | Out-Null
& $keytool -importcert -noprompt -alias cato-networks-root -file $cer -keystore $Out -storepass $StorePass
Remove-Item $cer -Force -ErrorAction SilentlyContinue

Write-Host "Truststore ready: $Out"
Write-Host "gradle.properties should reference: -Djavax.net.ssl.trustStore=tools/gradle-truststore.jks"
