window.UI = {
    _tpSelRef: null,
    _tpPopT: null,
    _ultToastT: null,

    roleLabel(g) {
        const arch = window.archetypeForGeneral ? archetypeForGeneral(g) : (g.archetype || g.attackType);
        return (window.ARCHETYPE_ROLE_LABEL && ARCHETYPE_ROLE_LABEL[arch]) || "武将";
    },

    ultimateTypeLabel(type) {
        return (window.ULTIMATE_TYPE_LABEL && ULTIMATE_TYPE_LABEL[type]) || "战法";
    },

    generalTagHtml(g) {
        const fc = (window.FACTION_COLORS && FACTION_COLORS[g.faction]) || {};
        const accent = fc.accent || "#f7d774";
        return `<span class="gen-tag gen-tag-faction" style="border-color:${accent};color:${accent}">[${g.faction}]</span>`
            + `<span class="gen-tag gen-tag-role">${this.roleLabel(g)}</span>`;
    },

    attackTypeLabel(at) {
        const m = {
            melee: "近战重斩",
            splash: "范围溅射",
            rapid: "高频突刺",
            magic: "谋略法攻",
            pierce: "穿透连击"
        };
        return m[at] || "远程";
    },

    projectileLabel(pt) {
        const m = {
            slash: "刀光",
            shock: "震波",
            spear: "枪芒",
            fan: "羽扇",
            halberd: "画戟",
            fire: "火球（火焰伤害）"
        };
        return m[pt] || "弹道";
    },

    generalCombatTags(g) {
        const tags = [];
        const s0 = g.levels && g.levels[0];
        tags.push(this.attackTypeLabel(g.attackType));
        if (s0) {
            if (s0.splash && g.attackType !== "splash") tags.push("溅射");
            if (s0.pierce && g.attackType !== "pierce") tags.push("穿透");
            if (s0.slow) tags.push("减速");
        }
        if (g.projectileType === "fire") tags.push("火焰");
        return [...new Set(tags)].slice(0, 3);
    },

    ultimateShortText(g) {
        const u = g.ultimate;
        if (u.short) return u.short;
        if (!u.desc) return "";
        return u.desc.length > 40 ? u.desc.slice(0, 38) + "…" : u.desc;
    },

    upgradeNewHints(stats, next, g) {
        if (!next) return "";
        const lines = [];
        const cur = stats;
        if (!cur.splash && next.splash) lines.push("<span class=\"tp-new\">升级解锁：溅射范围</span>");
        if (!cur.pierce && next.pierce) lines.push("<span class=\"tp-new\">升级解锁：穿透</span>");
        if (!cur.slow && next.slow) lines.push("<span class=\"tp-new\">升级解锁：减速</span>");
        if (cur.dmg && next.dmg && next.dmg >= cur.dmg * 1.35) {
            lines.push("<span class=\"tp-new\">下阶伤害显著提升</span>");
        }
        return lines.length ? "<div class=\"tp-hints\">" + lines.join("") + "</div>" : "";
    },

    showUltToast(text) {
        const el = document.getElementById("ult-toast");
        if (!el) return;
        el.textContent = text;
        el.classList.remove("hidden");
        el.classList.remove("ult-toast-show");
        void el.offsetWidth;
        el.classList.add("ult-toast-show");
        clearTimeout(this._ultToastT);
        this._ultToastT = setTimeout(() => {
            el.classList.remove("ult-toast-show");
            setTimeout(() => el.classList.add("hidden"), 400);
        }, 1600);
    },

    show(name) {
        document.querySelectorAll(".screen").forEach(s => s.classList.remove("active"));
        const el = document.getElementById(name);
        if (el) el.classList.add("active");
        if (name === "menu" && this._settingsReturnPause) {
            this._settingsReturnPause = false;
            const pause = document.getElementById("pause-mask");
            if (pause) pause.classList.add("hidden");
        }
        if (name !== "game-screen") {
            const hint = document.getElementById("first-battle-hint");
            if (hint) hint.classList.add("hidden");
        }
    },

    _portraitBacking(displayPx) {
        const dpr = Math.min(window.devicePixelRatio || 1, 2);
        return Math.max(32, Math.floor(displayPx * dpr));
    },

    _drawPortraitToCanvas(canvas, g, displayPx) {
        const px = this._portraitBacking(displayPx);
        canvas.width = px;
        canvas.height = px;
        canvas.style.width = displayPx + "px";
        canvas.style.height = displayPx + "px";
        const ctx = canvas.getContext("2d");
        Art.drawPortrait(ctx, px / 2, px / 2, px / 2 - 2, g);
    },

    syncSettingsUI() {
        const q = window.Progress ? Progress.getRenderQuality() : "auto";
        document.querySelectorAll('input[name="render-quality"]').forEach(el => {
            el.checked = el.value === q;
        });
        const ultEl = document.getElementById("settings-ult-auto");
        const sfxEl = document.getElementById("settings-sfx");
        if (ultEl && window.Progress) ultEl.checked = Progress.getUltAuto();
        if (sfxEl && window.Progress) sfxEl.checked = Progress.getSfxEnabled();
        const tpUlt = document.getElementById("tp-ult-auto");
        if (tpUlt && window.Progress) tpUlt.checked = Progress.getUltAuto();
    },

    syncCreditsQualityUI() {
        this.syncSettingsUI();
    },

    initSettings() {
        document.querySelectorAll('input[name="render-quality"]').forEach(el => {
            el.onchange = () => {
                if (el.checked && window.Progress) Progress.setRenderQuality(el.value);
            };
        });
        const ultEl = document.getElementById("settings-ult-auto");
        if (ultEl) {
            ultEl.onchange = () => {
                if (window.Progress) Progress.setUltAuto(ultEl.checked);
                const tpUlt = document.getElementById("tp-ult-auto");
                if (tpUlt) tpUlt.checked = ultEl.checked;
            };
        }
        const sfxEl = document.getElementById("settings-sfx");
        if (sfxEl) {
            sfxEl.onchange = () => {
                if (window.Progress) Progress.setSfxEnabled(sfxEl.checked);
            };
        }
        this.syncSettingsUI();
    },

    openSettings(fromPause) {
        this._settingsReturnPause = !!fromPause;
        this.syncSettingsUI();
        this.show("settings");
    },

    initCreditsQuality() {},

    TUTORIAL_STEPS: [
        "点击下方武将头像，选择要部署的将领。",
        "再点击地图上的空位，消耗金币完成布阵。",
        "点击已部署的武将，可升级、释放大招或撤退换阵。",
        "两尊同名且均为 Lv.3 的武将可合成红将，战力倍增。",
        "阻止敌军抵达皇城；守满全部波次并清场即可获胜。"
    ],

    _tutorialIndex: 0,

    initTutorial() {
        const overlay = document.getElementById("tutorial-overlay");
        const nextBtn = document.getElementById("btn-tutorial-next");
        const skipBtn = document.getElementById("btn-tutorial-skip");
        if (!overlay || !nextBtn || nextBtn.dataset.bound) return;
        nextBtn.dataset.bound = "1";
        const self = this;
        const finish = () => {
            overlay.classList.add("hidden");
            if (window.Progress) Progress.setTutorialDone(true);
            self._showFirstBattleHintIfNeeded(0);
        };
        nextBtn.onclick = () => {
            self._tutorialIndex += 1;
            if (self._tutorialIndex >= self.TUTORIAL_STEPS.length) {
                finish();
                return;
            }
            self._renderTutorialStep();
        };
        if (skipBtn && !skipBtn.dataset.bound) {
            skipBtn.dataset.bound = "1";
            skipBtn.onclick = finish;
        }
    },

    _renderTutorialStep() {
        const label = document.getElementById("tutorial-step-label");
        const text = document.getElementById("tutorial-step-text");
        const nextBtn = document.getElementById("btn-tutorial-next");
        const i = this._tutorialIndex;
        const total = this.TUTORIAL_STEPS.length;
        if (label) label.textContent = `新手引导 ${i + 1} / ${total}`;
        if (text) text.textContent = this.TUTORIAL_STEPS[i];
        if (nextBtn) nextBtn.textContent = i >= total - 1 ? "开始战斗" : "下一步";
    },

    maybeShowTutorial(levelIndex) {
        if (!window.Progress || Progress.isTutorialDone() || levelIndex !== 0) {
            this._showFirstBattleHintIfNeeded(levelIndex);
            return;
        }
        const overlay = document.getElementById("tutorial-overlay");
        if (!overlay) return;
        this._tutorialIndex = 0;
        this._renderTutorialStep();
        overlay.classList.remove("hidden");
    },

    _showFirstBattleHintIfNeeded(levelIndex) {
        const idx = levelIndex != null ? levelIndex : (window.Main ? Main.currentLevel : 0);
        const el = document.getElementById("first-battle-hint");
        if (!el || idx !== 0 || !window.Progress) return;
        if (Progress.data.firstBattleHintShown) return;
        const mapHint = document.getElementById("map-view-hint");
        if (mapHint && !mapHint.classList.contains("hidden")) return;
        Progress.data.firstBattleHintShown = true;
        Progress.save();
        el.classList.remove("hidden");
        clearTimeout(this._firstHintT);
        this._firstHintT = setTimeout(() => el.classList.add("hidden"), 12000);
    },

    initMenu() {
        document.getElementById("btn-start").onclick = () => this.openLevelSelect();
        document.getElementById("btn-help").onclick = () => { this.buildCodex(); this.show("codex"); };
        const btnSettings = document.getElementById("btn-settings");
        if (btnSettings) btnSettings.onclick = () => this.openSettings(false);
        document.getElementById("btn-credits").onclick = () => this.show("credits");
        this.initSettings();
        this.initTutorial();
        const settingsBack = document.querySelector("#settings .back-btn");
        if (settingsBack && !settingsBack.dataset.settingsBound) {
            settingsBack.dataset.settingsBound = "1";
            settingsBack.onclick = (e) => {
                e.preventDefault();
                if (this._settingsReturnPause) {
                    this._settingsReturnPause = false;
                    this.show("game-screen");
                    const pause = document.getElementById("pause-mask");
                    if (pause) pause.classList.remove("hidden");
                } else {
                    this.show("menu");
                }
            };
        }
        const app = document.getElementById("app");
        if (app && !app.dataset.backBound) {
            app.dataset.backBound = "1";
            app.addEventListener("click", (e) => {
                const btn = e.target.closest(".back-btn");
                if (!btn || !btn.dataset.back) return;
                e.preventDefault();
                this.show(btn.dataset.back);
            });
        }
        this.initCampaignReset();
    },

    initCampaignReset() {
        const btn = document.getElementById("btn-reset-campaign");
        if (!btn || btn.dataset.bound) return;
        btn.dataset.bound = "1";
        btn.onclick = () => {
            const ok = confirm(
                "确定重置战役进度？\n\n将清空已通关记录与已解锁武将，仅保留起始武将关羽。"
            );
            if (!ok) return;
            Progress.reset();
            this.buildLevelList();
            this.showBanner("战役进度已重置", 2000);
            requestAnimationFrame(() => this._scrollLevelSelectToPlayable());
        };
    },

    openLevelSelect() {
        this.buildLevelList();
        this.show("level-select");
        requestAnimationFrame(() => this._scrollLevelSelectToPlayable());
    },

    _scrollLevelSelectToPlayable() {
        const screen = document.getElementById("level-select");
        if (!screen) return;
        const target = document.querySelector("#level-list .level-card.current-target")
            || document.querySelector("#level-list .level-card:not(.locked)");
        if (target) {
            const y = target.offsetTop - screen.offsetTop - 72;
            screen.scrollTop = Math.max(0, y);
        } else {
            screen.scrollTop = 0;
        }
    },

    buildLevelList() {
        if (window.Progress) Progress.load();
        const list = document.getElementById("level-list");
        const progEl = document.getElementById("campaign-progress-text");
        const targetIdx = window.Progress ? Progress.getCurrentTargetIndex() : 0;
        if (progEl && window.Progress) {
            const cleared = Progress.maxCleared + 1;
            const progressLine = `战役进度：${Math.min(cleared, LEVELS.length)} / ${LEVELS.length}`;
            if (targetIdx >= 0 && LEVELS[targetIdx]) {
                const t = LEVELS[targetIdx];
                progEl.textContent = `${progressLine}\n当前要通：第 ${targetIdx + 1} 战 · ${t.name}`;
            } else {
                progEl.textContent = `${progressLine}\n当前要通：已全部通关`;
            }
        }
        list.innerHTML = "";
        LEVELS.forEach((lv, i) => {
            const card = document.createElement("div");
            const isTarget = i === targetIdx;
            card.className = "level-card " + (lv.uiTheme || "") + (isTarget ? " current-target" : "");
            if (window.ArtAssets && ArtAssets.hasMapArt(lv.mapTheme)) {
                card.classList.add("has-map-art");
                card.style.backgroundImage = "url(assets/maps/" + lv.mapTheme + "/bg.jpg)";
            }
            const tags = (lv.uiTags || []).map(x => `<span class="lv-tag">${x}</span>`).join("");
            const targetBadge = isTarget ? `<span class="lv-tag lv-current">当前战役</span>` : "";
            const stars = window.Progress ? Progress.getLevelStars(i) : 0;
            const starLine = stars > 0
                ? `<div class="lv-stars" aria-label="通关星级">${Progress.starsDisplay(stars)}</div>`
                : "";
            card.innerHTML = `
                <div class="lv-meta">第 ${i + 1} 战 · ${lv.difficulty}</div>
                <div class="lv-name">${lv.name}</div>
                ${starLine}
                <div class="lv-tags">${tags}${targetBadge}</div>
                <div class="lv-desc">${lv.desc}</div>
                ${lv.modifiers && lv.modifiers.desc ? `<div class="lv-meta lv-mod">${lv.modifiers.desc}</div>` : ""}
                <div class="lv-meta">起始金 ${lv.startGold} · 城防 ${lv.life} · 共 ${lv.waves.length} 波</div>
                <div class="lv-meta lv-win">${lv.winObjectiveText || "守满全部波次，歼灭全部敌军"}</div>
            `;
            if (!window.Progress || Progress.isLevelUnlocked(i)) {
                const bossTag = lv.bossName ? `<span class="lv-tag lv-boss">关底 · ${lv.bossName}</span>` : "";
                card.querySelector(".lv-tags").innerHTML += bossTag;
                card.onclick = () => Main.openLineup(i);
            } else {
                card.classList.add("locked");
                card.querySelector(".lv-meta").textContent += " · 未解锁";
            }
            list.appendChild(card);
        });
        this._scrollLevelSelectToPlayable();
    },

    openLineup(levelIndex) {
        Main.pendingLevelIndex = levelIndex;
        const lv = LEVELS[levelIndex];
        const title = document.getElementById("lineup-title");
        if (title) {
            const obj = lv.winObjectiveText ? ` · 胜利：${lv.winObjectiveText}` : "";
            title.textContent = `第 ${levelIndex + 1} 战 · ${lv.name} — 选择出战武将${obj}`;
        }
        this._lineupSelected = new Set(Progress.getDefaultLineupForLevel(levelIndex));
        this.buildLineupGrid();
        this.show("lineup-screen");
    },

    buildLineupGrid() {
        const grid = document.getElementById("lineup-grid");
        if (!grid) return;
        grid.innerHTML = "";
        const max = Campaign.MAX_LINEUP;
        Progress.getUnlockedGeneralIds().forEach(id => {
            const g = getGeneral(id);
            if (!g) return;
            const card = document.createElement("div");
            const sel = this._lineupSelected.has(id);
            card.className = "lineup-card" + (sel ? " selected" : "");
            card.innerHTML = `
                <canvas class="lineup-portrait" width="68" height="68"></canvas>
                <div class="lineup-name">${g.name}</div>
                <div class="lineup-tags">${this.generalTagHtml(g)}</div>
                <div class="lineup-ult">${g.ultimate.name}</div>
            `;
            this._drawPortraitToCanvas(card.querySelector("canvas"), g, 68);
            card.onclick = () => {
                if (this._lineupSelected.has(id)) {
                    this._lineupSelected.delete(id);
                } else if (this._lineupSelected.size >= max) {
                    this.showBanner(`最多选择 ${max} 人`);
                    return;
                } else {
                    this._lineupSelected.add(id);
                }
                this.buildLineupGrid();
            };
            grid.appendChild(card);
        });
    },

    initLineupUI() {
        const back = document.getElementById("btn-lineup-back");
        const start = document.getElementById("btn-lineup-start");
        if (back) back.onclick = () => this.show("level-select");
        if (start) start.onclick = () => {
            if (!this._lineupSelected || this._lineupSelected.size < 1) {
                this.showBanner("请至少选择 1 名武将");
                return;
            }
            const ids = Array.from(this._lineupSelected);
            Progress.setLastLineup(ids);
            Main.startGame(Main.pendingLevelIndex, ids);
        };
    },

    enemyCodexBlurb(t) {
        if (!t) return "";
        const parts = [];
        if (t.isBoss) parts.push("关底首领");
        if (t.fireVulnerable) parts.push("惧火");
        if (t.splashVulnerableMul) parts.push("惧溅射");
        if (t.pierceExtraCost) parts.push("反制穿透");
        if (t.deathNovaRadius) parts.push("阵亡时对邻近敌军造成真实伤害");
        if (t.regenPerSec) parts.push(`每秒自回 ${t.regenPerSec} 生命`);
        if (t.armor >= 0.35) parts.push("高甲");
        if (!t.isBoss && t.speed >= 108) parts.push("高速");
        if (t.weapon === "siege" || t.weapon === "baggage") parts.push("大型目标");
        return parts.length ? parts.join(" · ") : "常规单位";
    },

    buildEnemyCodex() {
        const list = document.getElementById("codex-enemy-list");
        if (!list || !window.ENEMY_TYPES) return;
        list.innerHTML = "";
        const keys = Object.keys(ENEMY_TYPES).filter(k => !ENEMY_TYPES[k].isBoss);
        keys.sort((a, b) => ENEMY_TYPES[a].name.localeCompare(ENEMY_TYPES[b].name, "zh"));
        keys.push("boss");
        for (const key of keys) {
            const t = ENEMY_TYPES[key];
            if (!t) continue;
            const card = document.createElement("div");
            card.className = "enemy-codex-card";
            card.innerHTML = `<div class="ec-name">${t.name}</div><div class="ec-blurb">${this.enemyCodexBlurb(t)}</div>`;
            list.appendChild(card);
        }
    },

    buildCodex() {
        const list = document.getElementById("codex-list");
        if (!list) return;
        list.innerHTML = "";
        const factionOrder = ["蜀", "魏", "吴", "群", "汉", "晋"];
        const byFaction = {};
        GENERALS.forEach(g => {
            const f = g.faction || "群";
            if (!byFaction[f]) byFaction[f] = [];
            byFaction[f].push(g);
        });
        const factions = factionOrder.filter(f => byFaction[f]);
        Object.keys(byFaction).forEach(f => {
            if (!factions.includes(f)) factions.push(f);
        });
        const self = this;
        const appendCard = (parent, g) => {
            const unlocked = isGeneralUnlocked(g.id);
            const card = document.createElement("div");
            card.className = "codex-card codex-card-v" + (unlocked ? "" : " locked");
            if (!unlocked) {
                card.innerHTML = '<canvas class="codex-portrait codex-portrait-top" width="80" height="80"></canvas>'
                    + '<div class="codex-body"><div class="c-name">未收录</div>'
                    + '<div class="c-desc">通关战役或达成里程碑后可查看。</div></div>';
                parent.appendChild(card);
                const ctx = card.querySelector("canvas").getContext("2d");
                Art.drawPortraitSilhouette(ctx, 40, 40, 36, g);
                return;
            }
            const portraitId = "cdx-" + g.id;
            const ultType = g.ultimate.type || "stun";
            card.innerHTML = '<canvas id="' + portraitId + '" class="codex-portrait codex-portrait-top" width="80" height="80"></canvas>'
                + '<div class="codex-body"><div class="c-name">' + g.name + '</div>'
                + '<div class="c-tags">' + self.generalTagHtml(g) + ' <span class="c-title">' + (g.title || "") + '</span></div>'
                + '<div class="c-combat"><span class="c-combat-label">作战</span> ' + self.attackTypeLabel(g.attackType) + ' · 「' + self.projectileLabel(g.projectileType) + '」</div>'
                + '<div class="c-skill"><b>' + g.ultimate.name + '</b><span class="c-ult-type">主动 · ' + self.ultimateTypeLabel(ultType) + '</span></div>'
                + '<div class="c-desc c-ult-full">' + self.ultimateShortText(g) + '</div>'
                + '<div class="c-desc">' + g.story + '</div></div>';
            parent.appendChild(card);
            this._drawPortraitToCanvas(document.getElementById(portraitId), g, 80);
        };
        factions.forEach(faction => {
            const hdr = document.createElement("h3");
            hdr.className = "codex-faction-title";
            hdr.textContent = faction;
            list.appendChild(hdr);
            const grid = document.createElement("div");
            grid.className = "codex-faction-grid";
            list.appendChild(grid);
            byFaction[faction].forEach(g => appendCard(grid, g));
        });
        this.buildEnemyCodex();
    },

    buildGeneralBar(lineupIds) {
        const bar = document.getElementById("general-bar");
        bar.innerHTML = "";
        const ids = lineupIds || Main.lineupIds || ["guanyu"];
        ids.map(id => getGeneral(id)).filter(Boolean).forEach(g => {
            const chip = document.createElement("div");
            chip.className = "gen-chip";
            chip.dataset.id = g.id;
            chip.style.setProperty("--gen-accent", g.color || "#8a6a3c");
            const combat = this.generalCombatTags(g).join(" · ");
            const ult = g.ultimate && g.ultimate.name ? g.ultimate.name : "";
            chip.title = [g.name, combat, ult].filter(Boolean).join(" · ");
            chip.innerHTML = `
                <canvas class="gen-chip-portrait" width="64" height="64"></canvas>
                <span class="gen-cost-badge">${g.cost}</span>
            `;
            bar.appendChild(chip);
            const cvs = chip.querySelector("canvas");
            this._drawPortraitToCanvas(cvs, g, 64);
            chip.onclick = () => {
                if (chip.classList.contains("disabled")) return;
                Game.pickGeneralToPlace(g);
            };
        });
    },

    refreshGeneralBarAffordability() {
        document.querySelectorAll(".gen-chip").forEach(chip => {
            const id = chip.dataset.id;
            const g = GENERALS.find(x => x.id === id);
            if (!g) return;
            const short = g.cost + " 金";
            const combat = this.generalCombatTags(g).join(" · ");
            const ult = g.ultimate && g.ultimate.name ? g.ultimate.name : "";
            if (g.cost > Game.gold) {
                chip.classList.add("disabled");
                const lack = g.cost - Game.gold;
                chip.title = g.name + " · 差 " + lack + " 金 · " + [combat, ult].filter(Boolean).join(" · ");
            } else {
                chip.classList.remove("disabled");
                chip.title = [g.name, combat, ult, short].filter(Boolean).join(" · ");
            }
            if (Game.placingGeneral && Game.placingGeneral.id === id) {
                chip.classList.add("gen-placing");
            } else {
                chip.classList.remove("gen-placing");
            }
        });
    },

    refreshPauseSummary() {
        const el = document.getElementById("pause-rule-summary");
        if (!el) return;
        if (!Game.level) {
            el.textContent = "";
            return;
        }
        const m = Game.level.modifiers;
        el.textContent = m && m.desc ? "本关规则：" + m.desc : "本关无额外战场修正。";
    },

    refreshTopbar() {
        const goldEl = document.getElementById("ui-gold");
        const lifeEl = document.getElementById("ui-life");
        const waveEl = document.getElementById("ui-wave");
        if (goldEl) goldEl.textContent = Game.gold;
        if (lifeEl) lifeEl.textContent = Game.life;
        const total = Game.level ? Game.level.waves.length : 0;
        if (waveEl) waveEl.textContent = `${Math.min(Game.waveIdx, total)}/${total}`;

        const extra = document.getElementById("ui-wave-extra");
        const objEl = document.getElementById("ui-objective");
        const bar = document.getElementById("ui-wave-progress-bar");
        const placing = document.getElementById("ui-placing-badge");
        const speedBadge = document.getElementById("ui-speed-badge");
        const btnSpeed = document.getElementById("btn-speed");

        if (objEl) {
            if (!Game.level || (Game.state !== "running" && Game.state !== "paused")) {
                objEl.textContent = "";
            } else {
                objEl.textContent = "胜利：" + (Game.level.winObjectiveText || "歼灭全部敌军");
            }
        }

        if (extra) {
            if (!Game.level || (Game.state !== "running" && Game.state !== "paused")) {
                extra.textContent = "";
            } else if (Game.waveIdx >= Game.level.waves.length) {
                const n = Game.enemies.length;
                extra.textContent = n > 0 ? ` · 清场 · 剩余 ${n}` : " · 清场";
            } else if (Game.spawning && Game.waveIdx === Game.level.waves.length - 1) {
                extra.textContent = " · 关底战";
            } else if (Game.spawning) {
                extra.textContent = " · 交战中";
            } else {
                const sec = Math.max(0, Math.ceil(Game.waveTimer));
                const isFinal = Game.waveIdx === Game.level.waves.length - 1;
                extra.textContent = isFinal ? ` · 最终波 ${sec}s` : ` · 下波 ${sec}s`;
            }
        }
        if (bar && Game.level && (Game.state === "running" || Game.state === "paused")) {
            if (Game.spawning || Game.waveIdx >= Game.level.waves.length) {
                bar.style.width = "100%";
                bar.classList.add("wave-bar-busy");
            } else {
                bar.classList.remove("wave-bar-busy");
                const w = Game.level.waves[Game.waveIdx];
                const delay = (w && w.delay) ? w.delay : 1;
                const p = delay > 0 ? Math.min(1, Math.max(0, 1 - Game.waveTimer / delay)) : 1;
                bar.style.width = (p * 100) + "%";
            }
        } else if (bar) {
            bar.style.width = "0%";
            bar.classList.remove("wave-bar-busy");
        }

        if (placing) {
            if (Game.placingGeneral) placing.classList.remove("hidden");
            else placing.classList.add("hidden");
        }
        const mergeBadge = document.getElementById("ui-merge-badge");
        if (mergeBadge) {
            if (Game.mergePickSource) mergeBadge.classList.remove("hidden");
            else mergeBadge.classList.add("hidden");
        }
        if (speedBadge) speedBadge.textContent = "倍速 ×" + Game.speed;
        if (btnSpeed) {
            btnSpeed.textContent = "×" + Game.speed;
            btnSpeed.classList.toggle("speed-active", Game.speed > 1);
        }
    },

    refreshTowerPanel() {
        const panel = document.getElementById("tower-panel");
        const t = Game.selectedTower;

        if (Game.mergePickSource) {
            panel.classList.add("hidden");
            panel.classList.remove("tp-pop", "tp-ult-glow", "tp-merge-pick");
            return;
        }

        if (!t) {
            this._tpSelRef = null;
            panel.classList.add("hidden");
            panel.classList.remove("tp-pop", "tp-ult-glow");
            return;
        }
        const isNewSelection = this._tpSelRef !== t;
        this._tpSelRef = t;
        panel.classList.remove("hidden");

        if (isNewSelection) {
            panel.classList.remove("tp-pop");
            void panel.offsetWidth;
            panel.classList.add("tp-pop");
            clearTimeout(this._tpPopT);
            this._tpPopT = setTimeout(() => panel.classList.remove("tp-pop"), 320);
        }

        const g = t.general;
        // 头像
        const portrait = document.getElementById("tp-portrait");
        portrait.innerHTML = "<canvas width='48' height='48'></canvas>";
        this._drawPortraitToCanvas(portrait.querySelector("canvas"), g, 48);

        document.getElementById("tp-name").textContent = `${g.name} · ${g.title}`;
        let lvText = `Lv.${t.level}` + (t.canUpgrade() ? "" : "（满级）");
        if (t.mergeLabel()) lvText += ` · ${t.mergeLabel()}`;
        document.getElementById("tp-level").textContent = lvText;

        const combatEl = document.getElementById("tp-combat-line");
        if (combatEl) {
            combatEl.innerHTML = `<span class="tp-combat-main">${this.attackTypeLabel(g.attackType)}</span><span class="tp-combat-sub">弹道「${this.projectileLabel(g.projectileType)}」</span>`;
        }
        const ultNameEl = document.getElementById("tp-ult-name");
        const ultSumEl = document.getElementById("tp-ult-summary");
        if (ultNameEl) ultNameEl.textContent = g.ultimate.name;
        if (ultSumEl) ultSumEl.textContent = this.ultimateShortText(g);
        const ultAutoEl = document.getElementById("tp-ult-auto");
        if (ultAutoEl) ultAutoEl.checked = window.Progress ? Progress.getUltAuto() : true;

        const stats = t.stats;
        const mergeMult = (s, tier) => {
            const m = Math.pow(2, tier);
            const rateMul = Math.pow(0.85, tier);
            return {
                ...s,
                dmg: Math.round(s.dmg * m),
                range: Math.round(s.range * m),
                rate: +(s.rate * rateMul).toFixed(2),
                splash: s.splash ? Math.round(s.splash * m) : 0,
                pierce: s.pierce || 0,
                slow: s.slow || 0
            };
        };
        const nextStats = t.canUpgrade() ? mergeMult(g.levels[t.level], t.mergeTier) : null;
        const fmt = (label, cur, next) => {
            if (next === undefined || next === null || next === cur) return `${label}：${cur}`;
            return `${label}：${cur} <span class="up">→ ${next}</span>`;
        };
        const fmtRate = (r) => (r != null && !isNaN(r) ? r.toFixed(2) + "s" : null);
        let statsHtml = `
            ${fmt("伤害", stats.dmg, nextStats?.dmg)}<br/>
            ${fmt("射程", stats.range, nextStats?.range)}<br/>
            ${fmt("攻速", fmtRate(stats.rate), nextStats ? fmtRate(nextStats.rate) : null)}<br/>
            ${stats.splash ? fmt("溅射", stats.splash, nextStats?.splash) + "<br/>" : ""}
            ${stats.pierce ? fmt("穿透", stats.pierce, nextStats?.pierce) + "<br/>" : ""}
            ${stats.slow ? fmt("减速", (stats.slow * 100 | 0) + "%", nextStats ? (nextStats.slow * 100 | 0) + "%" : null) + "<br/>" : ""}
        `;
        statsHtml += this.upgradeNewHints(stats, nextStats, g);
        document.getElementById("tp-stats").innerHTML = statsHtml;

        // 怒气
        const ratio = t.rage / t.maxRage;
        const fill = document.getElementById("tp-rage-fill");
        fill.style.width = (ratio * 100) + "%";
        fill.classList.toggle("rage-fill--full", ratio >= 0.999);
        document.getElementById("tp-rage-text").textContent = `怒气 ${t.rage | 0}/${t.maxRage}`;

        // 升级按钮
        const upBtn = document.getElementById("tp-upgrade");
        if (t.canUpgrade()) {
            const cost = t.upgradeCost();
            upBtn.textContent = `升级 ${cost}`;
            upBtn.disabled = Game.gold < cost;
        } else {
            upBtn.textContent = "已满级";
            upBtn.disabled = true;
        }

        const ultBtn = document.getElementById("tp-ultimate");
        const canUlt = t.canUlt();
        ultBtn.textContent = canUlt ? `${g.ultimate.name}！` : `${g.ultimate.name} · ${(ratio * 100) | 0}%`;
        ultBtn.disabled = !canUlt;
        ultBtn.setAttribute("aria-label", canUlt ? `释放大招 ${g.ultimate.name}：${this.ultimateShortText(g)}` : `怒气未满，当前 ${(ratio * 100) | 0}%`);
        ultBtn.classList.toggle("ult-ready-pulse", canUlt);
        panel.classList.toggle("tp-ult-glow", canUlt);

        const mergeBtn = document.getElementById("tp-merge");
        const partners = Game.findMergePartners(t);
        const canMerge = partners.length > 0;
        if (mergeBtn) {
            if (Game.mergePickSource === t) {
                mergeBtn.textContent = "点选同将…";
                mergeBtn.disabled = false;
                mergeBtn.classList.add("btn-merge-active");
            } else if (canMerge) {
                mergeBtn.textContent = `合成${t.nextMergeLabel()}`;
                mergeBtn.disabled = false;
                mergeBtn.classList.remove("btn-merge-active");
            } else {
                mergeBtn.classList.remove("btn-merge-active");
                if (t.mergeTier >= 2) {
                    mergeBtn.textContent = "已满阶";
                    mergeBtn.disabled = true;
                } else if (t.level < 3) {
                    mergeBtn.textContent = "Lv.3 可合成";
                    mergeBtn.disabled = true;
                } else {
                    mergeBtn.textContent = "需同名满级";
                    mergeBtn.disabled = true;
                }
            }
        }
        panel.classList.toggle("tp-merge-pick", Game.mergePickSource === t);

        document.getElementById("tp-sell").textContent = `撤退 +${t.sellValue()}`;

        const mergeBlock = document.getElementById("tp-merge-block");
        const mergeList = document.getElementById("tp-merge-list");
        if (mergeBlock && mergeList) {
            if (partners.length) {
                mergeBlock.classList.remove("hidden");
                mergeList.innerHTML = "";
                partners.forEach((other) => {
                    const dist = Math.round(U.dist(t.x, t.y, other.x, other.y));
                    const btn = document.createElement("button");
                    btn.type = "button";
                    btn.className = "tp-merge-item";
                    btn.innerHTML = `<span>${g.name} · ${other.mergeLabel() || "满级"}</span><span class="tp-merge-meta">距 ${dist} · 合为${t.nextMergeLabel()}</span>`;
                    btn.onclick = () => Game.mergeTowers(t, other);
                    mergeList.appendChild(btn);
                });
            } else {
                mergeBlock.classList.add("hidden");
                mergeList.innerHTML = "";
            }
        }
        this.layoutTowerPanelDock();
    },

    /** 右侧 dock：相对 game-screen + 底栏定位，避免矮横屏裁切操作按钮 */
    layoutTowerPanelDock() {
        const panel = document.getElementById("tower-panel");
        const gameScreen = document.getElementById("game-screen");
        const bar = document.getElementById("general-bar");
        const stage = document.querySelector(".stage");
        if (!panel || !gameScreen || panel.classList.contains("hidden")) return;

        panel.style.left = "auto";
        panel.style.top = "auto";
        panel.style.right = "";
        panel.style.bottom = "";

        const stageR = stage ? stage.getBoundingClientRect() : gameScreen.getBoundingClientRect();
        const w = Math.min(248, Math.max(176, Math.floor(stageR.width * 0.36)));
        panel.style.width = w + "px";

        const gsR = gameScreen.getBoundingClientRect();
        const barR = bar ? bar.getBoundingClientRect() : null;
        const topbar = document.querySelector(".topbar");
        const topLimit = topbar ? topbar.getBoundingClientRect().bottom + 6 : gsR.top + 72;

        let maxH;
        if (barR) {
            maxH = barR.top - topLimit - 10;
        } else {
            maxH = gsR.height - 140;
        }
        maxH = Math.max(148, Math.min(maxH, gsR.height * 0.54));
        panel.style.maxHeight = maxH + "px";
        panel.style.height = maxH + "px";
    },

    initGameUI() {
        this.initLineupUI();
        this.buildGeneralBar();

        document.getElementById("tp-upgrade").onclick = () => Game.upgradeSelected();
        const ultAutoCb = document.getElementById("tp-ult-auto");
        if (ultAutoCb) {
            ultAutoCb.checked = window.Progress ? Progress.getUltAuto() : true;
            ultAutoCb.onchange = () => {
                if (window.Progress) Progress.setUltAuto(ultAutoCb.checked);
            };
        }
        document.getElementById("tp-ultimate").onclick = () => {
            const tw = Game.selectedTower;
            if (!tw || !tw.canUlt()) return;
            const nm = tw.general.ultimate.name;
            Game.castUltSelected();
            this.showUltToast(`已释放 · ${nm}`);
        };
        const mergeBtnEl = document.getElementById("tp-merge");
        if (mergeBtnEl) {
            mergeBtnEl.onclick = () => {
                const tw = Game.selectedTower;
                if (!tw) return;
                if (Game.mergePickSource === tw) {
                    Game.mergePickSource = null;
                    if (Game.onUpdate) Game.onUpdate();
                    return;
                }
                if (Game.beginMergePick(tw)) {
                    this.showBanner("点选另一尊同名满级武将完成合成", 2200);
                }
            };
        }
        document.getElementById("tp-sell").onclick = () => Game.sellSelected();

        document.getElementById("btn-pause").onclick = () => {
            if (Game.state === "running") {
                Game.pause();
                this.refreshPauseSummary();
                document.getElementById("pause-mask").classList.remove("hidden");
            }
        };
        const pauseSettings = document.getElementById("btn-pause-settings");
        if (pauseSettings) {
            pauseSettings.onclick = () => {
                document.getElementById("pause-mask").classList.add("hidden");
                this.openSettings(true);
            };
        }
        document.getElementById("btn-resume").onclick = () => {
            Game.resume();
            document.getElementById("pause-mask").classList.add("hidden");
        };
        document.getElementById("btn-restart").onclick = () => {
            document.getElementById("pause-mask").classList.add("hidden");
            Main.startGame(Main.currentLevel, Main.lineupIds);
        };
        document.getElementById("btn-back-menu").onclick = () => {
            document.getElementById("pause-mask").classList.add("hidden");
            UI.show("menu");
        };
        document.getElementById("btn-quit").onclick = () => UI.show("menu");
        document.getElementById("btn-speed").onclick = () => {
            const next = Game.speed === 1 ? 2 : (Game.speed === 2 ? 3 : 1);
            Game.setSpeed(next);
        };

        document.getElementById("btn-replay").onclick = () => {
            document.getElementById("result-mask").classList.add("hidden");
            Main.startGame(Main.currentLevel, Main.lineupIds);
        };
        document.getElementById("btn-next").onclick = () => {
            document.getElementById("result-mask").classList.add("hidden");
            const next = Main.currentLevel + 1;
            if (next < LEVELS.length && Progress.isLevelUnlocked(next)) {
                UI.openLineup(next);
            } else if (next < LEVELS.length) {
                UI.show("menu");
            } else {
                UI.show("menu");
            }
        };
        document.getElementById("btn-result-menu").onclick = () => {
            document.getElementById("result-mask").classList.add("hidden");
            UI.show("menu");
        };

        // Demo 控制条
        const btnCastAll = document.getElementById("btn-cast-all");
        if (btnCastAll) {
            btnCastAll.onclick = () => {
                // 每隔 1.2 秒释放一个塔的大招
                Game.towers.forEach((t, i) => {
                    setTimeout(() => {
                        if (t && Game.towers.includes(t)) {
                            t.rage = t.maxRage;
                            Game.selectedTower = t;
                            Game.castUltSelected();
                        }
                    }, i * 1200);
                });
            };
        }
        const btnSpawn = document.getElementById("btn-spawn");
        if (btnSpawn) {
            btnSpawn.onclick = () => {
                // 召唤一组测试敌人
                ["fangji", "huojian", "desperado", "junyi", "scout", "tiger", "shield", "rattan"].forEach((k, i) => {
                    setTimeout(() => Game._spawnEnemy(k), i * 300);
                });
            };
        }
    },

    showBanner(text, ms = 1400, variant) {
        const b = document.getElementById("banner");
        b.textContent = text;
        b.classList.remove("hidden", "banner-toast", "banner-hero");
        if (variant === "toast") b.classList.add("banner-toast");
        else b.classList.add("banner-hero");
        void b.offsetWidth;
        b.classList.add("show");
        clearTimeout(this._bannerT);
        this._bannerT = setTimeout(() => {
            b.classList.remove("show");
            setTimeout(() => b.classList.add("hidden"), 350);
        }, ms);
    },

    showResult(state) {
        if (window.Haptics) {
            if (state === "victory") Haptics.victory();
            else Haptics.defeat();
        }
        document.getElementById("result-mask").classList.remove("hidden");
        const bossEscaped = state !== "victory" && Game.defeatReason === "boss_escape";
        const bossName = Game.level && Game.level.bossName;
        document.getElementById("result-title").textContent = state === "victory"
            ? "大获全胜"
            : (bossEscaped ? "敌将逃脱" : "山河易主");
        let mainText = state === "victory"
            ? `${Game.level.name} 已平定，威震四方！`
            : (bossEscaped
                ? `${bossName || "关底敌将"}突入皇城，此战失利。须阵斩敌将并清场方可过关。`
                : `${Game.level.name} 失守，再图后举。`);
        if (state === "victory") {
            if (Game._bossKilled && bossName) {
                mainText += `\n已阵斩关底 · ${bossName}。`;
            } else if (Game.level && Game.level.hasBoss) {
                mainText += "\n已歼灭全部敌军。";
            }
        }
        if (state === "victory" && window.Progress) {
            const u = Progress.onVictory(Main.currentLevel);
            const parts = [];
            if (u.newlyBoss) parts.push(Campaign.bossUnlockMessage(u.bossId));
            if (u.newlyMilestone) {
                const mg = getGeneral(u.newlyMilestone);
                parts.push(mg ? `${mg.name}已加入我军！` : "名将已加入！");
            }
            if (parts.length) mainText += "\n" + parts.join("\n");
        }
        document.getElementById("result-text").textContent = mainText;
        const statsEl = document.getElementById("result-stats");
        const rs = Game._lastResultStats;
        if (statsEl && Game.level && rs) {
            const start = rs.startLife != null ? rs.startLife : Game.level.life;
            const remain = rs.life != null ? rs.life : Game.life;
            const leaks = rs.leaks || 0;
            const kills = rs.kills || 0;
            const goldLeft = rs.goldLeft != null ? rs.goldLeft : Game.gold;
            let stars = 0;
            if (state === "victory" && window.Progress) {
                stars = Progress.recordLevelResult(Main.currentLevel, rs);
            }
            const starTxt = state === "victory" && stars > 0
                ? `通关评价 ${Progress.starsDisplay(stars)} · `
                : "";
            const base = state === "victory"
                ? `${starTxt}漏怪 ${leaks} 次 · 击杀 ${kills} · 剩余金 ${goldLeft} · 城防 ${remain}/${start}`
                : (bossEscaped
                    ? `关底敌将未被击杀 · 击杀 ${kills} · 城防 ${remain}/${start}`
                    : `漏怪 ${leaks} 次 · 击杀 ${kills} · 城防 ${remain}/${start}`);
            statsEl.textContent = base;
        } else if (statsEl) {
            statsEl.textContent = "";
        }
        document.getElementById("btn-next").style.display =
            (state === "victory" && Main.currentLevel < LEVELS.length - 1) ? "block" : "none";
    }
};
