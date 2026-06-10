# Google Play 上架清单（预备）

**完整流程与已生成素材** → [PLAY_LAUNCH_2026.md](PLAY_LAUNCH_2026.md)  
**控制台逐项勾选** → [play-store/CHECKLIST.md](play-store/CHECKLIST.md)

## 1. 签名与包体

- [ ] 按 [BUILD.md](BUILD.md) 配置 `keystore.properties` 与 `release.keystore`
- [ ] `node tools/pre-release.mjs` 通过
- [ ] `.\gradlew.bat bundleRelease`
- [ ] 验证 AAB：`app/build/outputs/bundle/release/app-release.aab`

## 2. 商店物料

| 项 | 说明 |
|----|------|
| 应用名 | 三国塔防 |
| 简短说明 | 单机三国塔防，30 关战役，布武将守皇城 |
| 完整说明 | 突出教程、星级、折叠屏适配、无账号 |
| 图标 | 512×512（可基于 `ic_launcher` 导出） |
| 截图 | 手机横屏 2～8 张：主菜单、战斗、选关、大招 |
| 隐私政策 URL | 可链到 GitHub 上的 [PRIVACY.md](../PRIVACY.md) 渲染页 |
| 内容分级 | 问卷填写（无血腥实拍、无社交） |

完整文案与截图清单见 [STORE_LISTING.md](STORE_LISTING.md)。

## 3. 合规

- [ ] [PRIVACY.md](../PRIVACY.md) 与 Play 数据安全表单一致（仅本地存储）
- [x] 确定 [LICENSE](../LICENSE)（MIT）并更新 README
- [ ] `applicationId`：`com.sanguotd.towerdefense2`（与 build.gradle 一致，上架后勿改）

## 4. 开源（GitHub）

- [ ] 仓库公开（本地已备 README / MIT / `.gitignore`）
- [x] README 安装说明与 [play-store/phone-screenshots/](play-store/phone-screenshots/)
- [x] GitHub Actions + Release APK artifact（`.github/workflows/android.yml`）

## 5. 上架后

- [ ] 内测轨道或正式版发布说明
- [ ] 记录 `versionCode` 递增规则
