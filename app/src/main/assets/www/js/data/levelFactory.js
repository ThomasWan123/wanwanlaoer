window.LevelFactory = {
    buildAll() {
        return CAMPAIGN_META.map((meta, i) => this.buildLevel(meta, i));
    },

    buildLevel(meta, index) {
        const bundle = PathGen.getBundle(meta, index);
        const modifiers = Regions.applyEnvironment(meta);
        const waves = WaveBuilder.buildWaves(meta, index);
        const tier = meta.tier || 1;

        let startGold = 280 + tier * 18 + index * 2;
        let life = Math.max(6, 13 - Math.floor(tier * 1.2) - Math.floor(index / 12));
        if (index < 6) {
            const goldTune = [300, 308, 316, 324, 332, 342];
            const lifeTune = [12, 12, 11, 11, 10, 10];
            startGold = goldTune[index];
            life = lifeTune[index];
        } else if (index < 15) {
            const midGold = [368, 376, 384, 392, 400, 408, 416, 424, 432];
            const midLife = [10, 10, 10, 9, 9, 9, 9, 8, 8];
            const mi = index - 6;
            startGold = midGold[mi] != null ? midGold[mi] : 368 + mi * 8;
            life = midLife[mi] != null ? midLife[mi] : 10;
        } else if (index < 25) {
            const lateGold = [440, 448, 456, 464, 472, 480, 488, 496, 504, 512];
            const lateLife = [8, 8, 8, 7, 7, 7, 7, 7, 7, 7];
            const li = index - 15;
            startGold = lateGold[li] != null ? lateGold[li] : 440 + li * 8;
            life = lateLife[li] != null ? lateLife[li] : 8;
        } else {
            startGold = 520 + (index - 25) * 8 + tier * 12;
            life = Math.max(6, 8 - Math.floor((index - 25) / 3));
        }

        return {
            id: meta.id,
            name: meta.name,
            desc: meta.desc,
            difficulty: Campaign.starsForTier(tier),
            startGold,
            life,
            path: bundle.path,
            slots: bundle.slots,
            obstacles: bundle.obstacles,
            modifiers,
            region: meta.region,
            terrain: meta.terrain,
            weather: meta.weather,
            mapTheme: meta.mapTheme,
            enemyPool: meta.enemyPool,
            bossGeneralId: meta.bossGeneralId,
            bossName: meta.bossName,
            hasBoss: !!meta.bossName,
            winObjectiveText: meta.bossName
                ? `击杀关底 · ${meta.bossName}，并歼灭全部敌军`
                : "守满全部波次，歼灭全部敌军",
            campaignIndex: index,
            uiTags: Regions.buildUiTags(meta),
            uiTheme: "lv-theme-" + meta.region,
            waves
        };
    }
};
