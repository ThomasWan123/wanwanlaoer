/**
 * 战役 30 关数值审计：金币 / 生命 / 波次 / 敌量 / 塔位密度
 * 输出 tools/acceptance-captures/balance-report.json
 */
import fs from "node:fs";
import vm from "node:vm";
import path from "node:path";

const ROOT = process.cwd();
const JS = path.join(ROOT, "app", "src", "main", "assets", "www", "js");
const OUT = path.join(ROOT, "tools", "acceptance-captures");

function loadContext() {
    const ctx = { window: {}, console, Math, JSON, Array, Object, Number, String, Boolean, Set };
    ctx.window = ctx;
    for (const rel of [
        "util.js",
        "data/regions.js",
        "data/campaign.js",
        "data/paths.js",
        "data/waveBuilder.js",
        "data/levelFactory.js"
    ]) {
        vm.runInNewContext(fs.readFileSync(path.join(JS, rel), "utf8"), ctx, { filename: rel });
    }
    return ctx;
}

function estimateSpawns(waves) {
    let total = 0;
    let maxWave = 0;
    for (const w of waves) {
        let waveTotal = 0;
        for (const s of w.spawns || []) {
            waveTotal += s.count || 0;
            total += s.count || 0;
        }
        maxWave = Math.max(maxWave, waveTotal);
    }
    return { total, maxWave, waveCount: waves.length };
}

function phaseOf(index) {
    if (index < 6) return "early";
    if (index < 18) return "mid";
    return "late";
}

function avg(arr) {
    return arr.length ? arr.reduce((a, b) => a + b, 0) / arr.length : 0;
}

function main() {
    const ctx = loadContext();
    const { LevelFactory, CAMPAIGN_META } = ctx;
    const levels = LevelFactory.buildAll();

    const rows = levels.map((lv, i) => {
        const sp = estimateSpawns(lv.waves);
        const goldPerEnemy = sp.total > 0 ? lv.startGold / sp.total : lv.startGold;
        return {
            index: i,
            level: i + 1,
            name: lv.name,
            phase: phaseOf(i),
            tier: CAMPAIGN_META[i].tier,
            startGold: lv.startGold,
            life: lv.life,
            slotCount: lv.slots.length,
            waveCount: sp.waveCount,
            spawnTotal: sp.total,
            maxWaveSpawns: sp.maxWave,
            goldPerSpawn: Math.round(goldPerEnemy * 10) / 10,
            boss: lv.bossName,
            hasBoss: lv.hasBoss
        };
    });

    const warnings = [];
    const phases = ["early", "mid", "late"];

    for (const ph of phases) {
        const subset = rows.filter(r => r.phase === ph);
        const avgGoldPer = avg(subset.map(r => r.goldPerSpawn));
        const avgLife = avg(subset.map(r => r.life));
        const avgSpawn = avg(subset.map(r => r.spawnTotal));

        if (ph === "early" && avgGoldPer < 3.5) {
            warnings.push({ phase: ph, code: "GOLD_TIGHT", severity: "warn", detail: `新手段金币/敌偏低 (${avgGoldPer.toFixed(1)})` });
        }
        if (ph === "mid" && avgGoldPer < 2.0) {
            warnings.push({ phase: ph, code: "GOLD_TIGHT", severity: "warn", detail: `中段金币/敌偏低 (${avgGoldPer.toFixed(1)})` });
        }
        if (ph === "late" && avgLife < 6) {
            warnings.push({ phase: ph, code: "LIFE_LOW", severity: "warn", detail: `后段平均生命偏低 (${avgLife.toFixed(1)})` });
        }
        if (ph === "late" && avgSpawn > 320) {
            warnings.push({ phase: ph, code: "SPAWN_SPIKE", severity: "warn", detail: `后段敌量偏高 (${avgSpawn.toFixed(0)})` });
        }

        console.log(`\n[${ph}] 关卡 ${subset[0].level}–${subset[subset.length - 1].level}`);
        console.log(`  平均金币 ${avg(subset.map(r => r.startGold)).toFixed(0)} | 生命 ${avgLife.toFixed(1)} | 敌量 ${avgSpawn.toFixed(0)} | 金币/敌 ${avgGoldPer.toFixed(1)}`);
    }

    for (const r of rows) {
        if (r.life <= 6 && r.spawnTotal >= 120) {
            warnings.push({ level: r.level, code: "HARD_COMBO", severity: "fail", detail: `${r.name} 生命${r.life} + 敌${r.spawnTotal}` });
        }
        if (r.goldPerSpawn < 1.5 && r.phase === "late") {
            warnings.push({ level: r.level, code: "GOLD_SPIKE", severity: "warn", detail: `${r.name} 金币/敌 ${r.goldPerSpawn}` });
        }
        if (r.slotCount < 8) {
            warnings.push({ level: r.level, code: "SLOTS_LOW", severity: "fail", detail: `${r.name} 仅 ${r.slotCount} 塔位` });
        }
    }

    const bossWaveElite = levels.map((lv, i) => {
        const last = lv.waves[lv.waves.length - 1];
        const elite = (last.spawns || []).filter(s => s.type !== "boss").reduce((n, s) => n + s.count, 0);
        return { level: i + 1, name: lv.bossName, elite, delay: last.delay };
    });

    const report = {
        time: new Date().toISOString(),
        levelCount: rows.length,
        phases: phases.map(ph => {
            const subset = rows.filter(r => r.phase === ph);
            return {
                phase: ph,
                levels: `${subset[0].level}-${subset[subset.length - 1].level}`,
                avgGold: Math.round(avg(subset.map(r => r.startGold))),
                avgLife: Math.round(avg(subset.map(r => r.life)) * 10) / 10,
                avgSpawnTotal: Math.round(avg(subset.map(r => r.spawnTotal))),
                avgGoldPerSpawn: Math.round(avg(subset.map(r => r.goldPerSpawn)) * 10) / 10
            };
        }),
        bossWaves: bossWaveElite,
        warnings,
        rows,
        pass: warnings.filter(w => w.severity === "fail" || (!w.severity && ["HARD_COMBO", "SLOTS_LOW"].includes(w.code))).length === 0
    };

    fs.mkdirSync(OUT, { recursive: true });
    fs.writeFileSync(path.join(OUT, "balance-report.json"), JSON.stringify(report, null, 2));

    console.log("\n=== 战役平衡审计 ===");
    console.log(`关卡: ${report.levelCount} | 警告: ${warnings.length} | 通过: ${report.pass ? "是" : "否"}`);
    for (const w of warnings.slice(0, 12)) {
        console.log(`  ⚠ ${w.level ? `第${w.level}关` : w.phase} [${w.code}] ${w.detail}`);
    }
    for (const idx of [2, 14, 27, 29]) {
        const b = bossWaveElite[idx];
        console.log(`  BOSS 第${b.level}关 ${b.name}: 精英${b.elite} delay=${b.delay}s`);
    }

    process.exit(report.pass ? 0 : 1);
}

main();
