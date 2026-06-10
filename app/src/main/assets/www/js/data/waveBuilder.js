window.WaveBuilder = {
    /** @returns {{ countMul: number, intervalAdd: number, delayAdd: number, eliteMul: number }} */
    _phaseEase(idx) {
        if (idx < 6) {
            return {
                countMul: 0.72 + idx * 0.04,
                intervalAdd: 0.12,
                delayAdd: 1,
                eliteMul: 0.7
            };
        }
        if (idx < 15) {
            const t = idx - 6;
            return {
                countMul: 0.92 - t * 0.012,
                intervalAdd: 0.06 - t * 0.003,
                delayAdd: 0,
                eliteMul: 0.88 - t * 0.01
            };
        }
        if (idx < 25) {
            const t = idx - 15;
            return {
                countMul: 0.82 - t * 0.008,
                intervalAdd: 0.04,
                delayAdd: 0,
                eliteMul: 0.92
            };
        }
        return { countMul: 0.9, intervalAdd: 0.02, delayAdd: 0, eliteMul: 0.92 };
    },

    buildWaves(meta, campaignIndex) {
        const pool = meta.enemyPool || ["yellow", "spear"];
        const tier = meta.tier || 1;
        const bossName = meta.bossName;
        const idx = typeof campaignIndex === "number" ? campaignIndex : 99;
        const early = idx < 6;
        const eased = idx < 25;
        const ease = this._phaseEase(idx);
        const waveCount = early ? Math.min(5 + tier, 6) : (5 + tier);
        const waves = [];
        const pick = (i) => pool[i % pool.length];

        for (let w = 0; w < waveCount - 1; w++) {
            const t = w / waveCount;
            let baseCount = Math.floor(6 + tier * 2 + w * 1.5);
            let interval = Math.max(0.28, 0.65 - tier * 0.04 - w * 0.02);
            if (eased) {
                baseCount = Math.max(4, Math.floor(baseCount * (ease.countMul + w * 0.03)));
                interval = Math.min(0.98, interval + ease.intervalAdd - w * 0.01);
            }
            const spawns = [];
            spawns.push({ type: pick(w), count: baseCount, interval });
            if (pool.length > 1) spawns.push({ type: pick(w + 1), count: Math.floor(baseCount * 0.6), interval: interval + 0.1, after: 2 });
            if (tier >= 2 && w >= 2 && pool.length > 2) {
                spawns.push({ type: pick(w + 2), count: Math.floor(3 + tier), interval: 1.0, after: 4 });
            }
            if (tier >= 3 && w === waveCount - 3 && pool.includes("shield")) {
                spawns.push({ type: "shield", count: 2 + tier, interval: 1.4, after: 3 });
            }
            const delay = early
                ? (w === 0 ? 3 : 11 + Math.floor(t * 5) + ease.delayAdd)
                : (w === 0 ? 2 + (eased ? 1 : 0) : 9 + Math.floor(t * 4) + (eased ? 1 : 0));
            waves.push({ delay, spawns });
        }

        const bossHpWave = {
            delay: early ? (14 + tier) : (12 + tier * 2 + (eased ? 1 : 0)),
            spawns: [
                { type: "boss", count: 1, interval: 1, name: bossName }
            ]
        };
        let eliteCount = 8 + tier * 3;
        if (eased) eliteCount = Math.max(5, Math.floor(eliteCount * ease.eliteMul));
        bossHpWave.spawns.push({ type: pick(waveCount), count: eliteCount, interval: Math.max(0.32, 0.5 - tier * 0.03), after: 1 });
        if (tier >= 2) bossHpWave.spawns.push({ type: pick(waveCount + 1), count: Math.floor(eliteCount * 0.5), interval: 0.85, after: 4 });
        if (tier >= 3 && pool.includes("desperado")) {
            bossHpWave.spawns.push({ type: "desperado", count: 4 + tier, interval: 0.75, after: 5 });
        }
        waves.push(bossHpWave);

        return waves;
    }
};
