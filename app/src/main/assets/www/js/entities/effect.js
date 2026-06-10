// 大招 / 视觉特效 控制器
// 创建带逻辑的特效（持续作用）

window.Ult = {
    _addBurst(game, tower, ultType) {
        const g = tower.general;
        game.effects.push({
            kind: "ultBurst",
            x: tower.x,
            y: tower.y,
            ultType: ultType,
            color: g.accent || g.color || "#f7d774",
            elapsed: 0,
            duration: 1.5
        });
    },

    cast(tower, game) {
        const ult = tower.general.ultimate;
        const path = game.level.path;
        const enemies = game.enemies;
        const type = ult.type;

        switch (type) {
            case "flood": {
                // 水淹七军：全路径减速 + 真实伤害
                game.effects.push({ kind: "flood", path, elapsed: 0, duration: 4, _ticks: 0 });
                game.scheduleTick({
                    duration: 4, interval: 0.5,
                    onTick: () => {
                        for (const e of enemies) {
                            if (!e.alive) continue;
                            // 仅作用于在路径上的敌人
                            e.takeDamage(U.rand(15, 30), true);
                            e.applySlow(0.6, 1.0);
                        }
                    }
                });
                break;
            }
            case "blaze": {
                // 火烧赤壁：全路径每秒造成范围伤害 5 秒
                game.effects.push({ kind: "blaze", path, elapsed: 0, duration: 5 });
                game.scheduleTick({
                    duration: 5, interval: 0.5,
                    onTick: () => {
                        for (const e of enemies) {
                            if (!e.alive) continue;
                            e.takeDamage(30, false, true);
                            e.applyDot(20, 1.5, { fire: true });
                        }
                    }
                });
                break;
            }
            case "stun": {
                // 当阳桥怒喝：眩晕全场 3 秒，造成 50 伤害
                game.effects.push({ kind: "stun", x: tower.x, y: tower.y, elapsed: 0, duration: 2.2 });
                for (const e of enemies) {
                    if (!e.alive) continue;
                    e.takeDamage(50);
                    e.applyStun(3);
                }
                break;
            }
            case "maze": {
                // 八阵图：在最强敌人附近形成阵图，5 秒每秒 25 伤害 + 减速
                let target = null, bestHp = -1;
                for (const e of enemies) {
                    if (e.alive && e.hp > bestHp) { bestHp = e.hp; target = e; }
                }
                if (!target) { target = { x: tower.x, y: tower.y }; }
                const cx = target.x, cy = target.y;
                game.effects.push({ kind: "maze", x: cx, y: cy, elapsed: 0, duration: 5 });
                game.scheduleTick({
                    duration: 5, interval: 0.5,
                    onTick: () => {
                        for (const e of enemies) {
                            if (!e.alive) continue;
                            const d = U.dist(e.x, e.y, cx, cy);
                            if (d <= 80) {
                                e.takeDamage(12.5);
                                e.applySlow(0.7, 1.0);
                            }
                        }
                    }
                });
                break;
            }
            case "execute": {
                // 方天破：处决场上 HP 最高的敌人 350 真实伤害
                let target = null, bestHp = -1;
                for (const e of enemies) {
                    if (e.alive && e.hp > bestHp) { bestHp = e.hp; target = e; }
                }
                if (target) {
                    game.effects.push({ kind: "execute", x: target.x, y: target.y, elapsed: 0, duration: 0.6 });
                    target.takeDamage(350, true);
                }
                break;
            }
            case "charge": {
                // 七进七出：沿路径冲杀两个来回
                const ef = {
                    kind: "charge",
                    elapsed: 0, duration: 4,
                    x: path[0].x, y: path[0].y,
                    trail: [],
                    _phase: 0,
                    _hits: new Set(),
                    onUpdate(dt) {
                        const totalLen = U.pathLength(path);
                        // 4 秒内做 4 次单程：起→终→起→终
                        const segDur = 1.0;
                        const phase = Math.floor(this.elapsed / segDur);
                        const tt = (this.elapsed % segDur) / segDur;
                        const forward = phase % 2 === 0;
                        const prog = forward ? tt * totalLen : (1 - tt) * totalLen;
                        const pt = U.pointOnPath(path, prog);
                        this.x = pt.x; this.y = pt.y;
                        this.trail.push({ x: pt.x, y: pt.y });
                        if (this.trail.length > 14) this.trail.shift();
                        // 撞击范围内敌人，每次单程造成 60 伤害
                        for (const e of enemies) {
                            if (!e.alive) continue;
                            if (U.dist(e.x, e.y, pt.x, pt.y) < 40) {
                                const key = e._id + "_" + phase;
                                if (!this._hits.has(key)) {
                                    this._hits.add(key);
                                    e.takeDamage(60);
                                }
                            }
                        }
                    }
                };
                game.effects.push(ef);
                break;
            }
            case "hex": {
                game.effects.push({ kind: "hex", path, elapsed: 0, duration: 5 });
                game.scheduleTick({
                    duration: 5, interval: 0.5,
                    onTick: () => {
                        for (const e of enemies) {
                            if (!e.alive) continue;
                            e.takeDamage(18);
                            e.applySlow(0.72, 1.1);
                        }
                    }
                });
                break;
            }
            case "tide": {
                game.effects.push({ kind: "tide", path, elapsed: 0, duration: 4.5 });
                game.scheduleTick({
                    duration: 4, interval: 0.5,
                    onTick: () => {
                        for (const e of enemies) {
                            if (!e.alive) continue;
                            e.takeDamage(U.rand(12, 22), true);
                            e.applySlow(0.45, 0.9);
                        }
                    }
                });
                game.gold += 45;
                for (const tw of game.towers) {
                    tw.rage = Math.min(tw.maxRage, tw.rage + 25);
                }
                if (game.onUpdate) game.onUpdate();
                break;
            }
            case "rally": {
                const bursts = game.towers.map(t => ({ x: t.x, y: t.y }));
                game.effects.push({
                    kind: "rally",
                    path,
                    bursts,
                    elapsed: 0,
                    duration: 2.8
                });
                for (const tw of game.towers) {
                    tw.rage = tw.maxRage;
                }
                game.scheduleTick({
                    duration: 3.5,
                    interval: 0.5,
                    onTick: () => {
                        for (const e of enemies) {
                            if (!e.alive) continue;
                            e.applySlow(0.35, 1.0);
                        }
                    }
                });
                if (game.life < game.level.life) {
                    game.life = Math.min(game.level.life, game.life + 1);
                }
                if (game.onUpdate) game.onUpdate();
                break;
            }
            default:
                break;
        }
        this._addBurst(game, tower, type);
    }
};
