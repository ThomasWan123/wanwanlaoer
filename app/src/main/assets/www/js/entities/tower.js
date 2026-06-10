window.Tower = class Tower {
    constructor(general, x, y, slotIndex) {
        this.general = general;
        this.x = x;
        this.y = y;
        this.slotIndex = slotIndex;
        this.level = 1; // 1-3
        this.mergeTier = 0; // 0 普通 | 1 红将 | 2 金将
        this.cooldown = 0;
        this.aim = null;
        this.rage = 0;
        this.maxRage = 100;
        this.targetCache = null;
    }

    getBaseStats() { return this.general.levels[this.level - 1]; }

    getEffectiveStats() {
        const s = this.getBaseStats();
        const m = Math.pow(2, this.mergeTier);
        const rateMul = Math.pow(0.85, this.mergeTier);
        return {
            ...s,
            dmg: s.dmg * m,
            range: s.range * m,
            rate: s.rate * rateMul,
            splash: s.splash ? s.splash * m : 0,
            pierce: s.pierce || 0,
            slow: s.slow || 0,
            slowDur: s.slowDur || 0,
            upgradeCost: s.upgradeCost
        };
    }

    get stats() { return this.getEffectiveStats(); }
    get range() { return this.stats.range; }
    get rate() { return this.stats.rate; }
    get damage() { return this.stats.dmg; }

    mergeLabel() {
        if (this.mergeTier === 2) return "金将";
        if (this.mergeTier === 1) return "红将";
        return "";
    }

    nextMergeLabel() {
        if (this.mergeTier === 0) return "红将";
        if (this.mergeTier === 1) return "金将";
        return "";
    }

    canMergeWith(other) {
        if (!other || other === this) return false;
        if (other.general.id !== this.general.id) return false;
        if (this.level !== 3 || other.level !== 3) return false;
        if (this.mergeTier !== other.mergeTier) return false;
        if (this.mergeTier >= 2) return false;
        return true;
    }

    canUpgrade() { return this.level < 3; }
    upgradeCost() { return this.getBaseStats().upgradeCost; }

    upgrade() {
        if (this.level < 3) this.level += 1;
    }

    totalInvestedGold() {
        let cost = this.general.cost;
        for (let i = 0; i < this.level - 1; i++) cost += this.general.levels[i].upgradeCost;
        return cost;
    }

    sellValue() {
        let val = Math.floor(this.totalInvestedGold() * 0.6);
        val *= 1 + this.mergeTier * 0.5;
        return Math.floor(val);
    }

    update(dt, enemies, projectiles, effects) {
        this.cooldown -= dt;
        let t = null, bestProgress = -1;
        for (const e of enemies) {
            if (!e.alive) continue;
            const d = U.dist(this.x, this.y, e.x, e.y);
            if (d <= this.range) {
                if (e.progress > bestProgress) { bestProgress = e.progress; t = e; }
            }
        }
        if (t) {
            this.aim = U.angleTo(this.x, this.y, t.x, t.y);
            if (this.cooldown <= 0) {
                this.fire(t, projectiles, effects);
                this.cooldown = this.rate;
            }
        }
    }

    fire(target, projectiles, effects) {
        const g = this.general;
        const stats = this.stats;

        const proj = {
            x: this.x, y: this.y - 10,
            target,
            damage: stats.dmg,
            speed: 480,
            kind: g.projectileType,
            color: g.accent,
            splash: stats.splash || 0,
            slow: stats.slow || 0,
            slowDur: stats.slowDur || 0,
            pierce: stats.pierce || 1
        };

        if (g.id === "zhouyu") {
            proj.dotPerSec = stats.dmg * 0.4;
            proj.dotDur = 2;
            proj.dotIsFire = true;
            proj.hitIsFire = true;
        }
        if (g.id === "guanyu") proj.speed = 700;

        projectiles.push(new Projectile(proj));
        this.rage = Math.min(this.maxRage, this.rage + 12);
    }

    canUlt() { return this.rage >= this.maxRage; }
    consumeRage() { this.rage = 0; }
};
