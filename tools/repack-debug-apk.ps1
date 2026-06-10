# Repack assets/www into existing debug APK when Gradle cannot run (e.g. SSL).
param(
    [string]$SrcApk = "$PSScriptRoot\..\app\build\outputs\apk\debug\app-debug.apk",
    [string]$Www = "$PSScriptRoot\..\app\src\main\assets\www",
    [string]$OutApk = "$PSScriptRoot\..\app\build\outputs\apk\debug\app-debug-repacked.apk",
    [string]$BuildTools = "$env:LOCALAPPDATA\Android\Sdk\build-tools\35.0.0"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$SrcApk = (Resolve-Path $SrcApk).Path
$Www = (Resolve-Path $Www).Path
$OutDir = Split-Path $OutApk -Parent
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$OutApk = (Join-Path $OutDir (Split-Path $OutApk -Leaf))
$AlignedApk = $OutApk -replace "\.apk$", "-aligned.apk"

Copy-Item $SrcApk $OutApk -Force

$zip = [System.IO.Compression.ZipFile]::Open($OutApk, [System.IO.Compression.ZipArchiveMode]::Update)
try {
    $remove = @($zip.Entries | Where-Object { $_.FullName -like "assets/www/*" })
    foreach ($e in $remove) { $e.Delete() }

    $prefix = "assets/www/"
    Get-ChildItem $Www -Recurse -File | ForEach-Object {
        $rel = $prefix + $_.FullName.Substring($Www.Length + 1).Replace("\", "/")
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, $rel, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally {
    $zip.Dispose()
}

$zipalign = Join-Path $BuildTools "zipalign.exe"
$apksigner = Join-Path $BuildTools "apksigner.bat"
$debugKs = Join-Path $env:USERPROFILE ".android\debug.keystore"

if (-not (Test-Path $zipalign)) { throw "zipalign not found: $zipalign" }
if (-not (Test-Path $apksigner)) { throw "apksigner not found: $apksigner" }

Remove-Item $AlignedApk -Force -ErrorAction SilentlyContinue
& $zipalign -f -p 4 $OutApk $AlignedApk
Move-Item $AlignedApk $OutApk -Force

$SignedApk = $OutApk -replace "\.apk$", "-signed.apk"
Remove-Item $SignedApk -Force -ErrorAction SilentlyContinue
& $apksigner sign --ks $debugKs --ks-pass pass:android --key-pass pass:android --out $SignedApk $OutApk
Move-Item $SignedApk $OutApk -Force
& $apksigner verify --verbose $OutApk | Out-Null

$mb = (Get-Item $OutApk).Length / 1MB
Write-Host "Repacked: $OutApk ($([math]::Round($mb, 1)) MB)"
Write-Host "Install: adb install -r `"$OutApk`""
