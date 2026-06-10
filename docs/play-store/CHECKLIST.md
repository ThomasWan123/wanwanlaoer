# Play Console 上架勾选清单

包名：**com.sanguotd.towerdefense2** · 首版 **versionCode 8**

---

## A. 账号与法律

- [ ] Google Play 开发者账号已注册（$25 已付）
- [ ] 开发者身份 / 设备验证已完成（个人新账号）
- [ ] 已阅读并同意开发者政策、Play App Signing 条款

## B. 创建应用

- [ ] 创建应用 → **游戏** → **免费**
- [ ] 默认语言：**中文（简体）**
- [ ] 应用名称：**三国塔防**（与包名无关，包名已是 towerdefense2）

## C. 商店详情（Grow → 商店详情 → 主要商店详情）

- [ ] 应用名称
- [ ] 简短说明 ← [listing-zh-CN.txt](listing-zh-CN.txt)
- [ ] 完整说明 ← 同上
- [ ] 应用图标 **512×512** ← [icon-512.png](icon-512.png)
- [ ] 功能图 **1024×500** ← [feature-graphic-1024x500.jpg](feature-graphic-1024x500.jpg)
- [ ] 手机截图 **至少 2 张，建议 6 张** ← [phone-screenshots/](phone-screenshots/)
- [ ] 联系邮箱 **wan27990@gmail.com**
- [ ] 隐私政策 URL ← [PRIVACY_URL.md](PRIVACY_URL.md)（仓库需 Public）

- [ ] 仓库 https://github.com/ThomasWan123/wanwanlaoer 已公开且含 `PRIVACY.md`
- [ ] **广告** → 否
- [ ] **应用内购买** → 否
- [ ] **内容分级（IARC）** ← [iarc-guide.md](iarc-guide.md)
- [ ] **数据安全** ← [data-safety-answers.md](data-safety-answers.md)
- [ ] **目标受众** → 非面向儿童专用（无 COPPA 定向）
- [ ] **新闻应用 / COVID 等** → 均不适用，跳过

## E. 定价与分发

- [ ] 国家/地区：选要发布的区域（建议先选港澳台 + 东南亚 + 全球，不含大陆商店）
- [ ] 定价：免费

## F. 发布版本

### 内部测试（建议先做）

- [ ] 测试 → 内部测试 → 创建新版本
- [ ] 上传 `app/build/outputs/bundle/release/app-release.aab`
- [ ] 版本说明 ← listing 中 v2.0.1 说明
- [ ] 添加测试人员 Gmail
- [ ] 开始发布 → 用 opt-in 链接安装验证

### 生产

- [ ] 生产 → 创建新版本（或从内部测试推广）
- [ ] 同一 AAB，`versionCode` 不可重复
- [ ] 分阶段发布（可选）
- [ ] 提交审核

## G. 本机构建（上传前）

```powershell
.\tools\setup-java-truststore.ps1    # 公司网络首次需要
.\tools\prepare-play-store-assets.ps1
.\tools\build-release-aab.ps1
node tools/pre-release.mjs --skip-gradle   # 静态检查
```

- [ ] AAB 已生成
- [ ] `keystore` 已备份

---

## 常见拒审预防

- [ ] 隐私政策链接有效
- [ ] 数据安全与 PRIVACY.md 一致（不收集却声明收集会被拒）
- [ ] 截图来自真实游戏，非虚假宣传
- [ ] 包名与首次创建时完全一致
