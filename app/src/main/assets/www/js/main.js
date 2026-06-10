window.Main = {
    currentLevel: 0,
    pendingLevelIndex: 0,
    lineupIds: ["guanyu"],
    lastT: 0,

    openLineup(idx) {
        if (window.UI && UI.openLineup) UI.openLineup(idx);
    },

    init() {
        UI.initMenu();
        UI.initGameUI();
        const canvas = document.getElementById("canvas");
        Game.init(canvas);
        Game.onUpdate = () => {
            UI.refreshTopbar();
            UI.refreshGeneralBarAffordability();
            UI.refreshTowerPanel();
        };
        Game.onWaveChange = (i, total) => {
            if (i === total) {
                const bn = Game.level && Game.level.bossName;
                UI.showBanner(bn ? `最终波 · 关底敌将 ${bn} 即将出现` : "最终波 · 关底敌将即将出现", 1800);
            } else {
                UI.showBanner(`第 ${i} 波 / 共 ${total} 波`);
            }
        };
        Game.onBossSpawn = (bossName) => {
            if (window.Sfx) Sfx.play("boss");
            if (window.Haptics) Haptics.boss();
            UI.showBanner(`关底敌将 · ${bossName} 来袭`, 2000, "hero");
            const gs = document.getElementById("game-screen");
            if (gs) {
                gs.classList.add("boss-vignette");
                setTimeout(() => gs.classList.remove("boss-vignette"), 900);
            }
        };
        Game.onResult = (state) => {
            setTimeout(() => UI.showResult(state), 600);
        };
        requestAnimationFrame(this._loop.bind(this));
    },

    startGame(idx, lineupIds) {
        this.currentLevel = idx;
        this.lineupIds = (lineupIds && lineupIds.length)
            ? lineupIds.slice(0, Campaign.MAX_LINEUP)
            : (Progress.getDefaultLineupForLevel(idx) || ["guanyu"]);
        UI.buildGeneralBar(this.lineupIds);
        UI.show("game-screen");
        if (window.MobileBridge) {
            requestAnimationFrame(() => MobileBridge.onViewportChanged());
        }
        Game.loadLevel(LEVELS[idx]);
        // Demo 模式：URL 含 ?demo 时给无限金币 + 不掉血，便于演示
        const demoBar = document.getElementById("demo-bar");
        if (location.search.includes("demo")) {
            Game.gold = 9999;
            Game._demoMode = true;
            if (demoBar) demoBar.classList.remove("hidden");
        } else {
            Game._demoMode = false;
            if (demoBar) demoBar.classList.add("hidden");
        }
        UI.refreshTopbar();
        UI.refreshGeneralBarAffordability();
        UI.refreshTowerPanel();
        const lv = LEVELS[idx];
        UI.showBanner(lv.name, 1600, "hero");
        if (lv.winObjectiveText) {
            setTimeout(() => UI.showBanner("胜利：" + lv.winObjectiveText, 2400, "toast"), 500);
        }
        if (window.UI && UI.maybeShowTutorial) {
            setTimeout(() => UI.maybeShowTutorial(idx), 900);
        }
    },

    _loop(t) {
        // 后台时暂停循环，重置时间避免跳帧
        if (document.hidden) {
            this.lastT = 0;
            requestAnimationFrame(this._loop.bind(this));
            return;
        }
        const dt = Math.min(0.05, (t - this.lastT) / 1000 || 0);
        this.lastT = t;
        Game.update(dt);
        // 渲染（仅在游戏屏激活时）
        if (document.getElementById("game-screen").classList.contains("active")) {
            Game.render();
            // 持续刷新塔面板（怒气 / 选中）
            if (Game.selectedTower && !Game.mergePickSource) UI.refreshTowerPanel();
            // 持续刷新顶栏（金钱）
            UI.refreshTopbar();
            UI.refreshGeneralBarAffordability();
        }
        requestAnimationFrame(this._loop.bind(this));
    }
};

// 快捷键（桌面浏览器可用，移动端自动忽略）
window.addEventListener("keydown", (e) => {
    // R = 当前选中武将满怒气
    if (e.key === "r" || e.key === "R") {
        if (Game.selectedTower) {
            Game.selectedTower.rage = Game.selectedTower.maxRage;
            if (Game.onUpdate) Game.onUpdate();
        }
    }
    // F = 当前选中武将立即释放大招（强制满怒气并释放）
    if (e.key === "f" || e.key === "F") {
        if (Game.selectedTower) {
            const nm = Game.selectedTower.general.ultimate.name;
            Game.selectedTower.rage = Game.selectedTower.maxRage;
            Game.castUltSelected();
            if (window.UI && UI.showUltToast) UI.showUltToast("已释放 · " + nm);
        }
    }
    // P = 暂停切换
    if (e.key === "p" || e.key === "P") {
        if (Game.state === "running") Game.pause();
        else if (Game.state === "paused") Game.resume();
    }
});

window.addEventListener("DOMContentLoaded", () => {
    if (window.Progress) Progress.load();
    const boot = () => {
        if (window.Sfx) {
            Sfx.init();
            document.addEventListener("click", () => {
                if (Sfx._ensureCtx) Sfx._ensureCtx();
            }, { once: true, capture: true });
        }
        Main.init();
        if (window.MobileBridge) {
            setTimeout(() => MobileBridge.init(), 200);
        }
    };
    if (window.ArtAssets) {
        ArtAssets.init().finally(boot);
    } else {
        boot();
    }
});
