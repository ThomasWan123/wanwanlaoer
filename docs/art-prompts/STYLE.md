# 已用 AI 出图 Prompt（手绘亮调 · 谋定天下向）

全局 Style Lock（每条末尾已包含语义）：

```text
premium hand-painted Chinese Three Kingdoms mobile SLG, bright warm lighting,
refined guofeng, gold-jade-silk palette, no text, no logo, original design
```

## UI

### menu_bg.jpg

```text
Mobile game main menu background, Three Kingdoms hand-painted guofeng art style,
bright warm golden hour lighting, elegant Chinese scroll and jade gold ornaments,
distant green mountains and ancient fortress silhouette, soft luminous clouds,
empty lower third for UI buttons, landscape 16:9, no text
```

## 立绘（portraits/{id}.png）

模板：`Hand-painted SLG hero portrait bust, {角色英文名+特征}, bright warm upper-left light, soft gradient background, no text`

| id | 特征关键词 |
|----|-----------|
| guanyu | long beard, green gold armor, guan dao |
| zhangfei | fierce face, black beard, red accent armor |
| zhaoyun | silver armor, red cape, spear |
| zhugeliang | feather fan, green white robe, wise smile |
| liubei | yellow robe, kind dignified |
| lvbu | red plume, golden armor, halberd |
| zhouyu | blue white robe, jade pendant |
| caocao | blue purple Wei armor, sharp eyes |
| sunquan | red gold Jiangdong robe |

## 地图（maps/{mapTheme}/bg.jpg）

| mapTheme | Prompt 要点 |
|----------|-------------|
| central_fort | Hulao Pass fortress, bright daylight, yellow earth path |
| east_fire | Chibi burning ships, Yangtze, dramatic bright sky |
| central_winter | Wuzhang snow plains, pale blue sky, tents |

## 再生图

在 Cursor Agent 模式说明 id / mapTheme，或复制上表 prompt 到 Midjourney / Flux，保持同一 Style Lock 以统一画风。
