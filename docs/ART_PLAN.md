# 美术升级计划 — 手绘亮调国风（谋定天下向）

> **状态**：执行中 · **风格**：精致手绘国风、偏亮、暖金/绢帛色调  
> **参考**（仅学风格，不抄资产）：三国谋定天下、三国志战略版、三国天下归心  
> **原则**：资产驱动 + 程序绘制兜底；玩法/数值不改；不协作 SOLO 补丁

## 风格锁（Style Lock）

所有 AI 出图 prompt 末尾统一附加：

```text
Style: premium hand-painted Chinese Three Kingdoms mobile SLG art, bright warm
lighting, refined guofeng, gold-jade-silk palette, luminous sky, semi-realistic
hero portraits, layered 2.5D battlefield backgrounds, vivid but elegant, game asset,
no text, no watermark, no logo, original design.
Negative: dark muddy, anime chibi, cyberpunk, flat vector, copy existing game UI.
```

**色调**：主色 `#f7d774` 金、`#3a6b4a` 蜀绿、`#2a5080` 魏蓝、`#8a3030` 吴红；背景比当前 D 基线**更亮约 20%**。

## 目录结构

```
app/src/main/assets/www/assets/
  manifest.json
  ui/menu_bg.jpg
  ui/panel_texture.png      # 可选
  portraits/{id}.png          # guanyu, zhangfei, …
  maps/{mapTheme}/bg.jpg      # 单层背景（样板关）
  maps/{mapTheme}/thumb.jpg   # 选关缩略图（可选）
```

## 分阶段

| 阶段 | 内容 | 验收 |
|------|------|------|
| **1** | UI 壳：主菜单底图、CSS 接入 | 打开即像商业 SLG |
| **2** | 9 将立绘 PNG | 5 秒内认出武将 |
| **3** | 3 关地图：虎牢(`central_fort`)、赤壁(`east_fire`)、五丈原(`central_winter`) | 3 秒内认出场景 |
| **4** | 朋友盲测 3 关 → 满意后铺 30 关 | — |
| **5** | 并行 Play 上架（keystore / AAB） | ROADMAP |

## 武将清单（`generals.js`）

| id | 名 | 阵营色点缀 |
|----|-----|-----------|
| guanyu | 关羽 | 绿金 |
| zhangfei | 张飞 | 黑红 |
| zhaoyun | 赵云 | 银蓝 |
| zhugeliang | 诸葛亮 | 青白羽扇 |
| liubei | 刘备 | 仁德黄 |
| lvbu | 吕布 | 赤金戟 |
| zhouyu | 周瑜 | 儒将蓝 |
| caocao | 曹操 | 魏蓝紫 |
| sunquan | 孙权 | 江东红 |

## 样板关卡

| 关名 | mapTheme | 画面要点 |
|------|----------|----------|
| 虎牢关 | `central_fort` | 关隘、黄土路、明亮日光 |
| 赤壁 | `east_fire` | 江面、火船、烟霞 |
| 五丈原 | `central_winter` | 雪原、军帐、冷蓝天空 |

## 工程接入

- `js/assets.js` — 预加载 manifest 资源，`ArtAssets.get(path)`
- `art.js` — `drawMap` / `drawPortrait` 优先 `drawImage`，失败走程序绘制
- `style.css` — `#menu` 使用 `menu_bg.jpg`；`.has-art-assets` 增强面板
- `index.html` — 引入 `assets.js`（在 `art.js` 之前）

## Prompt 模板

见各阶段 `docs/art-prompts/` 或本文「样板 Prompt」节。

### 主菜单

```text
Mobile game main menu background, Three Kingdoms hand-painted guofeng, bright
golden hour, scroll and jade ornaments, distant green mountains and fortress,
soft luminous clouds, empty lower third for buttons, [Style Lock]
```

### 立绘（改名字/武器）

```text
Hand-painted SLG hero portrait bust, {name}, Three Kingdoms general, {traits},
bright warm key light from upper left, transparent-friendly solid dark backdrop edge,
[Style Lock]
```

### 地图背景

```text
Hand-painted 2.5D SLG battlefield background, {scene}, bright daylight, layered
depth far-mid-near, clear central path area for troop route overlay, landscape 16:9,
[Style Lock]
```

## 版权

- 仅参考风格，不使用竞品官方素材
- 出图前确认所用 AI 平台商用条款

## 变更记录

| 日期 | 说明 |
|------|------|
| 2026-06-01 | 初版；定调手绘亮调；放弃 SOLO 补丁路线 |
| 2026-06-01 | Phase 1–3 执行：manifest、9 立绘、3 关地图、工程接入 |
| 2026-06-01 | v2.0.0 三国塔防2：27 mapTheme 全地图 + 38 将立绘；applicationId 独立安装 |

## 资产体积说明

当前 AI 出图约 2MB/张，打包前建议 WebP 压缩（目标立绘 <200KB、地图 <400KB）。可用 `tools/compress-art.ps1` 或手工导出。

## 下一步

- [ ] 朋友盲测虎牢/赤壁/五丈原三关
- [ ] 满意后批量出其余 `mapTheme` 背景
- [ ] 压缩资产减小 APK
- [ ] Play 上架（并行）
