# 下周一工作清单

> **工作流**：D = 正式版 · C = 开发版 · C 试验 → 测试 → 同步回 D

## 0. 开始前确认

- [ ] C 与 D 已对齐（若不确定，先跑 `.\tools\sync-d-to-c.ps1`）
- [ ] 手机已连接：`adb devices` 可见 `RFCY71HFAYX`（SM-F9660）
- [ ] 在 **C** 目录操作：`cd C:\GT-123\game\AndroidVer`

## 1. SOLO 视觉补丁（在 C 上重做）

原补丁针对旧版 `art.js`（`MAP_THEMES`），当前 D 基线使用 `_drawMapAmbience`，**不能整包 git apply**，需按文件手动合并。

参考 diff：`tools/solo-patches/`

| 顺序 | 补丁 | 文件 | 说明 |
|------|------|------|------|
| 01 | `patch_01_level_theme_runtime.diff` | `main.js`, `ui.js` | body 关卡主题；离开 game-screen 清除主题 |
| 02 | `patch_02_ui_tokens_and_themes.diff` | `style.css` | CSS 变量 `--ui-*` + 主题覆盖 |
| 03 | `patch_03_map_biomes_artjs.diff` | `art.js` | 地图生物群系 — **需并入 `_drawMapAmbience`，勿整段替换** |
| 04 | `patch_04_characters_and_ultimates.diff` | `art.js` | 塔 idle 起伏、武将细节、大招 VFX |
| 05 | `patch_05_vfx_intensity_tuning.diff` | `art.js` | 大招强度微调（真机反馈版） |
| 05b | （上次会话手动改） | `art.js`, `effect.js` | Execute 可读性：`hold` 曲线 + `duration 0.75` |

**D 基线已有（可跳过或只做增量）：**

- `levelFactory.js` 已有 `uiTheme: "lv-theme-" + meta.region`
- `style.css` 已有 `.level-card.lv-theme-*` 三行边框样式
- `main.js` 的 `startGame` 签名与 patch_01 不同（含 lineup），合并时注意挂到 `startGame` 而非旧版 `startGame(idx)` 单行逻辑

### 建议合并顺序

1. patch_01 → `main.js` + `ui.js`（body 主题运行时）
2. patch_02 → `style.css`（在现有 lv-theme 规则上扩展 token）
3. patch_04 + 05 + 05b → `art.js` 大招/塔/武将（与 `_drawMapAmbience` 独立，冲突较少）
4. patch_03 最后做 — 对照 D 的 `_drawMapAmbience` 逐段移植 hills/river/scorch/snow/城门

## 2. 构建与真机测试（C）

```powershell
cd C:\GT-123\game\AndroidVer
.\gradlew assembleDebug
adb -s RFCY71HFAYX install -r app\build\outputs\apk\debug\app-debug.apk
```

### 大招 VFX 自动化（可选）

```powershell
# 需 WebView 调试 + adb forward
node tools\ult-vfx-test.mjs
node tools\ult-execute-capture.mjs
```

### 手动检查项

- [ ] 进关后 body 主题 class 正确，回主菜单后清除
- [ ] 地图 ambience 各区域可区分（雪/火/河等）
- [ ] 大招：flood / blaze / stun / maze / execute / charge 均可见
- [ ] execute 约 100ms 内可读（05b）
- [ ] logcat 无 JS 报错

## 3. 试验通过后同步到 D

```powershell
# 推荐带备份
.\tools\sync-c-to-d.ps1 -Backup

# 在 D 打 release
cd D:\GT-123\game\AndroidVer
.\gradlew assembleRelease
adb -s RFCY71HFAYX install -r app\build\outputs\apk\release\app-release.apk
```

## 4. 常用脚本

| 脚本 | 作用 |
|------|------|
| `tools\sync-d-to-c.ps1` | D → C，重置/拉取正式版 |
| `tools\sync-c-to-d.ps1` | C → D，试验通过后推送（`-Backup` 先备份 D） |
| `tools\sync-c-to-d.ps1 -Force` | 跳过确认（脚本/CI 用） |

## 5. 版本号提醒

同步后若 C 的 `versionCode` 高于 D，安装 release 可能触发降级拦截 — 需先卸载或统一 bump `app/build.gradle` 中的 `versionCode` / `versionName`。
