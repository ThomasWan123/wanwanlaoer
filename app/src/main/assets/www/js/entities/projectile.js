window.Projectile = class Projectile {
    constructor(opts) {
        Object.assign(this, opts);
        // x, y, target, kind, color, damage, speed, splash, slow, slowDur, pierce, dotPerSec, dotDur
        this.alive = true;
        this.angle = 0;
        this._hitIds = new Set();
    }

    update(dt, enemies, effects) {
        if (!this.alive) return;
        if (!this.target || !this.target.alive) {
            // 找到最近敌人作为新目标，否则消失
            this.target = Game.findNearestEnemy(this.x, this.y, 200, this._hitIds);
            if (!this.target) { this.alive = false; return; }
        }
        const dx = this.target.x - this.x;
        const dy = this.target.y - this.y;
        const d = Math.hypot(dx, dy);
        this.angle = Math.atan2(dy, dx);
        const step = this.speed * dt;
        if (d <= step + this.target.type.size * 0.5) {
            this.hit(this.target, enemies, effects);
        } else {
            this.x += dx / d * step;
            this.y += dy / d * step;
        }
        if (!this._trail) this._trail = [];
        this._trail.push({ x: this.x, y: this.y });
        while (this._trail.length > 16) this._trail.shift();
    }

    hit(target, enemies, effects) {
        if (this._hitIds.has(target._id)) return;
        this._hitIds.add(target._id);
        target.takeDamage(this.damage, false, !!this.hitIsFire, false);
        effects.push({ kind: "impact", x: target.x, y: target.y, projKind: this.kind, fire: !!this.hitIsFire, elapsed: 0, duration: 0.24 });
        effects.push({ kind: "hit", x: target.x, y: target.y, elapsed: 0, duration: 0.32 });
        effects.push({ kind: "damage", x: target.x, y: target.y - 10, value: Math.round(this.damage * (1 - target.armor)), elapsed: 0, duration: 0.7 });
        if (this.slow) target.applySlow(this.slow, this.slowDur || 1.5);
        if (this.dotPerSec) target.applyDot(this.dotPerSec, this.dotDur || 2, { fire: !!this.dotIsFire });

        // 范围伤害
        if (this.splash) {
            const splashFire = !!this.hitIsFire;
            for (const e of enemies) {
                if (e === target || !e.alive) continue;
                const d = U.dist(e.x, e.y, target.x, target.y);
                if (d <= this.splash) {
                    e.takeDamage(this.damage * (1 - d / this.splash) * 0.7, false, splashFire, true);
                    effects.push({ kind: "impact", x: e.x, y: e.y, projKind: this.kind, fire: splashFire, small: true, elapsed: 0, duration: 0.16 });
                    effects.push({ kind: "hit", x: e.x, y: e.y, elapsed: 0, duration: 0.28 });
                }
            }
        }

        // 穿透（方天戟近卫等：pierceExtraCost 额外消耗穿透层数）
        if (this.pierce && this.pierce > 1) {
            const cost = 1 + (target.type.pierceExtraCost | 0);
            this.pierce -= cost;
            if (this.pierce < 1) {
                this.alive = false;
            } else {
                effects.push({ kind: "pierceFlash", x: target.x, y: target.y, elapsed: 0, duration: 0.14 });
                this.target = Game.findNearestEnemy(this.x, this.y, 220, this._hitIds);
                if (!this.target) this.alive = false;
            }
        } else {
            this.alive = false;
        }
    }
};
