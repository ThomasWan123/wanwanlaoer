# 路线图

## 目标

1. **内测**：朋友可安装的 `app-release.apk`
2. **开源**：GitHub 公开仓库（MIT）
3. **体验**：UI、地图、大招、7～30 关平衡
4. **内容**：加将（刘备、曹操、孙权等）
5. **发行**：Google Play

## 已完成（基线 + 第二版 + 第三版打磨）

| 阶段 | 内容 |
|------|------|
| 基线 | 30 关、进度/星级、Fold、`viewport`、教程/设置/音效、`verifyApkAssetsParity` |
| A | [BETA.md](BETA.md)、TESTING、README/docs、CI + Release APK artifact |
| B | UI 美化、地图环境/地标、大招震感+音效、1～6 关缓和 |
| C | 刘备、**曹操**（`hex`）、**孙权**（`tide`）及里程碑解锁 |
| 打磨续 | 7～30 关 `WaveBuilder`/`LevelFactory` 分段缓和；火/雪/水/帝都地图强化 |
| 发行预备 | MIT [LICENSE](../LICENSE)、[RELEASE.md](RELEASE.md)、[PLAY.md](PLAY.md)、真机截图 |

## 待你本地完成

- 配置正式 `keystore.properties` 后 `bundleRelease` 上架 Play
- Play 控制台物料（图标 512、截图、分级问卷）
- `git push` 公开仓库（若尚未推送）

## 加将约定

每新增一将：

1. [`generals.js`](../app/src/main/assets/www/js/data/generals.js)
2. [`campaign.js`](../app/src/main/assets/www/js/data/campaign.js) `MILESTONE_GENERALS`
3. 新大招类型：[`effect.js`](../app/src/main/assets/www/js/entities/effect.js) + [`art.js`](../app/src/main/assets/www/js/art.js) + [`audio.js`](../app/src/main/assets/www/js/audio.js)
4. 图鉴自动收录（`generals-registry.js`）

## 暂不计划

- 50+ 关卡、装备天赋、联机、换引擎、云存档
