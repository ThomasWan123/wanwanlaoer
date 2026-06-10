# 发布与上架流程

在体验打磨与内测满意后，按此清单执行 Google Play 与版本发布。

## 版本号

- `versionCode`：每次上架递增（[`app/build.gradle`](../app/build.gradle) `defaultConfig`）
- `versionName`：语义化，如 `1.1.0`

## 1. 正式签名

1. 复制 `keystore.properties.example` → `keystore.properties`（勿提交 Git）
2. 生成密钥库：`keytool -genkey -v -keystore release.keystore -alias release -keyalg RSA -keysize 2048 -validity 10000`
3. 填写 `storeFile`、`storePassword`、`keyAlias`、`keyPassword`
4. 构建：`.\gradlew.bat bundleRelease`
5. 产物：`app/build/outputs/bundle/release/app-release.aab`

未配置 `keystore.properties` 时 Release 使用 debug 证书，**仅用于真机测试**。

## 2. 本地校验

```bash
node tools/pre-release.mjs
# 或分步：
.\gradlew.bat verifyApkAssetsParity
node tools/audit-campaign-balance.mjs
node tools/verify-art-manifest.mjs --apk app/build/outputs/apk/debug/app-debug.apk
```

安装 `app-release.apk` 做 [TESTING.md](../TESTING.md) 快测（含 Fold 外屏 A7–A10）。

## 3. Play 控制台

见 [PLAY.md](PLAY.md) 与 [STORE_LISTING.md](STORE_LISTING.md)。上架前必备：

- 512×512 图标
- 横屏截图 2～8 张（见 [play-store/phone-screenshots/](play-store/phone-screenshots/)）
- 简短/完整说明
- 隐私政策 URL（链到仓库 `PRIVACY.md`）
- 数据安全表单：仅本地存储，与 [PRIVACY.md](../PRIVACY.md) 一致

## 4. 开源仓库

- 确认 `.gitignore` 排除 `keystore.properties`、`*.keystore`、`app/build/`
- README 截图与构建说明已更新
- LICENSE：MIT（见根目录 [LICENSE](../LICENSE)）

## 5. CI 产物

GitHub Actions 在 `main`/`master` 推送后上传 `app-release-apk` artifact，可供内测下载。
