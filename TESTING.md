# 三国塔防 · 设备回归清单

发版或改动镜头 / 布局 / 塔面板后，在真机上按表勾选（Debug 或 Release 均可）。

## 构建与安装

```bash
cd AndroidVer
.\gradlew.bat assembleDebug
# 或 release：.\gradlew.bat assembleRelease
```

安装：

- Debug：`app\build\outputs\apk\debug\app-debug.apk`
- Release：`app\build\outputs\apk\release\app-release.apk`（已签名，可直接安装）
  - 未配置 `keystore.properties` 时暂用 **debug 证书** 签名，仅用于真机测试
  - 上架前请按 `keystore.properties.example` 配置正式密钥库

**勿安装** `app-release-unsigned.apk`：未签名，系统会提示 *package appears to be invalid*。

### Debug 与 Release 是否同一套游戏？

**是。** 两边都从 `app/src/main/assets/www` 打包，无 `src/debug` / `src/release` 独立资源目录。

| 项目 | Debug | Release |
|------|-------|---------|
| 关卡 / UI / 存档逻辑 | 相同 | 相同 |
| `assets/www` 文件 | 相同（已校验） | 相同 |
| 包体大小 | 较大（未 R8 压缩） | 较小（`minifyEnabled true`） |
| WebView 远程调试 | 开启（`BuildConfig.DEBUG`） | 关闭 |
| 原生桥 `Native`（震动等） | 有 | 有（ProGuard 已 keep） |

发版前可跑一致性校验：

```bash
.\gradlew.bat verifyApkAssetsParity
```

通过即表示两个 APK 内 `assets/www` 字节级一致。

## A. 折叠屏（优先）

| # | 场景 | 操作 | 预期 |
|---|------|------|------|
| A1 | 外屏横屏 · 进关 | 第一关开战 | 地图铺满中间区域；可见路径与敌军；无明显横向拉扁 |
| A2 | 外屏 · 拖动 | 单指在画布上拖动 | 可查看路径全程（上下/左右） |
| A3 | 外屏 · 缩放 | 点右侧 ＋／－／视野，或双指捏合 | 能放大缩小；视野复位有效 |
| A4 | 外屏 · 塔面板 | 点选已部署武将 | 升级/大招/合成/撤退按钮完整可见可点 |
| A5 | 内屏横屏 · 进关 | 同一关卡 | 整图比例正常；与内屏历史体验一致 |
| A6 | 外屏↔内屏 | 战斗中折叠切换 | 不崩溃；布局重算后仍可操作 |
| A7 | 外屏 · 进关聚焦 | 任意关开战（尤其 1、5、15、30） | **道路 + 塔位**出现在 HUD 安全区中部（非贴顶）；与画布 `data-layout-mode="fill-pan"` 一致 |
| A8 | 外屏 · 最小缩放 | 点「−」或捏合缩至最小 | **整条道路 + 全部塔位**均在可视区内；无需再拖才能布阵 |
| A9 | 外屏 · 复位 | 双击画布或点「视野」 | 回到进关时的聚焦缩放；道路仍居中 |
| A10 | 外屏 · 全关抽检 | 抽 3～5 关（含虎牢/赤壁/五丈原） | 每关 A7–A9 均满足；地图背景正常加载 |

### 外屏自动化验收（发版必跑）

折叠屏外屏（约 960×411 横屏）在 USB 连接真机且 App 已启动时：

```bash
node tools/test-campaign-viewport.mjs          # 默认第 1–30 关
node tools/device-acceptance.mjs               # 资产 / 选关 / 样板关战斗
node tools/test-level5-battle.mjs              # 第 5 关小沛战斗流程
```

或一键跑本地 + 真机门禁（见 [docs/BUILD.md](docs/BUILD.md)）：

```bash
node tools/pre-release.mjs
```

**通过标准（视口脚本）**：30/30 关 `pass`；`contentCy` 约 220–380；进关时道路中心与 HUD 安全区中心偏差 ≤12%；最小缩放下路径点与塔位全在屏内；单指拖动有效。

报告输出：`tools/acceptance-captures/campaign-viewport-report.json`。

## B. 普通手机横屏（16:9～20:9）

| # | 场景 | 操作 | 预期 |
|---|------|------|------|
| B1 | 进关 | 任意已解锁关 | 地图完整可见或合理缩放；无黑边小窗 |
| B2 | 布阵 | 点底部武将 → 点空地 | 正常部署 |
| B3 | 暂停/返回 | 暂停 → 继续 / 放弃 | 状态正确 |

## C. 进度与设置

| # | 场景 | 操作 | 预期 |
|---|------|------|------|
| C1 | 存档 | 通关一关 → 杀进程重开 | 解锁与星级保留 |
| C2 | 重置 | 选关页「重置战役」 | 进度清空 |
| C3 | 设置 | 主菜单「设置」改画质/音效/自动大招 | 立即或重进战斗后生效 |
| C4 | 教程 | 新装或清空数据后第一关 | 出现短引导；完成后不再强制弹出 |

## D. 音效（若已开启）

| # | 操作 | 预期 |
|---|------|------|
| D1 | 部署武将 | 短提示音 |
| D2 | 击杀 / 胜利 / 失败 | 各有反馈（设置中可关） |

## E. 内测第二版专项检查

| # | 场景 | 操作 | 预期 |
|---|------|------|------|
| E1 | 覆盖安装 | 已装旧版 → 装 `app-release.apk` | 可安装；进度保留 |
| E2 | 清数据 | 系统清除应用数据后进第一关 | 新手引导再次出现 |
| E3 | 刘备 | 通关第 4 关后 | 刘备加入；图鉴可见 |
| E4 | 大招 | 释放任意大招 | 屏闪/路径高亮/音效；BOSS 震感更强 |
| E5 | 地图 | 赤壁/雪地等关 | 地标（皇城/水面/火迹）可见 |

## F. 发版前自动化（CI / 本地）

| # | 命令 | 预期 |
|---|------|------|
| F1 | `.\gradlew.bat verifyApkAssetsParity` | debug / release 的 `assets/www` SHA-256 一致 |
| F2 | `node tools/audit-campaign-balance.mjs` | 30 关数值无异常尖峰；报告见 `tools/acceptance-captures/balance-report.json` |
| F3 | `node tools/verify-art-manifest.mjs --apk app/build/outputs/apk/debug/app-debug.apk` | manifest 与 APK 内图片一致；超体积文件有警告 |
| F4 | `node tools/pre-release.mjs --device` | F1–F3 + 真机视口 / 验收（需 USB 设备） |
| F5 | `node tools/ult-vfx-test.mjs` | 6 种大招特效均触发（ flood/blaze/stun/maze/execute/charge ） |

## 回归记录（手工填写）

| 日期 | 测试人 | 包类型 release/debug | A1-A10 | B1-B3 | C1-C4 | F1-F4 | 备注 |
|------|--------|----------------------|--------|-------|-------|-------|------|
| 2026-05-26 | 用户+adb | release | ☑ A1-A6 | ☑ | ☑ | — | SM-F9660 全项通过；含 A4/A6、C4、D、E4 及 B2/B3、C1-C3 用户复测无问题 |
| 2026-06-10 | adb 自动化 | debug | ☑ A7-A10（30/30） | — | — | ☑ F1-F3 | 外屏道路居中 + 最小缩放全可见；见 campaign-viewport-report.json |

## 备注

- 外屏判定：`fill-pan` 布局，见画布 `data-layout-mode`（开发者工具）或右侧缩放条是否显示。
- 内测分发说明见 [docs/BETA.md](docs/BETA.md)。
- 问题请记：机型、内/外屏、关卡号、截图。
