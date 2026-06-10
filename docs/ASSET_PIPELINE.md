# 美术资源管线

目标：manifest 与 APK 内图片一致；立绘 **<200KB**、地图/UI **<400KB**（见 [ART_PLAN.md](ART_PLAN.md)）。

## 目录

```
app/src/main/assets/www/assets/
  manifest.json          # 唯一索引，由 sync 脚本生成
  ui/menu_bg.jpg
  portraits/{id}.png
  maps/{mapTheme}/bg.jpg
```

**源图**（默认不在仓库）：`%USERPROFILE%\.cursor\projects\c-GT-123-game-AndroidVer\assets\`  
由 Cursor 出图或手工导出后放入该目录。

## 工作流

```powershell
# 1. 从源目录同步到 www/assets 并重建 manifest
.\tools\sync-art-assets.ps1

# 2. 超体积文件压缩（需本机 ImageMagick 或 .NET Drawing）
.\tools\compress-art.ps1

# 3. 校验 manifest ↔ 磁盘 / APK
node tools/verify-art-manifest.mjs
node tools/verify-art-manifest.mjs --apk app/build/outputs/apk/debug/app-debug.apk

# 4. 重建 APK
.\gradlew.bat assembleDebug
```

## 体积说明

- Debug APK ~170MB：主要为 **未压缩 JPG/PNG** 全量打入 `assets/www/assets`。
- 运行 `.\tools\compress-art.ps1` 后工作区资源约 **15–20MB**（立绘 <200KB、地图 <400KB）；需重新 `assembleDebug` 才能反映到 APK。
- 工作区可能仅有 `manifest.json`（大图仅存在于本机构建/APK）；CI 用 `--apk` 校验。

## 发版门禁

`node tools/pre-release.mjs` 会跑 `verify-art-manifest.mjs`（有 debug APK 时对照包内文件）。
