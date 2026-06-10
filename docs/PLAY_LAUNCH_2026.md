# Google Play 上架方案 · 三国塔防（2026）

针对本仓库 **`com.sanguotd.towerdefense2`** 的省力流程。预计 **3～7 天**（账号已有）或 **1～2 周**（含新注册与首次审核）。

## 你的项目现状（已就绪）

| 项 | 状态 |
|----|------|
| 包名 | `com.sanguotd.towerdefense2`（**创建应用后不可改**） |
| 版本 | `versionCode 8` / `versionName 2.0.1` |
| 格式 | 需上传 **AAB**（`bundleRelease`） |
| 权限 | 仅 `VIBRATE`，无 `INTERNET` |
| 隐私文案 | [PRIVACY.md](../PRIVACY.md) |
| 商店文案 | [play-store/listing-zh-CN.txt](play-store/listing-zh-CN.txt) |
| 数据安全答案 | [play-store/data-safety-answers.md](play-store/data-safety-answers.md) |
| IARC 指南 | [play-store/iarc-guide.md](play-store/iarc-guide.md) |
| 图形资产 | 运行 `tools/prepare-play-store-assets.ps1` → `docs/play-store/` |
| 真机验收 | `node tools/pre-release.mjs --device` |

**说明**：安装包内应用名显示「三国塔防2」，商店可统一为「三国塔防」（Play 列表名称与 APK `app_name` 可以不同）。

---

## 推荐时间线（按顺序做）

### 第 0 天：账号与密钥（约 1 小时）

1. [Play Console](https://play.google.com/console) 注册开发者（**$25 一次性**，需国际信用卡）
2. 大陆访问通常需稳定网络环境
3. 本机生成**上传密钥**（仅首次）：
   ```powershell
   cd c:\GT-123\game\AndroidVer
   copy keystore.properties.example keystore.properties
   keytool -genkeypair -v -keystore release.keystore -alias release -keyalg RSA -keysize 2048 -validity 10000
   ```
4. 编辑 `keystore.properties` 填写密码与路径  
5. **备份** `release.keystore` 与密码（丢失则无法更新同一应用）

### 第 1 天：构建 AAB（约 30 分钟）

```powershell
# 若 Gradle HTTPS 报错（公司网络），先执行一次：
.\tools\setup-java-truststore.ps1

# 发版门禁 + 打正式包
.\tools\build-release-aab.ps1
```

产物：`app/build/outputs/bundle/release/app-release.aab`

首次上传 AAB 时，Play Console 会引导启用 **Play App Signing**（Google 托管应用签名密钥）→ **接受即可**。

### 第 2 天：创建应用 + 填控制台（约 2～3 小时）

1. **创建应用** → 类型选 **游戏** → 免费 → 默认语言 **中文（简体）**
2. 按 [play-store/CHECKLIST.md](play-store/CHECKLIST.md) 逐项打勾
3. **商店详情**：粘贴 [listing-zh-CN.txt](play-store/listing-zh-CN.txt)
4. 上传 `docs/play-store/icon-512.png`、`feature-graphic-1024x500.jpg`、`phone-screenshots/*.jpg`
5. **内容分级**：按 [iarc-guide.md](play-store/iarc-guide.md)
6. **数据安全**：按 [data-safety-answers.md](play-store/data-safety-answers.md)
7. **隐私政策 URL**：https://github.com/ThomasWan123/wanwanlaoer/blob/main/PRIVACY.md（见 [PRIVACY_URL.md](play-store/PRIVACY_URL.md)）  
   **联系邮箱**：wan27990@gmail.com

### 第 3～4 天：内部测试轨（强烈建议）

1. **测试 → 内部测试 → 创建版本**
2. 上传同一 `app-release.aab`
3. 添加测试人员 Gmail（可只加自己）
4. 用内部测试链接安装，外屏再玩 2～3 关

### 第 5～7 天：生产发布

1. 内部测试无问题 → **生产 → 创建新版本** → 上传 AAB（或从内部测试 **推广版本**）
2. 选择 **分阶段发布**（如 20% → 50% → 100%）更稳妥
3. 提交审核，首次约 **1～7 天**

---

## 与本游戏相关的特别说明

| 主题 | 建议 |
|------|------|
| **横屏** | 仅 landscape；截图用 **横屏**（脚本已从真机 2520×1080 导出） |
| **包体** | 当前 AAB 约 20MB 级，无需 Play Asset Delivery |
| **折叠屏** | 可作为商店描述卖点，非必填项 |
| **无内购/无广告** | 应用内容里选「不含广告」「不含应用内购买」 |
| **个人开发者账号** | 2023 年后新账号可能需 Play Console **手机 App 设备验证** |
| **更新** | 每次 `versionCode` +1，重新 `bundleRelease` 上传 |

---

## 文件索引

```
docs/play-store/
  CHECKLIST.md              ← 控制台逐项勾选
  listing-zh-CN.txt         ← 商店文案
  data-safety-answers.md
  iarc-guide.md
  PRIVACY_URL.md
  icon-512.png              ← 脚本生成
  feature-graphic-1024x500.jpg
  phone-screenshots/        ← 6～8 张横屏截图

tools/
  prepare-play-store-assets.ps1
  build-release-aab.ps1
```

---

## 官方参考

- [创建并设置应用](https://support.google.com/googleplay/android-developer/answer/9859152?hl=zh-Hans)
- [上传 App Bundle](https://support.google.com/googleplay/android-developer/answer/7159011)
- [Play App Signing](https://support.google.com/googleplay/android-developer/answer/9842756)
