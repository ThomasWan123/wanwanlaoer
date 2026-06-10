window.Enemy = class Enemy {
    constructor(typeKey, path, opts = {}) {
        this.typeKey = typeKey;
        this.type = Object.assign({}, ENEMY_TYPES[typeKey], opts);
        this.path = path;
        this.pathLen = U.pathLength(path);
        this.progress = 0;
        this.hp = this.type.hp;
        this.maxHp = this.type.hp;
        this.speed = this.type.speed;
        this.x = path[0].x;
        this.y = path[0].y;
        this.angle = 0;
        this.alive = true;
        this.reachedEnd = false;
        this.slowUntil = 0;
        this.slowFactor = 1;
        this.stunUntil = 0;
        this.dotEnd = 0;
        this.dotPerSec = 0;
        this.armor = this.type.armor || 0;
        this._dotAccum = 0;
        this._deathNovaFired = false;

        const L = typeof Game !== "undefined" && Game.level ? Game.level : null;
        const mods = L && L.modifiers;
        if (mods) {
            if (mods.enemySpeedMul) this.speed *= mods.enemySpeedMul;
            if (mods.riderSpeedMul && (typeKey === "rider" || typeKey === "tiger" || typeKey === "bingzhou_rider" || typeKey === "qiang_rider")) {
                this.speed *= mods.riderSpeedMul;
            }
            if (mods.scoutSpeedMul && typeKey === "scout") this.speed *= mods.scoutSpeedMul;
            if (mods.baggageSpeedMul && (typeKey === "baggage" || typeKey === "siege" || typeKey === "nanman_elite")) {
                this.speed *= mods.baggageSpeedMul;
            }
        }
    }

    update(dt) {
        if (!this.alive) return;

        const regen = this.type.regenPerSec;
        if (regen && this.hp > 0 && this.hp < this.maxHp) {
            this.hp = Math.min(this.maxHp, this.hp + regen * dt);
        }

        const now = performance.now();
        // DoT
        if (now < this.dotEnd) {
            this._dotAccum += dt;
            if (this._dotAccum >= 0.25) {
                this.takeDamage(this.dotPerSec * 0.25, true);
                this._dotAccum = 0;
            }
        }
        if (now < this.stunUntil) return; // 眩晕禁动

        let speed = this.speed;
        if (now < this.slowUntil) speed *= (1 - this.slowFactor);

        this.progress += speed * dt;
        if (this.progress >= this.pathLen) {
            this.alive = false;
            this.reachedEnd = true;
            return;
        }
        const p = U.pointOnPath(this.path, this.progress);
        this.x = p.x; this.y = p.y; this.angle = p.angle;
    }

    takeDamage(amount, trueDmg = false, fromFire = false, fromSplash = false) {
        if (!this.alive) return;
        let amt = amount;
        if (fromFire && this.type.fireVulnerable) {
            amt *= this.type.fireVulnerableMul != null ? this.type.fireVulnerableMul : 1.55;
        }
        const L = typeof Game !== "undefined" && Game.level ? Game.level : null;
        const fm = L && L.modifiers && L.modifiers.fireDamageMul;
        if (fromFire && fm) amt *= fm;
        if (fromSplash && this.type.splashVulnerableMul) {
            amt *= this.type.splashVulnerableMul;
        }
        const final = trueDmg ? amt : amt * (1 - this.armor);
        this.hp -= final;
        if (this.hp <= 0) {
            this.hp = 0;
            if (!this._deathNovaFired && this.type.deathNovaRadius) {
                this._deathNovaFired = true;
                const R = this.type.deathNovaRadius;
                const D = this.type.deathNovaDamage != null ? this.type.deathNovaDamage : 40;
                if (typeof Game !== "undefined" && Game.enemies) {
                    for (const o of Game.enemies) {
                        if (o === this || !o.alive) continue;
                        if (U.dist(o.x, o.y, this.x, this.y) <= R) o.takeDamage(D, true);
                    }
                }
                if (typeof Game !== "undefined" && Game.effects) {
                    Game.effects.push({ kind: "hit", x: this.x, y: this.y, elapsed: 0, duration: 0.4 });
                }
            }
            this.alive = false;
        }
    }

    applySlow(factor, dur) {
        const end = performance.now() + dur * 1000;
        if (end > this.slowUntil || factor > this.slowFactor) {
            this.slowUntil = end;
            this.slowFactor = factor;
        }
    }

    applyStun(dur) {
        this.stunUntil = Math.max(this.stunUntil, performance.now() + dur * 1000);
    }

    applyDot(dps, dur, opts) {
        let eff = dps;
        const fromFire = opts && opts.fire;
        if (fromFire && this.type.fireVulnerable) {
            eff *= this.type.fireVulnerableMul != null ? this.type.fireVulnerableMul : 1.55;
        }
        const L = typeof Game !== "undefined" && Game.level ? Game.level : null;
        const fm = L && L.modifiers && L.modifiers.fireDamageMul;
        if (fromFire && fm) eff *= fm;
        this.dotEnd = Math.max(this.dotEnd, performance.now() + dur * 1000);
        this.dotPerSec = Math.max(this.dotPerSec, eff);
    }
};
