# 构建说明

## 环境

- JDK 11 或更高
- Android SDK（`compileSdk 35`，`minSdk 21`）
- Windows：`.\gradlew.bat`；macOS/Linux：`./gradlew`

## 常用命令

```bash
.\gradlew.bat assembleDebug
.\gradlew.bat assembleRelease
.\gradlew.bat bundleRelease          # Google Play AAB
.\gradlew.bat verifyApkAssetsParity
```

## Release 签名

1. 复制 `keystore.properties.example` 为 `keystore.properties`（**勿提交 Git**）。
2. 生成密钥库（示例）：
   ```bash
   keytool -genkeypair -v -keystore release.keystore -alias release -keyalg RSA -keysize 2048 -validity 10000
   ```
3. 填写 `storeFile`、`storePassword`、`keyAlias`、`keyPassword`。
4. 再执行 `assembleRelease` / `bundleRelease`。

未配置 `keystore.properties` 时，Release 使用 **debug 证书** 签名，仅供内测，不可上架。

## AAB（Google Play）

```bash
.\gradlew.bat bundleRelease
```

输出：`app/build/outputs/bundle/release/app-release.aab`

上架前需正式签名，并准备商店图、说明、内容分级。见 [PLAY.md](PLAY.md)、[PLAY_LAUNCH_2026.md](PLAY_LAUNCH_2026.md) 与 [play-store/CHECKLIST.md](play-store/CHECKLIST.md)。

## 游戏资源位置

- Web：`app/src/main/assets/www/`
- 原生壳：`app/src/main/java/com/sanguotd/`

## 发版前门禁

```bash
node tools/pre-release.mjs              # Gradle 一致性 + 平衡审计 + manifest
node tools/pre-release.mjs --device     # 外加真机视口 / 验收（需 USB）
node tools/pre-release.mjs --skip-gradle  # 已构建时跳过 Gradle
```

详见 [TESTING.md](../TESTING.md) 章节 F。

## 美术资源

同步、压缩与 manifest 校验见 [ASSET_PIPELINE.md](ASSET_PIPELINE.md)。

```powershell
.\tools\sync-art-assets.ps1
.\tools\compress-art.ps1
node tools/verify-art-manifest.mjs --apk app/build/outputs/apk/debug/app-debug.apk
```

## 一致性校验

`verifyApkAssetsParity` 对比 debug 与 release APK 内 `assets/www/` 的 SHA-256，防止误用 `src/debug` 或 `src/release` 独立资源。

### Gradle 无法联网时（SSL / 证书）

**原因**：公司网络（如 Cato Networks）对 HTTPS 做 SSL  inspection，Java 默认信任库不含企业根证书。

**修复（一次性）**：

```powershell
.\tools\setup-java-truststore.ps1
```

会在 `tools/gradle-truststore.jks` 导入 Windows 证书库中的 Cato 根证书；`gradle.properties` 已配置 Gradle 使用该 truststore。

验证：`.\gradlew.bat assembleDebug` 应能正常解析 Maven 依赖。

若 Gradle 仍失败，可重打包现有 APK 中的 `assets/www`：

```powershell
.\tools\repack-debug-apk.ps1
adb install -r app\build\outputs\apk\debug\app-debug-repacked.apk
node tools/pre-release.mjs --skip-gradle --device
```

产物约 20MB（压缩后美术资源），**versionCode 仍为旧 APK 内嵌值**；完整递增版本需修复 Gradle 网络后正式构建。
