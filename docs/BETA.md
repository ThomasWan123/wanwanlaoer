# 内测包说明（第三版）

## 安装

1. 在电脑上构建：
   ```bash
   cd AndroidVer
   .\gradlew.bat assembleRelease
   ```
2. 将以下文件拷到手机并安装（覆盖安装即可）：
   - `app\build\outputs\apk\release\app-release.apk`
3. **不要**安装 `app-release-unsigned.apk`（未签名，会提示 package invalid）。

## 与 Debug 包的关系

- 游戏内容相同（`assets/www` 一致）。
- Release 体积更小（R8 压缩）；当前未配置正式密钥库时使用 **debug 证书** 签名，与 `app-debug.apk` 可互相覆盖安装。

## 清数据 / 覆盖安装

| 操作 | 效果 |
|------|------|
| 覆盖安装 APK | 保留 `localStorage` 战役进度 |
| 系统设置 → 应用 → 清除数据 | 进度、教程、星级、设置全部重置 |
| 卸载后重装 | 同「清除数据」 |

## 建议测试（约 15 分钟）

按 [TESTING.md](../TESTING.md) 勾选，优先：

- Fold 外屏：第一关进战、单指拖图、缩放、塔面板按钮
- 普通机：布阵、暂停、通关后杀进程重开看存档
- 设置：音效开关、画质切换

## 反馈请附带

- 机型（如 Galaxy Z Fold 7 / S24）
- 内屏或外屏
- 关卡号
- 截图或录屏
- 安装的是 `app-release.apk` 还是 debug 包

## 版本

- `versionName` 1.1.0（见 `app/build.gradle`）
- **第三版变更**：7～30 关平衡缓和；火/雪/水/帝都地图强化；曹操（奇门 `hex`）、孙权（水师 `tide`）；连杀/清波音效；MIT 开源
- 第二版：教程、设置、Fold、刘备、UI/大招等（见 [ROADMAP.md](ROADMAP.md)）

## CI 下载

GitHub Actions 构建成功后，在 Actions 页下载 **app-release-apk** artifact（与本地 `app-release.apk` 等价）。
