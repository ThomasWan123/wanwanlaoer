# Google Play 商店物料（预备）

`applicationId`：**com.sanguotd.towerdefense2**（与 [`app/build.gradle`](../app/build.gradle) 一致）

下一版上架：`versionCode` **8**，`versionName` **2.0.1**

## 文案（可直接粘贴）

**应用名**  
三国塔防

**简短说明（80 字内）**  
单机三国塔防：30 关战役，布武将守线，释放大招逆转战局。支持折叠屏外屏，无需联网。

**完整说明**  
《三国塔防》是一款离线单机塔防游戏。  
• 30 关经典战役，从黄巾之乱到三分归晋  
• 收集三国名将，升级兵种与合成高阶武将  
• 星级评价与战役进度本地保存  
• 针对折叠屏外屏优化：道路居中、双指缩放、单指拖动  
• 无账号、无广告追踪、数据仅存本机  

## 图形资产

| 项 | 规格 | 状态 |
|----|------|------|
| 高分辨率图标 | 512×512 PNG | 由 `ic_launcher` 导出 |
| 功能图 | 1024×500（可选） | 待制 |
| 手机截图 | 横屏 16:9，至少 2 张，建议 4–8 张 | 见 `docs/play-store/phone-screenshots/`（真机验收后运行 `tools/prepare-play-store-assets.ps1`） |
| 7 寸 / 10 寸平板图 | 可选 | — |

**建议截图场景**：主菜单、选关（地图缩略图）、战斗（赤壁/虎牢关）、大招释放、图鉴。

## 合规

| 项 | 内容 |
|----|------|
| 隐私政策 URL | https://github.com/ThomasWan123/wanwanlaoer/blob/main/PRIVACY.md（见 [PRIVACY_URL.md](play-store/PRIVACY_URL.md)） |
| 联系邮箱 | wan27990@gmail.com |
| 数据安全 | 仅本地存储；无收集、无共享（与 PRIVACY 一致） |
| 内容分级 | IARC：卡通战斗、无写实血腥 |
| 目标受众 | 13+ / 全年龄（按问卷选择） |

## 构建命令

```powershell
# 1. 配置正式签名（勿提交 keystore.properties）
copy keystore.properties.example keystore.properties
# 编辑 storeFile / 密码 / alias

# 2. 发版前门禁
node tools/pre-release.mjs --device

# 3. 打 AAB
.\gradlew.bat bundleRelease
# 产物：app/build/outputs/bundle/release/app-release.aab
```

## 控制台检查清单

- [ ] 创建应用（若尚未创建），包名 `com.sanguotd.towerdefense2`
- [ ] 上传 AAB（versionCode 8）
- [ ] 填写商店 listing（上表文案）
- [ ] 上传图标与截图
- [ ] 隐私政策 URL + 数据安全表单
- [ ] 内容分级问卷
- [ ] 内部测试轨道 → 生产（或分阶段发布）
