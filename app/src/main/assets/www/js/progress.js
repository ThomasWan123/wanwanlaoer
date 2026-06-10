window.Progress = {
    STORAGE_KEY: "sanguotd_campaign_v1",
    data: null,

    _defaultData() {
        return {
            version: 1,
            maxCleared: -1,
            lastLineup: ["guanyu"],
            ultAuto: true,
            renderQuality: "auto",
            sfxEnabled: true,
            tutorialDone: false,
            firstBattleHintShown: false,
            levelStars: {},
            levelStats: {}
        };
    },

    _migrate() {
        const d = this.data;
        if (typeof d.ultAuto !== "boolean") d.ultAuto = true;
        if (!d.renderQuality) d.renderQuality = "auto";
        if (!d.lastLineup || !d.lastLineup.length) d.lastLineup = ["guanyu"];
        if (typeof d.sfxEnabled !== "boolean") d.sfxEnabled = true;
        if (typeof d.tutorialDone !== "boolean") d.tutorialDone = false;
        if (typeof d.firstBattleHintShown !== "boolean") d.firstBattleHintShown = false;
        if (!d.levelStars || typeof d.levelStars !== "object") d.levelStars = {};
        if (!d.levelStats || typeof d.levelStats !== "object") d.levelStats = {};
        const maxIdx = (window.CAMPAIGN_META ? CAMPAIGN_META.length : 30) - 1;
        if (typeof d.maxCleared !== "number" || d.maxCleared < -1) d.maxCleared = -1;
        if (d.maxCleared > maxIdx) d.maxCleared = maxIdx;
    },

    load() {
        try {
            const raw = localStorage.getItem(this.STORAGE_KEY);
            this.data = raw ? JSON.parse(raw) : null;
        } catch (e) {
            this.data = null;
        }
        if (!this.data || typeof this.data.version !== "number") {
            this.data = this._defaultData();
            this.save();
        } else {
            this._migrate();
        }
        return this.data;
    },

    save() {
        localStorage.setItem(this.STORAGE_KEY, JSON.stringify(this.data));
    },

    get maxCleared() {
        return this.data ? this.data.maxCleared : -1;
    },

    isLevelUnlocked(index) {
        return index === 0 || index <= this.maxCleared + 1;
    },

    getCurrentTargetIndex() {
        const next = this.maxCleared + 1;
        if (next >= CAMPAIGN_META.length) return -1;
        return next;
    },

    getUnlockedGeneralIds() {
        const ids = new Set(STARTER_GENERAL_IDS);
        const max = this.maxCleared;
        for (let i = 0; i <= max && i < CAMPAIGN_META.length; i++) {
            ids.add(CAMPAIGN_META[i].bossGeneralId);
        }
        for (const m of MILESTONE_GENERALS) {
            if (max >= m.unlockAfterLevel) ids.add(m.id);
        }
        return Array.from(ids).filter(id => window.getGeneral && getGeneral(id));
    },

    computeStars(victory, leaks, life, startLife) {
        if (!victory) return 0;
        if (leaks === 0) return 3;
        if (leaks <= 1 && life >= Math.ceil(startLife * 0.5)) return 2;
        return 1;
    },

    recordLevelResult(levelIndex, result) {
        if (!this.data) this.load();
        const stars = this.computeStars(
            result.victory,
            result.leaks || 0,
            result.life,
            result.startLife
        );
        const key = String(levelIndex);
        const prevStars = this.data.levelStars[key] || 0;
        if (stars > prevStars) this.data.levelStars[key] = stars;

        const prev = this.data.levelStats[key];
        const stats = {
            leaks: result.leaks || 0,
            kills: result.kills || 0,
            goldLeft: result.goldLeft || 0,
            lifeLeft: result.life,
            stars: Math.max(prevStars, stars)
        };
        if (!prev || stats.stars > (prev.stars || 0)
            || (stats.stars === (prev.stars || 0) && stats.leaks < (prev.leaks ?? 99))) {
            this.data.levelStats[key] = stats;
        }
        this.save();
        return stars;
    },

    getLevelStars(levelIndex) {
        if (!this.data || !this.data.levelStars) return 0;
        return this.data.levelStars[String(levelIndex)] || 0;
    },

    getLevelStats(levelIndex) {
        if (!this.data || !this.data.levelStats) return null;
        return this.data.levelStats[String(levelIndex)] || null;
    },

    starsDisplay(n) {
        const s = Math.max(0, Math.min(3, n || 0));
        let out = "";
        for (let i = 0; i < 3; i++) out += i < s ? "★" : "☆";
        return out;
    },

    onVictory(levelIndex) {
        const prev = this.maxCleared;
        if (levelIndex > this.maxCleared) {
            this.data.maxCleared = levelIndex;
            this.save();
        }
        const newlyBoss = levelIndex > prev ? CAMPAIGN_META[levelIndex].bossGeneralId : null;
        let newlyMilestone = null;
        for (const m of MILESTONE_GENERALS) {
            if (m.unlockAfterLevel === levelIndex && levelIndex > prev) {
                newlyMilestone = m.id;
            }
        }
        return { newlyBoss, newlyMilestone, bossId: CAMPAIGN_META[levelIndex].bossGeneralId };
    },

    setLastLineup(ids) {
        this.data.lastLineup = ids.slice(0, Campaign.MAX_LINEUP);
        this.save();
    },

    getDefaultLineupForLevel(levelIndex) {
        const unlocked = this.getUnlockedGeneralIds();
        const last = (this.data.lastLineup || []).filter(id => unlocked.includes(id));
        if (last.length) return last.slice(0, Campaign.MAX_LINEUP);
        return unlocked.slice(0, Math.min(Campaign.MAX_LINEUP, unlocked.length));
    },

    reset() {
        this.data = this._defaultData();
        this.save();
    },

    getRenderQuality() {
        const q = this.data && this.data.renderQuality;
        if (q === "performance" || q === "quality") return q;
        return "auto";
    },

    setRenderQuality(q) {
        if (!this.data) this.load();
        this.data.renderQuality = (q === "performance" || q === "quality") ? q : "auto";
        this.save();
        if (window.MobileBridge && MobileBridge.onViewportChanged) MobileBridge.onViewportChanged();
    },

    getUltAuto() {
        return this.data ? this.data.ultAuto !== false : true;
    },

    setUltAuto(on) {
        if (!this.data) this.load();
        this.data.ultAuto = !!on;
        this.save();
    },

    getSfxEnabled() {
        return this.data ? this.data.sfxEnabled !== false : true;
    },

    setSfxEnabled(on) {
        if (!this.data) this.load();
        this.data.sfxEnabled = !!on;
        this.save();
        if (window.Sfx) Sfx.setEnabled(this.data.sfxEnabled);
    },

    isTutorialDone() {
        return this.data ? !!this.data.tutorialDone : false;
    },

    setTutorialDone(done) {
        if (!this.data) this.load();
        this.data.tutorialDone = !!done;
        this.save();
    }
};
