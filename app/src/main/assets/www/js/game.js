window.Game = {
    canvas: null,
    ctx: null,
    level: null,
    state: "idle",   // idle | running | paused | victory | defeat
    speed: 1,
    gold: 0,
    life: 0,
    waveIdx: 0,
    waveTimer: 0,
    enemies: [],
    projectiles: [],
    towers: [],
    obstacles: [],
    effects: [],
    slots: [],       // {x,y,occupied:Tower}
    spawning: null,  // 当前波次 spawns 队列
    selectedTower: null,
    selectedSlot: null,
    placingGeneral: null,
    mouse: { x: -1, y: -1, hovering: null },
    view: { zoom: 1, panX: 0, panY: 0 },
    WORLD_W: 960,
    WORLD_H: 600,
    MIN_ZOOM: 1,
    MAX_ZOOM: 2.5,
    onUpdate: null,  // UI 同步回调
    onWaveChange: null,
    onBossSpawn: null,
    onResult: null,
    _enemyId: 0,
    _bossAlive: false,
    _bossKilled: false,
    defeatReason: null,
    _scheduledTicks: [],
    _ultFlash: null,
    mergePickSource: null,
    renderScale: 1,
    _cssDisplayW: 960,
    _cssDisplayH: 600,
    PIXEL_BUDGET_AUTO: 2500000,
    PIXEL_BUDGET_PERF: 2000000,
    PIXEL_BUDGET_QUALITY: 3000000,

    init(canvas) {
        this.canvas = canvas;
        this.ctx = canvas.getContext("2d");
        canvas.addEventListener("mousemove", this._onMove.bind(this));
        canvas.addEventListener("click", this._onClick.bind(this));
        canvas.addEventListener("contextmenu", e => { e.preventDefault(); this._cancelSelect(); });
        canvas.addEventListener("wheel", this._onWheel.bind(this), { passive: false });
        this.resetView();
        if (window.MobileBridge && MobileBridge.onViewportChanged) {
            MobileBridge.onViewportChanged();
        } else {
            this.syncCanvasResolution(960, 600);
        }
    },

    syncCanvasResolution(cssW, cssH) {
        const canvas = this.canvas;
        if (!canvas || cssW < 1 || cssH < 1) return;
        this._cssDisplayW = cssW;
        this._cssDisplayH = cssH;
        const q = window.Progress ? Progress.getRenderQuality() : "auto";
        let dprCap = Math.min(window.devicePixelRatio || 1, 3);
        let pixelBudget = this.PIXEL_BUDGET_AUTO;
        if (q === "performance") {
            dprCap = 2;
            pixelBudget = this.PIXEL_BUDGET_PERF;
        } else if (q === "quality") {
            dprCap = 3;
            pixelBudget = this.PIXEL_BUDGET_QUALITY;
        }
        let bw = Math.floor(cssW * dprCap);
        let bh = Math.floor(cssH * dprCap);
        const pixels = bw * bh;
        if (pixels > pixelBudget) {
            const s = Math.sqrt(pixelBudget / pixels);
            bw = Math.max(1, Math.floor(bw * s));
            bh = Math.max(1, Math.floor(bh * s));
        }
        canvas.width = bw;
        canvas.height = bh;
        canvas.classList.toggle("canvas-hidpi", (bw / cssW) >= 1.75);
        this.renderScale = bw / this.WORLD_W;
    },

    _visibleSize() {
        return window.Viewport
            ? Viewport.logicalVisibleSize(this.view.zoom)
            : { w: this.WORLD_W / this.view.zoom, h: this.WORLD_H / this.view.zoom };
    },

    _isFillPan() {
        return window.Viewport && Viewport.isFillPan();
    },

    _viewFrame() {
        return window.Viewport
            ? Viewport.viewFrame(this)
            : { vw: this.WORLD_W / this.view.zoom, vh: this.WORLD_H / this.view.zoom, scale: null };
    },

    _minZoom() {
        if (window.Viewport) {
            const gs = document.getElementById("game-screen");
            const aw = gs ? gs.clientWidth : (this._cssDisplayW || this.WORLD_W);
            const ah = gs ? gs.clientHeight : (this._cssDisplayH || this.WORLD_H);
            if (this.level && this.level.path) {
                return Viewport.getMinZoom({
                    availW: aw,
                    availH: ah,
                    path: this.level.path,
                    slots: this.level.slots
                });
            }
            return Viewport.getMinZoom({ availW: aw, availH: ah });
        }
        if (window.MobileBridge && MobileBridge.getMinZoom) return MobileBridge.getMinZoom();
        return this.MIN_ZOOM;
    },

    _clampPan() {
        if (window.Viewport) Viewport.clampPan(this);
        else {
            const v = this.view;
            const { vw, vh } = this._viewFrame();
            v.panX = U.clamp(v.panX, 0, Math.max(0, this.WORLD_W - vw));
            v.panY = U.clamp(v.panY, 0, Math.max(0, this.WORLD_H - vh));
        }
    },

    resetView() {
        this.view.zoom = 1;
        this.view.panX = 0;
        this.view.panY = 0;
        this._clampPan();
    },

    _setZoom(zoom, focalWorldX, focalWorldY) {
        const v = this.view;
        const oldZoom = v.zoom;
        const nz = U.clamp(zoom, this._minZoom(), this.MAX_ZOOM);
        if (Math.abs(nz - oldZoom) < 0.001) return;

        const oldF = this._viewFrame();
        const fx = focalWorldX != null ? focalWorldX : v.panX + oldF.vw * 0.5;
        const fy = focalWorldY != null ? focalWorldY : v.panY + oldF.vh * 0.5;

        v.zoom = nz;
        const newF = this._viewFrame();
        const ratioX = oldF.vw > 0 ? (fx - v.panX) / oldF.vw : 0.5;
        const ratioY = oldF.vh > 0 ? (fy - v.panY) / oldF.vh : 0.5;
        v.panX = fx - ratioX * newF.vw;
        v.panY = fy - ratioY * newF.vh;
        this._clampPan();
    },

    _applyPan(dWorldX, dWorldY) {
        if (window.Viewport && !Viewport.canPanAtZoom(this.view.zoom)) return;
        else if (!this._isFillPan() && this.view.zoom <= 1.001) return;
        this.view.panX += dWorldX;
        this.view.panY += dWorldY;
        this._clampPan();
    },

    _screenToWorld(clientX, clientY) {
        return window.Viewport
            ? Viewport.screenToWorld(this, clientX, clientY)
            : { x: 0, y: 0 };
    },

    _worldToScreen(worldX, worldY) {
        return window.Viewport
            ? Viewport.worldToScreen(this, worldX, worldY)
            : { x: 0, y: 0 };
    },

    _onWheel(ev) {
        if (!this.level) return;
        ev.preventDefault();
        const w = this._screenToWorld(ev.clientX, ev.clientY);
        const delta = ev.deltaY > 0 ? -0.12 : 0.12;
        this._setZoom(this.view.zoom * (1 + delta), w.x, w.y);
    },

    loadLevel(level) {
        this.level = level;
        this.state = "running";
        this.gold = level.startGold;
        this.life = level.life;
        this.waveIdx = 0;
        this.waveTimer = level.waves[0].delay;
        this.spawning = null;
        this.enemies.length = 0;
        this.projectiles.length = 0;
        this.towers.length = 0;
        this.effects.length = 0;
        this._scheduledTicks.length = 0;
        this._ultFlash = null;
        this._enemyId = 0;
        this.slots = level.slots.map(s => ({ x: s.x, y: s.y, occupied: null }));
        this.obstacles = level.obstacles.map(o => ({
            x: o.x, y: o.y,
            kind: o.kind,
            hp: o.hp, maxHp: o.hp,
            gold: o.gold,
            alive: true,
            _id: -1 - this.obstacles?.length
        }));
        // 把障碍物当作可被攻击的伪敌人（具有 .alive .hp .x .y 等接口）
        // 这里我们另写筛选逻辑：塔会优先攻击附近的敌人；若无敌人，可选攻击障碍
        this.selectedTower = null;
        this.selectedSlot = null;
        this.placingGeneral = null;
        this.mergePickSource = null;
        this.speed = 1;
        this.leaksThisLevel = 0;
        this._killsThisLevel = 0;
        this._killStreak = 0;
        this._bossAlive = false;
        this._bossKilled = false;
        this.defeatReason = null;
        if (window.MobileBridge && MobileBridge.applyLevelViewport) {
            MobileBridge.applyLevelViewport();
        } else {
            this.resetView();
        }
        if (this.onUpdate) this.onUpdate();
    },

    pause() { if (this.state === "running") this.state = "paused"; },
    resume() { if (this.state === "paused") this.state = "running"; },
    setSpeed(s) {
        this.speed = s;
        if (this.onUpdate) this.onUpdate();
    },

    scheduleTick(opts) {
        // duration, interval, onTick
        this._scheduledTicks.push(Object.assign({ elapsed: 0, accum: 0 }, opts));
    },

    /** 大招释放时把视口对准特效中心（缩放后也能看见） */
    focusViewForUlt(tower, ultType) {
        if (!this.level || !tower) return;
        const path = this.level.path;
        let cx = tower.x, cy = tower.y;
        if (path && path.length && (ultType === "flood" || ultType === "blaze")) {
            let sx = 0, sy = 0;
            for (const p of path) { sx += p.x; sy += p.y; }
            cx = sx / path.length;
            cy = sy / path.length;
        } else if (path && path.length && (ultType === "charge" || ultType === "rally")) {
            const pt = U.pointOnPath(path, U.pathLength(path) * 0.5);
            cx = pt.x;
            cy = pt.y;
        }
        const { w: vw, h: vh } = this._visibleSize();
        this.view.panX = U.clamp(cx - vw * 0.5, 0, Math.max(0, this.WORLD_W - vw));
        this.view.panY = U.clamp(cy - vh * 0.5, 0, Math.max(0, this.WORLD_H - vh));
        this._clampPan();
    },

    triggerUltFlash(ultType, x, y) {
        const dur = { flood: 1.6, blaze: 1.8, stun: 1.2, maze: 1.6, execute: 1.0, charge: 1.4, rally: 2.0 }[ultType] || 1.2;
        this._ultFlash = { type: ultType, x, y, elapsed: 0, duration: dur };
        const overlay = document.getElementById("ult-flash-overlay");
        if (!overlay) return;
        overlay.className = "ult-flash-overlay on " + (ultType || "flood");
        overlay.removeAttribute("aria-hidden");
        clearTimeout(this._ultDomTimer);
        this._ultDomTimer = setTimeout(() => {
            overlay.className = "ult-flash-overlay hidden";
            overlay.setAttribute("aria-hidden", "true");
        }, dur * 1000);
    },

    _tickVisuals(dt) {
        for (const ef of this.effects) {
            ef.elapsed += dt;
            if (ef.onUpdate) ef.onUpdate(dt);
        }
        this.effects = this.effects.filter(ef => ef.elapsed < ef.duration);
        if (this._ultFlash) {
            this._ultFlash.elapsed += dt;
            if (this._ultFlash.elapsed >= this._ultFlash.duration) this._ultFlash = null;
        }
    },

    _spawnEnemy(typeKey, opts) {
        const e = new Enemy(typeKey, this.level.path, opts);
        e._id = ++this._enemyId;
        this.enemies.push(e);
        const t = ENEMY_TYPES[typeKey];
        if (t && t.isBoss) {
            this._bossAlive = true;
            if (this.onBossSpawn) {
                this.onBossSpawn(e.type.name || this.level.bossName || "敌将");
            }
        }
    },

    update(dt) {
        if (this.state === "running" || this.state === "paused") {
            this._tickVisuals(dt * (this.state === "running" ? this.speed : 1));
        }
        if (this.state !== "running") return;
        dt *= this.speed;

        // 波次推进
        if (!this.spawning) {
            this.waveTimer -= dt;
            if (this.waveTimer <= 0 && this.waveIdx < this.level.waves.length) {
                const wave = this.level.waves[this.waveIdx];
                // 把这波 spawns 转化为带计时器的队列
                this.spawning = wave.spawns.map(s => ({
                    type: s.type,
                    remaining: s.count,
                    interval: s.interval,
                    timer: (s.after || 0),
                    name: s.name
                }));
                if (this.onWaveChange) this.onWaveChange(this.waveIdx + 1, this.level.waves.length);
            }
        } else {
            let allDone = true;
            for (const sp of this.spawning) {
                if (sp.remaining > 0) {
                    allDone = false;
                    sp.timer -= dt;
                    if (sp.timer <= 0) {
                        this._spawnEnemy(sp.type, sp.name ? { name: sp.name } : {});
                        sp.remaining -= 1;
                        sp.timer = sp.interval;
                    }
                }
            }
            if (allDone) {
                this.spawning = null;
                this.waveIdx += 1;
                if (this.enemies.length === 0 && window.Sfx) Sfx.play("wave");
                if (this.waveIdx < this.level.waves.length) {
                    this.waveTimer = this.level.waves[this.waveIdx].delay;
                }
            }
        }

        // 计时性大招效果
        for (const t of this._scheduledTicks) {
            t.elapsed += dt;
            t.accum += dt;
            while (t.accum >= t.interval && t.elapsed <= t.duration) {
                t.onTick();
                t.accum -= t.interval;
            }
        }
        this._scheduledTicks = this._scheduledTicks.filter(t => t.elapsed < t.duration);

        // 敌人
        for (const e of this.enemies) e.update(dt);
        // 处理走到终点 / 死亡
        for (const e of this.enemies) {
            if (!e.alive && e.reachedEnd) {
                if (!this._demoMode) {
                    if (e.type.isBoss) {
                        this.defeatReason = "boss_escape";
                        this._end("defeat");
                        return;
                    }
                    this.life -= 1;
                    this.leaksThisLevel += 1;
                    if (window.Haptics) Haptics.leak();
                }
                if (this.onUpdate) this.onUpdate();
            } else if (!e.alive && !e._rewarded) {
                e._rewarded = true;
                this._killsThisLevel = (this._killsThisLevel || 0) + 1;
                if (window.Sfx) {
                    Sfx.play("kill");
                    const streak = this._killStreak || 0;
                    this._killStreak = streak + 1;
                    if (this._killStreak >= 5 && this._killStreak % 5 === 0) Sfx.play("wave");
                }
                if (e.type.isBoss) this._bossKilled = true;
                this.gold += e.type.gold;
                this.effects.push({ kind: "coin", x: e.x, y: e.y, value: e.type.gold, elapsed: 0, duration: 0.8 });
                // 击杀奖励怒气：所有附近的塔
                for (const tw of this.towers) {
                    if (U.dist(tw.x, tw.y, e.x, e.y) < tw.range + 60) {
                        tw.rage = Math.min(tw.maxRage, tw.rage + (e.type.isBoss ? 50 : 10));
                    }
                }
                if (this.onUpdate) this.onUpdate();
            }
        }
        this.enemies = this.enemies.filter(e => e.alive);

        // 投射物
        for (const p of this.projectiles) p.update(dt, this.enemies, this.effects);
        this.projectiles = this.projectiles.filter(p => p.alive);

        // 塔
        for (const t of this.towers) t.update(dt, this.enemies, this.projectiles, this.effects);

        if (window.Progress && Progress.getUltAuto()) {
            for (const t of this.towers) {
                if (t.canUlt()) this.castUlt(t, { auto: true });
            }
        }

        // 障碍物：被附近塔的投射物意外击中？为了简化，让塔在没有敌人时主动打障碍
        for (const t of this.towers) {
            const nearEnemy = this.enemies.some(e => e.alive && U.dist(t.x, t.y, e.x, e.y) <= t.range);
            if (!nearEnemy && t.cooldown <= 0) {
                // 找最近障碍
                let ob = null, bestD = t.range;
                for (const o of this.obstacles) {
                    if (!o.alive) continue;
                    const d = U.dist(t.x, t.y, o.x, o.y);
                    if (d <= bestD) { bestD = d; ob = o; }
                }
                if (ob) {
                    ob.hp -= t.damage;
                    this.effects.push({ kind: "hit", x: ob.x, y: ob.y, elapsed: 0, duration: 0.3 });
                    this.effects.push({ kind: "damage", x: ob.x, y: ob.y - 10, value: Math.round(t.damage), elapsed: 0, duration: 0.6 });
                    t.cooldown = t.rate;
                    t.aim = U.angleTo(t.x, t.y, ob.x, ob.y);
                    if (ob.hp <= 0) {
                        ob.alive = false;
                        this.gold += ob.gold;
                        this.effects.push({ kind: "coin", x: ob.x, y: ob.y, value: ob.gold, elapsed: 0, duration: 1.0 });
                        if (this.onUpdate) this.onUpdate();
                    }
                }
            }
        }

        // 胜负判定（demo 模式不触发结算）
        if (!this._demoMode) {
            if (this.life <= 0) {
                if (!this.defeatReason) this.defeatReason = "life";
                this._end("defeat");
            } else {
                const wavesDone = this.waveIdx >= this.level.waves.length && !this.spawning;
                const fieldClear = this.enemies.length === 0;
                const bossOk = !this._bossAlive || this._bossKilled;
                if (wavesDone && fieldClear && bossOk) this._end("victory");
            }
        }
    },

    _end(state) {
        if (this.state === "victory" || this.state === "defeat") return;
        this.state = state;
        const startLife = this.level ? this.level.life : 10;
        this._lastResultStats = {
            victory: state === "victory",
            leaks: this.leaksThisLevel || 0,
            kills: this._killsThisLevel || 0,
            goldLeft: this.gold,
            life: this.life,
            startLife
        };
        if (window.Sfx) Sfx.play(state === "victory" ? "victory" : "defeat");
        if (this.onResult) this.onResult(state);
    },

    findNearestEnemy(x, y, maxD, exclude) {
        let best = null, bd = maxD;
        for (const e of this.enemies) {
            if (!e.alive) continue;
            if (exclude && exclude.has(e._id)) continue;
            const d = U.dist(x, y, e.x, e.y);
            if (d < bd) { bd = d; best = e; }
        }
        return best;
    },

    // === 鼠标 / 触摸指针 ===
    _onMove(ev) {
        const w = this._screenToWorld(ev.clientX, ev.clientY);
        this.mouse.x = w.x;
        this.mouse.y = w.y;
        // 计算 hover 的格子或塔
        this.mouse.hovering = null;
        // 已占用的槽位与塔同坐标；若先判槽位会永远命中 slot，导致无法再点开塔（升级/大招面板）
        for (const t of this.towers) {
            if (U.dist(t.x, t.y, this.mouse.x, this.mouse.y) < 30) {
                this.mouse.hovering = { type: "tower", tower: t };
                return;
            }
        }
        for (const s of this.slots) {
            if (s.occupied) continue;
            if (U.dist(s.x, s.y, this.mouse.x, this.mouse.y) < 26) {
                this.mouse.hovering = { type: "slot", slot: s };
                return;
            }
        }
    },

    _onClick(ev) {
        if (this.state !== "running" && this.state !== "paused") return;
        const h = this.mouse.hovering;
        if (this.mergePickSource) {
            const src = this.mergePickSource;
            if (h && h.type === "tower" && h.tower !== src && src.canMergeWith(h.tower)) {
                this.mergeTowers(src, h.tower);
            } else if (h && h.type === "tower" && h.tower === src) {
                this.mergePickSource = null;
            } else {
                this.mergePickSource = null;
            }
            if (this.onUpdate) this.onUpdate();
            return;
        }
        if (this.placingGeneral) {
            if (h && h.type === "slot" && !h.slot.occupied) {
                const g = this.placingGeneral;
                if (this.gold >= g.cost) {
                    const tower = new Tower(g, h.slot.x, h.slot.y, this.slots.indexOf(h.slot));
                    h.slot.occupied = tower;
                    this.towers.push(tower);
                    this.gold -= g.cost;
                    this.placingGeneral = null;
                    this.selectedTower = tower;
                    this.selectedSlot = null;
                    if (window.Sfx) Sfx.play("place");
                    if (this.onUpdate) this.onUpdate();
                }
            } else if (h && h.type === "tower") {
                this.placingGeneral = null;
                this.selectedTower = h.tower;
                if (this.onUpdate) this.onUpdate();
            } else {
                this.placingGeneral = null;
                if (this.onUpdate) this.onUpdate();
            }
            return;
        }
        if (h && h.type === "tower") {
            this.selectedTower = h.tower;
            this.selectedSlot = null;
            if (this.onUpdate) this.onUpdate();
        } else if (h && h.type === "slot") {
            this.selectedSlot = h.slot;
            this.selectedTower = null;
            if (this.onUpdate) this.onUpdate();
        } else {
            this._cancelSelect();
        }
    },

    _cancelSelect() {
        this.selectedTower = null;
        this.selectedSlot = null;
        this.placingGeneral = null;
        this.mergePickSource = null;
        if (this.onUpdate) this.onUpdate();
    },

    findMergePartners(tower) {
        if (!tower) return [];
        return this.towers.filter(t => t !== tower && tower.canMergeWith(t));
    },

    beginMergePick(source) {
        const partners = this.findMergePartners(source);
        if (!partners.length) return false;
        this.mergePickSource = source;
        this.selectedTower = source;
        this.placingGeneral = null;
        if (this.onUpdate) this.onUpdate();
        return true;
    },

    mergeTowers(keeper, consumed) {
        if (!keeper || !consumed || !keeper.canMergeWith(consumed)) return false;
        const refund = Math.floor(consumed.totalInvestedGold() * 0.3);
        this.gold += refund;
        keeper.mergeTier += 1;
        keeper.rage = Math.min(keeper.maxRage, keeper.rage + Math.floor(consumed.rage * 0.5));
        const slot = this.slots[consumed.slotIndex];
        if (slot) slot.occupied = null;
        const idx = this.towers.indexOf(consumed);
        if (idx >= 0) this.towers.splice(idx, 1);
        this.mergePickSource = null;
        this.selectedTower = keeper;
        this.effects.push({
            kind: "mergeBurst",
            x: keeper.x,
            y: keeper.y,
            mergeTier: keeper.mergeTier,
            elapsed: 0,
            duration: 0.9
        });
        if (window.UI && UI.showBanner) {
            UI.showBanner(`合成成功 · ${keeper.general.name} ${keeper.mergeLabel()}`, 2000);
        }
        if (window.Sfx) Sfx.play("upgrade");
        if (this.onUpdate) this.onUpdate();
        this.render();
        return true;
    },

    upgradeSelected() {
        const t = this.selectedTower;
        if (!t || !t.canUpgrade()) return;
        const cost = t.upgradeCost();
        if (this.gold < cost) return;
        this.gold -= cost;
        t.upgrade();
        if (window.Sfx) Sfx.play("upgrade");
        if (this.onUpdate) this.onUpdate();
    },

    castUlt(tower, opts) {
        if (!tower || !tower.canUlt()) return false;
        const auto = opts && opts.auto;
        const ultType = tower.general.ultimate.type;
        if (window.Sfx && Sfx.playUlt) Sfx.playUlt(ultType);
        else if (window.Sfx) Sfx.play("ult");
        if (window.Haptics) Haptics.ult();
        Ult.cast(tower, this);
        if (!auto) this.focusViewForUlt(tower, ultType);
        this.triggerUltFlash(ultType, tower.x, tower.y);
        tower.consumeRage();
        if (this.onUpdate) this.onUpdate();
        this.render();
        return true;
    },

    castUltSelected() {
        const t = this.selectedTower;
        if (!t || !t.canUlt()) return;
        this.castUlt(t, { auto: false });
    },

    sellSelected() {
        const t = this.selectedTower;
        if (!t) return;
        this.gold += t.sellValue();
        const slot = this.slots[t.slotIndex];
        if (slot) slot.occupied = null;
        const idx = this.towers.indexOf(t);
        if (idx >= 0) this.towers.splice(idx, 1);
        this.selectedTower = null;
        if (this.onUpdate) this.onUpdate();
    },

    pickGeneralToPlace(general) {
        if (this.gold < general.cost) return;
        this.placingGeneral = general;
        this.selectedTower = null;
        this.selectedSlot = null;
        if (this.onUpdate) this.onUpdate();
    },

    // === 渲染 ===
    render() {
        const ctx = this.ctx;
        const cw = this.canvas.width;
        const ch = this.canvas.height;
        if (!this.level) {
            ctx.fillStyle = "#000"; ctx.fillRect(0, 0, cw, ch);
            return;
        }

        ctx.setTransform(1, 0, 0, 1, 0, 0);
        ctx.fillStyle = "#0a0604";
        ctx.fillRect(0, 0, cw, ch);
        if (window.Viewport) {
            const frame = Viewport.applyRenderTransform(ctx, this, cw, ch);
            this.renderScale = cw / frame.vw;
        } else if (this._isFillPan()) {
            const v = this.view;
            const s = Math.max(cw / (this.WORLD_W / v.zoom), ch / (this.WORLD_H / v.zoom));
            ctx.setTransform(s, 0, 0, s, -v.panX * s, -v.panY * s);
            this.renderScale = cw / this._viewFrame().vw;
        } else {
            const v = this.view;
            const { w: vw, h: vh } = this._visibleSize();
            ctx.setTransform(cw / vw, 0, 0, ch / vh, -v.panX * (cw / vw), -v.panY * (ch / vh));
            this.renderScale = cw / vw;
        }

        Art.drawMap(ctx, this.level);

        for (const o of this.obstacles) if (o.alive) Art.drawObstacle(ctx, o);

        for (const s of this.slots) {
            if (s.occupied) continue;
            const hov = this.mouse.hovering && this.mouse.hovering.type === "slot" && this.mouse.hovering.slot === s;
            Art.drawSlot(ctx, s, hov);
        }

        for (const t of this.towers) Art.drawTower(ctx, t);

        if (this.mergePickSource) {
            const partners = this.findMergePartners(this.mergePickSource);
            for (const p of partners) Art.drawMergeHighlight(ctx, p.x, p.y);
        }

        for (const e of this.enemies) Art.drawEnemy(ctx, e);

        for (const p of this.projectiles) Art.drawProjectile(ctx, p);

        for (const ef of this.effects) Art.drawEffect(ctx, ef);

        if (this.selectedTower && !this.mergePickSource) {
            Art.drawRange(ctx, this.selectedTower.x, this.selectedTower.y, this.selectedTower.range);
        }

        if (this.placingGeneral && this.mouse.hovering && this.mouse.hovering.type === "slot") {
            const s = this.mouse.hovering.slot;
            const valid = !s.occupied && this.gold >= this.placingGeneral.cost;
            Art.drawPlacePreview(ctx, s.x, s.y, this.placingGeneral.range, valid, this.placingGeneral);
        }

        ctx.setTransform(1, 0, 0, 1, 0, 0);
        if (this._ultFlash) Art.drawUltOverlay(ctx, this._ultFlash, cw, ch, this);
    }
};
