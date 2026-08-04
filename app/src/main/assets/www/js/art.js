// 绘制：武将立绘 / 头像 / 敌人 / 障碍 / 路径 / 大招特效
// 优先使用 assets/ 手绘资源（见 docs/ART_PLAN.md），缺失时 Canvas 2D 程序绘制兜底

window.Art = {
    _coverImage(ctx, img, W, H) {
        const scale = Math.max(W / img.width, H / img.height);
        const dw = img.width * scale;
        const dh = img.height * scale;
        ctx.drawImage(img, (W - dw) / 2, (H - dh) / 2, dw, dh);
    },
    _worldScale(ctx) {
        if (typeof Game !== "undefined" && Game.renderScale) return Game.renderScale;
        const t = ctx.getTransform();
        return Math.hypot(t.a, t.b) || 1;
    },

    _decorDensity() {
        const rs = (typeof Game !== "undefined" && Game.renderScale) ? Game.renderScale : 1;
        return U.clamp(rs, 1, 1.5);
    },

    // 圆形头像内立绘：cover 裁剪，锚点对齐脸部（见 PORTRAIT_FACE_ANCHOR）
    _drawPortraitImageFace(ctx, img, r, general) {
        const id = general && general.id;
        const ov = (window.PORTRAIT_FACE_ANCHOR && id && PORTRAIT_FACE_ANCHOR[id]) || {};
        const ax = ov.x != null ? ov.x : 0.5;
        const ay = ov.y != null ? ov.y : 0.28;
        const diam = r * 2;
        const scale = Math.max(diam / img.width, diam / img.height);
        const w = img.width * scale;
        const h = img.height * scale;
        // 锚点 (ax, ay) 对齐圆心 (0,0)
        ctx.drawImage(img, -ax * w, -ay * h, w, h);
    },

    // ============ 武将头像 (圆形) ============
    drawPortrait(ctx, x, y, r, general) {
        ctx.save();
        ctx.translate(x, y);

        const img = window.ArtAssets && general.id ? ArtAssets.getPortrait(general.id) : null;

        ctx.beginPath();
        ctx.arc(0, 0, r, 0, Math.PI * 2);
        ctx.closePath();
        ctx.save();
        ctx.clip();

        if (img) {
            Art._drawPortraitImageFace(ctx, img, r, general);
        } else {
            const grd = ctx.createRadialGradient(0, -r * 0.4, r * 0.2, 0, 0, r);
            grd.addColorStop(0, U.shade(general.color, 60));
            grd.addColorStop(1, U.shade(general.color, -40));
            ctx.fillStyle = grd;
            ctx.fillRect(-r, -r, r * 2, r * 2);
            ctx.fillStyle = "rgba(0,0,0,.18)";
            for (let i = -r; i < r; i += 6) {
                ctx.fillRect(-r, i, r * 2, 1);
            }
            Art._drawHero(ctx, r, general);
        }

        ctx.restore();

        ctx.lineWidth = Math.max(2, r * 0.08);
        ctx.strokeStyle = general.accent;
        ctx.stroke();

        ctx.restore();
    },

    // ============ 战场上武将塔（俯视角） ============
    drawMergeHighlight(ctx, x, y) {
        ctx.save();
        ctx.translate(x, y);
        const pulse = 0.5 + 0.5 * Math.sin(Date.now() / 200);
        ctx.strokeStyle = `rgba(247, 215, 116, ${0.55 + pulse * 0.35})`;
        ctx.lineWidth = 3;
        ctx.setLineDash([6, 4]);
        ctx.beginPath();
        ctx.arc(0, 0, 38, 0, Math.PI * 2);
        ctx.stroke();
        ctx.setLineDash([]);
        ctx.restore();
    },

    drawTower(ctx, tower) {
        const { x, y, general, level, mergeTier } = tower;
        const P = window.Projection;
        const iso = P && P.enabled;
        ctx.save();
        ctx.translate(x, y);

        const base = 22 + level * 2 + (mergeTier || 0) * 3;

        // ===== 地面层：阴影 + 石台底盘（始终画在 y=0 地面） =====
        if (iso) {
            // 2.5D：额外投射阴影
            P.drawShadow(ctx, base * 1.1, base * 0.42, P.SHADOW_ALPHA, 6);
        }

        ctx.fillStyle = "rgba(0,0,0,.35)";
        ctx.beginPath();
        ctx.ellipse(0, 10, base * 0.95, base * 0.35, 0, 0, Math.PI * 2);
        ctx.fill();
        ctx.fillStyle = "rgba(0,0,0,.4)";
        ctx.beginPath();
        ctx.ellipse(0, 6, base, base * 0.45, 0, 0, Math.PI * 2);
        ctx.fill();

        // 阵营石台底盘
        ctx.fillStyle = "rgba(60, 42, 24, 0.85)";
        ctx.beginPath();
        ctx.ellipse(0, 8, base + 4, base * 0.38, 0, 0, Math.PI * 2);
        ctx.fill();
        ctx.strokeStyle = "rgba(201, 137, 43, 0.45)";
        ctx.lineWidth = 1.5;
        ctx.beginPath();
        ctx.ellipse(0, 6, base + 1, base * 0.32, 0, 0, Math.PI * 2);
        ctx.stroke();
        ctx.fillStyle = U.shade(general.color, -20);
        ctx.beginPath();
        ctx.ellipse(0, 4, base - 1, base * 0.28, 0, 0, Math.PI * 2);
        ctx.fill();

        if (mergeTier === 1) {
            ctx.strokeStyle = "rgba(220, 50, 40, 0.85)";
            ctx.lineWidth = 3;
            ctx.beginPath();
            ctx.ellipse(0, 4, base + 6, base * 0.5 + 4, 0, 0, Math.PI * 2);
            ctx.stroke();
        } else if (mergeTier === 2) {
            ctx.strokeStyle = "rgba(247, 215, 116, 0.95)";
            ctx.lineWidth = 4;
            ctx.beginPath();
            ctx.ellipse(0, 4, base + 8, base * 0.55 + 5, 0, 0, Math.PI * 2);
            ctx.stroke();
            const grd = ctx.createRadialGradient(0, 0, 0, 0, 0, base + 10);
            grd.addColorStop(0, "rgba(255, 220, 100, 0.25)");
            grd.addColorStop(1, "rgba(255, 180, 40, 0)");
            ctx.fillStyle = grd;
            ctx.beginPath();
            ctx.arc(0, 0, base + 10, 0, Math.PI * 2);
            ctx.fill();
        }

        ctx.fillStyle = mergeTier === 2 ? "#f7d774" : (mergeTier === 1 ? "#e85a40" : general.accent);
        ctx.font = "bold 10px serif";
        ctx.textAlign = "center";
        let tierTag = "★".repeat(level);
        if (mergeTier === 1) tierTag += " 红";
        if (mergeTier === 2) tierTag += " 金";
        ctx.fillText(tierTag, 0, base * 0.5 + 12);

        // ===== 2.5D 高度层：将头像本体抬升至石台上方 =====
        const r = 18 + level * 1.5;

        if (iso) {
            // 画一个从底盘到头像的"石柱"侧面，给立体感
            const colH = P.heightFor("tower", { level, mergeTier });
            const liftY = P.heightOffset(colH, 1);

            // 石柱侧面（深色梯形）
            ctx.fillStyle = "rgba(40, 28, 16, 0.6)";
            ctx.beginPath();
            ctx.moveTo(-base * 0.5, 0);
            ctx.lineTo(base * 0.5, 0);
            ctx.lineTo(base * 0.35, liftY + 2);
            ctx.lineTo(-base * 0.35, liftY + 2);
            ctx.closePath();
            ctx.fill();

            // 石柱顶面
            ctx.fillStyle = "rgba(80, 56, 30, 0.7)";
            ctx.beginPath();
            ctx.ellipse(0, liftY + 2, base * 0.4, base * 0.14, 0, 0, Math.PI * 2);
            ctx.fill();

            ctx.translate(0, liftY - 4);
        } else {
            ctx.translate(0, -8);
        }

        // 旋转朝向当前目标
        if (tower.aim != null) {
            ctx.save();
            ctx.rotate(tower.aim + Math.PI / 2);
            // 武器拖影
            Art._drawWeapon(ctx, general, level);
            ctx.restore();
        }

        Art.drawPortrait(ctx, 0, 0, r, general);
        ctx.restore();
    },

    // 武器（简单线条）
    _drawWeapon(ctx, g, level) {
        ctx.strokeStyle = g.accent;
        ctx.lineWidth = 2 + level;
        ctx.beginPath();
        ctx.moveTo(0, -10);
        ctx.lineTo(0, -28 - level * 2);
        ctx.stroke();
        ctx.fillStyle = "#cccccc";
        ctx.beginPath();
        ctx.arc(0, -30 - level * 2, 3 + level, 0, Math.PI * 2);
        ctx.fill();
    },

    // ============ 各武将差异化绘制 (在 [-r,r] 局部坐标) ============
    _idSeed(id) {
        let h = 0;
        const s = id || "";
        for (let i = 0; i < s.length; i++) h = ((h << 5) - h) + s.charCodeAt(i);
        return Math.abs(h);
    },

    _drawHero(ctx, r, g) {
        const fn = Art["_hero_" + g.id];
        if (fn) { fn(ctx, r, g); return; }
        if (window.PORTRAIT_TRAITS && PORTRAIT_TRAITS[g.id] && !(window.CORE_PORTRAIT_IDS && CORE_PORTRAIT_IDS.has(g.id))) {
            Art._drawPortraitTraits(ctx, r, g);
            return;
        }
        const arch = g.archetype || g.attackType;
        const afn = Art["_heroArchetype_" + arch];
        if (afn) afn(ctx, r, g);
        else Art._heroFallback(ctx, r, g);
    },

    _drawPortraitTraits(ctx, r, g) {
        const t = PORTRAIT_TRAITS[g.id];
        if (!t) { Art._heroFallback(ctx, r, g); return; }
        const arch = g.archetype || g.attackType || "melee";
        const baseFn = Art["_heroArchetype_" + arch] || Art._heroArchetype_melee;
        baseFn(ctx, r, g);
        Art._paintTraitFace(ctx, r, g, t);
        Art._paintTraitHat(ctx, r, g, t);
        Art._paintTraitBeard(ctx, r, g, t);
        Art._paintTraitWeapon(ctx, r, g, t);
        Art._paintTraitExtra(ctx, r, g, t);
        Art._heroArchetypeVariation(ctx, r, g);
    },

    _paintTraitFace(ctx, r, g, t) {
        const colors = {
            pale: "#f0d4ad",
            tan: "#d4a574",
            dark: "#5a3820",
            red: "#c43222",
            weathered: "#b89068"
        };
        const c = colors[t.face] || colors.pale;
        ctx.fillStyle = c;
        ctx.beginPath();
        ctx.arc(0, -r * 0.04, r * 0.46, 0, Math.PI * 2);
        ctx.fill();
        ctx.fillStyle = "#1a0a04";
        ctx.beginPath(); ctx.arc(-r * 0.17, -r * 0.04, 2, 0, Math.PI * 2); ctx.fill();
        ctx.beginPath(); ctx.arc(r * 0.17, -r * 0.04, 2, 0, Math.PI * 2); ctx.fill();
    },

    _paintTraitHat(ctx, r, g, t) {
        const a = g.accent || "#c9892b";
        const h = t.hat;
        if (h === "yellowTurban") {
            ctx.fillStyle = "#d4a020";
            ctx.beginPath();
            ctx.moveTo(-r * 0.38, -r * 0.72); ctx.lineTo(0, -r * 0.98); ctx.lineTo(r * 0.38, -r * 0.72);
            ctx.lineTo(r * 0.3, -r * 0.55); ctx.lineTo(-r * 0.3, -r * 0.55); ctx.fill();
        } else if (h === "helm") {
            ctx.fillStyle = "#8a9098";
            ctx.beginPath();
            ctx.moveTo(-r * 0.55, -r * 0.35); ctx.quadraticCurveTo(0, -r * 0.88, r * 0.55, -r * 0.35);
            ctx.lineTo(r * 0.48, -r * 0.5); ctx.lineTo(-r * 0.48, -r * 0.5); ctx.fill();
            ctx.fillStyle = a;
            ctx.fillRect(-r * 0.06, -r * 0.92, r * 0.12, r * 0.14);
        } else if (h === "crown") {
            ctx.fillStyle = a;
            ctx.fillRect(-r * 0.42, -r * 0.72, r * 0.84, r * 0.14);
            ctx.strokeStyle = a;
            ctx.lineWidth = 2;
            for (let i = -2; i <= 2; i++) {
                ctx.beginPath(); ctx.moveTo(i * r * 0.12, -r * 0.72); ctx.lineTo(i * r * 0.12, -r * 0.88); ctx.stroke();
            }
        } else if (h === "turban") {
            ctx.fillStyle = U.shade(g.color, 30);
            ctx.beginPath();
            ctx.moveTo(-r * 0.6, -r * 0.38); ctx.quadraticCurveTo(0, -r * 0.9, r * 0.6, -r * 0.38);
            ctx.fill();
        } else if (h === "official") {
            ctx.fillStyle = "#2a2a3a";
            ctx.beginPath();
            ctx.moveTo(-r * 0.55, -r * 0.32); ctx.quadraticCurveTo(0, -r * 0.78, r * 0.55, -r * 0.32);
            ctx.lineTo(r * 0.45, -r * 0.48); ctx.lineTo(-r * 0.45, -r * 0.48); ctx.fill();
            ctx.fillStyle = a;
            ctx.beginPath(); ctx.arc(0, -r * 0.55, r * 0.08, 0, Math.PI * 2); ctx.fill();
        } else if (h === "navy") {
            ctx.fillStyle = U.shade(g.color, 20);
            ctx.beginPath();
            ctx.moveTo(-r * 0.58, -r * 0.34); ctx.quadraticCurveTo(0, -r * 0.82, r * 0.58, -r * 0.34); ctx.fill();
            ctx.fillStyle = "#e8e8e8";
            ctx.fillRect(-r * 0.35, -r * 0.5, r * 0.7, r * 0.08);
        } else if (h === "hood") {
            ctx.fillStyle = U.shade(g.color, -20);
            ctx.beginPath();
            ctx.moveTo(-r * 0.65, -r * 0.2); ctx.quadraticCurveTo(0, -r * 0.95, r * 0.65, -r * 0.2);
            ctx.lineTo(r * 0.5, -r * 0.15); ctx.lineTo(-r * 0.5, -r * 0.15); ctx.fill();
        } else if (h === "warrior") {
            ctx.fillStyle = U.shade(g.color, 15);
            ctx.beginPath();
            ctx.moveTo(-r * 0.52, -r * 0.36); ctx.quadraticCurveTo(0, -r * 0.8, r * 0.52, -r * 0.36); ctx.fill();
        }
    },

    _paintTraitBeard(ctx, r, g, t) {
        const b = t.beard;
        if (!b || b === "none") return;
        ctx.fillStyle = "#1a1208";
        if (b === "long") {
            ctx.beginPath();
            ctx.moveTo(-r * 0.22, r * 0.08);
            ctx.bezierCurveTo(-r * 0.12, r * 0.58, r * 0.12, r * 0.58, r * 0.22, r * 0.08);
            ctx.fill();
        } else if (b === "tiger") {
            ctx.strokeStyle = "#1a1208"; ctx.lineWidth = 2;
            for (let i = -2; i <= 2; i++) {
                ctx.beginPath(); ctx.moveTo(0, r * 0.12); ctx.lineTo(i * r * 0.16, r * 0.52); ctx.stroke();
            }
        } else if (b === "goatee") {
            ctx.beginPath();
            ctx.moveTo(-r * 0.08, r * 0.15); ctx.quadraticCurveTo(0, r * 0.45, r * 0.08, r * 0.15);
            ctx.fill();
        } else if (b === "short") {
            ctx.beginPath();
            ctx.moveTo(-r * 0.15, r * 0.12); ctx.quadraticCurveTo(0, r * 0.32, r * 0.15, r * 0.12);
            ctx.fill();
        }
    },

    _paintTraitWeapon(ctx, r, g, t) {
        const w = t.weapon;
        if (!w || w === "none") return;
        ctx.strokeStyle = U.shade(g.accent || "#aaa", -10);
        ctx.lineWidth = 2;
        if (w === "fan") {
            ctx.fillStyle = g.accent || "#e8dcc0";
            ctx.beginPath(); ctx.ellipse(r * 0.62, -r * 0.05, r * 0.18, r * 0.28, -0.4, 0, Math.PI * 2); ctx.fill();
            ctx.beginPath(); ctx.moveTo(r * 0.35, r * 0.05); ctx.lineTo(r * 0.55, -r * 0.2); ctx.stroke();
        } else if (w === "halberd") {
            ctx.beginPath(); ctx.moveTo(r * 0.5, r * 0.05); ctx.lineTo(r * 0.92, -r * 0.5); ctx.stroke();
            ctx.beginPath(); ctx.moveTo(r * 0.78, -r * 0.35); ctx.lineTo(r * 0.95, -r * 0.35); ctx.stroke();
        } else if (w === "spear") {
            ctx.beginPath(); ctx.moveTo(r * 0.48, r * 0.02); ctx.lineTo(r * 0.95, -r * 0.25); ctx.stroke();
        } else if (w === "saber") {
            ctx.beginPath(); ctx.moveTo(r * 0.52, r * 0.08); ctx.lineTo(r * 0.88, -r * 0.3); ctx.stroke();
        } else if (w === "bow") {
            ctx.strokeStyle = "#8a5a28";
            ctx.beginPath(); ctx.arc(r * 0.7, -r * 0.05, r * 0.22, -1.2, 1.2); ctx.stroke();
        } else if (w === "crossbow") {
            ctx.strokeStyle = "#666";
            ctx.fillStyle = "#555";
            ctx.fillRect(r * 0.55, -r * 0.08, r * 0.28, r * 0.06);
        }
    },

    _paintTraitExtra(ctx, r, g, t) {
        const e = t.extra;
        if (!e || e === "none") return;
        if (e === "flame") {
            ctx.fillStyle = "rgba(255,100,30,.55)";
            ctx.beginPath(); ctx.moveTo(0, -r * 0.85); ctx.lineTo(-r * 0.1, -r * 0.65); ctx.lineTo(r * 0.1, -r * 0.65); ctx.fill();
        } else if (e === "water") {
            ctx.fillStyle = "rgba(80,160,255,.45)";
            ctx.beginPath(); ctx.ellipse(0, r * 0.5, r * 0.55, r * 0.1, 0, 0, Math.PI * 2); ctx.fill();
        } else if (e === "scar") {
            ctx.strokeStyle = "rgba(80,20,10,.7)"; ctx.lineWidth = 2;
            ctx.beginPath(); ctx.moveTo(-r * 0.25, -r * 0.15); ctx.lineTo(-r * 0.05, r * 0.05); ctx.stroke();
        } else if (e === "blind") {
            ctx.fillStyle = "rgba(0,0,0,.55)";
            ctx.fillRect(-r * 0.28, -r * 0.12, r * 0.22, r * 0.08);
        } else if (e === "mask") {
            ctx.fillStyle = "rgba(30,30,40,.5)";
            ctx.fillRect(-r * 0.2, r * 0.05, r * 0.4, r * 0.12);
        }
    },

    drawPortraitSilhouette(ctx, x, y, r, general) {
        ctx.save();
        ctx.translate(x, y);
        ctx.beginPath();
        ctx.arc(0, 0, r, 0, Math.PI * 2);
        ctx.clip();
        ctx.globalAlpha = 0.32;
        const ghost = Object.assign({}, general, {
            color: general.color || "#3a3a3a",
            accent: "#5a5a5a"
        });
        Art._drawHero(ctx, r, ghost);
        ctx.globalAlpha = 1;
        ctx.fillStyle = "rgba(0,0,0,.48)";
        ctx.fillRect(-r, -r, r * 2, r * 2);
        ctx.strokeStyle = "#c9892b";
        ctx.lineWidth = Math.max(2, r * 0.1);
        ctx.beginPath();
        ctx.moveTo(-r * 0.22, -r * 0.12);
        ctx.lineTo(-r * 0.22, r * 0.18);
        ctx.arc(0, r * 0.18, r * 0.22, Math.PI, 0);
        ctx.lineTo(r * 0.22, -r * 0.12);
        ctx.stroke();
        ctx.lineWidth = Math.max(2, r * 0.08);
        ctx.strokeStyle = general.accent || "#8a6a3c";
        ctx.beginPath();
        ctx.arc(0, 0, r, 0, Math.PI * 2);
        ctx.stroke();
        ctx.restore();
    },

    _heroArchetypeBase(ctx, r, g) {
        ctx.fillStyle = U.shade(g.color || "#4a3219", -10);
        ctx.beginPath();
        ctx.moveTo(-r, r); ctx.lineTo(r, r);
        ctx.lineTo(r * 0.72, r * 0.22); ctx.lineTo(0, r * 0.42);
        ctx.lineTo(-r * 0.72, r * 0.22); ctx.closePath(); ctx.fill();
        ctx.fillStyle = "#f0d4ad";
        ctx.beginPath(); ctx.arc(0, -r * 0.04, r * 0.48, 0, Math.PI * 2); ctx.fill();
        ctx.fillStyle = "#1a0a04";
        ctx.beginPath(); ctx.arc(-r * 0.17, -r * 0.04, 2, 0, Math.PI * 2); ctx.fill();
        ctx.beginPath(); ctx.arc(r * 0.17, -r * 0.04, 2, 0, Math.PI * 2); ctx.fill();
    },

    _heroArchetypeVariation(ctx, r, g) {
        const seed = Art._idSeed(g.id);
        const beard = seed % 3;
        if (beard > 0) {
            ctx.fillStyle = "#1a1208";
            const len = beard === 2 ? r * 0.55 : r * 0.35;
            ctx.beginPath();
            ctx.moveTo(-r * 0.2, r * 0.12);
            ctx.quadraticCurveTo(0, len, r * 0.2, r * 0.12);
            ctx.fill();
        }
        if (seed % 2 === 0) {
            ctx.fillStyle = U.shade(g.accent || "#c9892b", seed % 4 === 0 ? 20 : -15);
            ctx.fillRect(-r * 0.5, -r * 0.72, r, r * 0.12);
        }
    },

    _heroArchetype_melee(ctx, r, g) {
        Art._heroArchetypeBase(ctx, r, g);
        ctx.fillStyle = U.shade(g.color, 20);
        ctx.beginPath();
        ctx.moveTo(-r * 0.55, -r * 0.38); ctx.quadraticCurveTo(0, -r * 0.82, r * 0.55, -r * 0.38);
        ctx.lineTo(r * 0.45, -r * 0.52); ctx.lineTo(-r * 0.45, -r * 0.52); ctx.fill();
        ctx.fillStyle = g.accent;
        ctx.fillRect(-r * 0.08, -r * 0.9, r * 0.16, r * 0.22);
        ctx.strokeStyle = "#888"; ctx.lineWidth = 2;
        ctx.beginPath(); ctx.moveTo(r * 0.55, r * 0.1); ctx.lineTo(r * 0.85, -r * 0.35); ctx.stroke();
        Art._heroArchetypeVariation(ctx, r, g);
    },

    _heroArchetype_magic(ctx, r, g) {
        Art._heroArchetypeBase(ctx, r, g);
        ctx.fillStyle = "#e8e0d0";
        ctx.beginPath();
        ctx.moveTo(-r * 0.62, -r * 0.32); ctx.quadraticCurveTo(0, -r * 0.92, r * 0.62, -r * 0.32);
        ctx.lineTo(r * 0.5, -r * 0.15); ctx.lineTo(-r * 0.5, -r * 0.15); ctx.fill();
        ctx.fillStyle = g.accent;
        ctx.beginPath(); ctx.arc(0, -r * 0.58, r * 0.1, 0, Math.PI * 2); ctx.fill();
        ctx.strokeStyle = U.shade(g.accent, -20); ctx.lineWidth = 1.5;
        ctx.beginPath(); ctx.moveTo(r * 0.35, r * 0.05); ctx.lineTo(r * 0.75, -r * 0.15); ctx.stroke();
        const seed = Art._idSeed(g.id);
        if (seed % 3 === 0) {
            ctx.fillStyle = "#d4a020";
            ctx.beginPath();
            ctx.moveTo(-r * 0.35, -r * 0.75); ctx.lineTo(0, -r * 0.95); ctx.lineTo(r * 0.35, -r * 0.75); ctx.fill();
        }
        Art._heroArchetypeVariation(ctx, r, g);
    },

    _heroArchetype_rapid(ctx, r, g) {
        Art._heroArchetypeBase(ctx, r, g);
        ctx.fillStyle = "#c8cfd6";
        ctx.beginPath();
        ctx.moveTo(-r * 0.58, -r * 0.35); ctx.quadraticCurveTo(0, -r * 0.88, r * 0.58, -r * 0.35);
        ctx.fill();
        ctx.fillStyle = "#d22a2a";
        ctx.fillRect(-r * 0.05, -r * 0.92, r * 0.1, r * 0.18);
        ctx.strokeStyle = "#aaa"; ctx.lineWidth = 2;
        ctx.beginPath(); ctx.moveTo(r * 0.5, r * 0.05); ctx.lineTo(r * 0.9, -r * 0.2); ctx.stroke();
        Art._heroArchetypeVariation(ctx, r, g);
    },

    _heroArchetype_pierce(ctx, r, g) {
        Art._heroArchetypeBase(ctx, r, g);
        ctx.fillStyle = g.accent;
        ctx.beginPath();
        ctx.moveTo(-r * 0.5, -r * 0.4); ctx.quadraticCurveTo(0, -r * 0.8, r * 0.5, -r * 0.4); ctx.fill();
        ctx.strokeStyle = "#ccc"; ctx.lineWidth = 2.5;
        ctx.beginPath(); ctx.moveTo(r * 0.45, -r * 0.1); ctx.lineTo(r * 0.95, -r * 0.55); ctx.stroke();
        ctx.beginPath(); ctx.moveTo(r * 0.88, -r * 0.58); ctx.lineTo(r * 0.88, -r * 0.42); ctx.stroke();
        Art._heroArchetypeVariation(ctx, r, g);
    },

    _heroArchetype_splash(ctx, r, g) {
        Art._heroArchetypeBase(ctx, r, g);
        ctx.fillStyle = g.accent;
        ctx.beginPath();
        ctx.moveTo(-r * 0.62, -r * 0.38); ctx.quadraticCurveTo(0, -r * 0.88, r * 0.62, -r * 0.38);
        ctx.lineTo(r * 0.5, -r * 0.55); ctx.quadraticCurveTo(0, -r * 0.98, -r * 0.5, -r * 0.55); ctx.fill();
        ctx.strokeStyle = "#1a0a04"; ctx.lineWidth = 2;
        for (let i = -1; i <= 1; i++) {
            ctx.beginPath(); ctx.moveTo(0, r * 0.15); ctx.lineTo(i * r * 0.2, r * 0.5); ctx.stroke();
        }
        Art._heroArchetypeVariation(ctx, r, g);
    },

    _heroArchetype_blaze(ctx, r, g) {
        Art._heroArchetypeBase(ctx, r, g);
        ctx.fillStyle = U.shade(g.color, 10);
        ctx.beginPath();
        ctx.moveTo(-r * 0.55, -r * 0.35); ctx.quadraticCurveTo(0, -r * 0.85, r * 0.55, -r * 0.35); ctx.fill();
        ctx.fillStyle = g.accent;
        ctx.beginPath(); ctx.moveTo(0, -r * 0.88); ctx.lineTo(-r * 0.12, -r * 0.65); ctx.lineTo(r * 0.12, -r * 0.65); ctx.fill();
        ctx.strokeStyle = "rgba(255,120,40,.7)"; ctx.lineWidth = 2;
        ctx.beginPath(); ctx.moveTo(-r * 0.3, r * 0.2); ctx.quadraticCurveTo(0, -r * 0.1, r * 0.3, r * 0.2); ctx.stroke();
        Art._heroArchetypeVariation(ctx, r, g);
    },

    _heroArchetype_charge(ctx, r, g) {
        Art._heroArchetypeBase(ctx, r, g);
        ctx.fillStyle = "#9aa6b0";
        ctx.beginPath(); ctx.arc(-r * 0.65, r * 0.45, r * 0.22, 0, Math.PI * 2); ctx.fill();
        ctx.beginPath(); ctx.arc(r * 0.65, r * 0.45, r * 0.22, 0, Math.PI * 2); ctx.fill();
        ctx.fillStyle = "#c8cfd6";
        ctx.beginPath();
        ctx.moveTo(-r * 0.55, -r * 0.35); ctx.quadraticCurveTo(0, -r * 0.82, r * 0.55, -r * 0.35); ctx.fill();
        ctx.strokeStyle = "#bbb"; ctx.lineWidth = 2;
        ctx.beginPath(); ctx.moveTo(r * 0.5, r * 0.05); ctx.lineTo(r * 1.0, r * 0.25); ctx.stroke();
        Art._heroArchetypeVariation(ctx, r, g);
    },

    _heroArchetype_flood(ctx, r, g) {
        Art._heroArchetypeBase(ctx, r, g);
        ctx.fillStyle = "#4a7ab8";
        ctx.beginPath();
        ctx.moveTo(-r * 0.55, -r * 0.32); ctx.quadraticCurveTo(0, -r * 0.8, r * 0.55, -r * 0.32); ctx.fill();
        ctx.fillStyle = "rgba(100,180,255,.55)";
        ctx.beginPath(); ctx.ellipse(0, r * 0.55, r * 0.7, r * 0.15, 0, 0, Math.PI * 2); ctx.fill();
        ctx.strokeStyle = g.accent; ctx.lineWidth = 1.5;
        ctx.beginPath(); ctx.moveTo(-r * 0.4, r * 0.35); ctx.quadraticCurveTo(0, r * 0.2, r * 0.4, r * 0.35); ctx.stroke();
        Art._heroArchetypeVariation(ctx, r, g);
    },

    _heroFallback(ctx, r, g) {
        Art._heroArchetype_melee(ctx, r, g);
    },

    _hero_guanyu(ctx, r, g) {
        // 关羽：红脸长须，绿袍金冠
        // 衣领
        ctx.fillStyle = "#1c5a2a";
        ctx.beginPath();
        ctx.moveTo(-r, r); ctx.lineTo(r, r);
        ctx.lineTo(r * 0.7, r * 0.2); ctx.lineTo(0, r * 0.4);
        ctx.lineTo(-r * 0.7, r * 0.2); ctx.closePath(); ctx.fill();
        // 长须
        ctx.fillStyle = "#1a1208";
        ctx.beginPath();
        ctx.moveTo(-r * 0.5, r * 0.05);
        ctx.bezierCurveTo(-r * 0.3, r * 0.55, r * 0.3, r * 0.55, r * 0.5, r * 0.05);
        ctx.bezierCurveTo(r * 0.3, r * 0.6, -r * 0.3, r * 0.6, -r * 0.5, r * 0.05);
        ctx.fill();
        // 红脸
        ctx.fillStyle = "#c43222";
        ctx.beginPath(); ctx.arc(0, -r * 0.05, r * 0.5, 0, Math.PI * 2); ctx.fill();
        // 卧蚕眉
        ctx.strokeStyle = "#1a0a04"; ctx.lineWidth = 2;
        ctx.beginPath(); ctx.moveTo(-r * 0.35, -r * 0.18); ctx.quadraticCurveTo(-r * 0.18, -r * 0.32, 0, -r * 0.18); ctx.stroke();
        ctx.beginPath(); ctx.moveTo(0, -r * 0.18); ctx.quadraticCurveTo(r * 0.18, -r * 0.32, r * 0.35, -r * 0.18); ctx.stroke();
        // 眼
        ctx.fillStyle = "#1a0a04";
        ctx.beginPath(); ctx.ellipse(-r * 0.18, -r * 0.05, 2, 1, 0, 0, Math.PI * 2); ctx.fill();
        ctx.beginPath(); ctx.ellipse(r * 0.18, -r * 0.05, 2, 1, 0, 0, Math.PI * 2); ctx.fill();
        // 金冠
        ctx.fillStyle = g.accent;
        ctx.fillRect(-r * 0.45, -r * 0.7, r * 0.9, r * 0.15);
        ctx.beginPath(); ctx.moveTo(-r * 0.45, -r * 0.55); ctx.lineTo(r * 0.45, -r * 0.55); ctx.lineTo(r * 0.3, -r * 0.4); ctx.lineTo(-r * 0.3, -r * 0.4); ctx.fill();
    },

    _hero_zhangfei(ctx, r, g) {
        // 张飞：黑面虎须，绿袍燕领
        ctx.fillStyle = "#193b1f";
        ctx.beginPath();
        ctx.moveTo(-r, r); ctx.lineTo(r, r);
        ctx.lineTo(r * 0.7, r * 0.2); ctx.lineTo(0, r * 0.45);
        ctx.lineTo(-r * 0.7, r * 0.2); ctx.closePath(); ctx.fill();
        // 黑脸
        ctx.fillStyle = "#3a2410";
        ctx.beginPath(); ctx.arc(0, -r * 0.05, r * 0.55, 0, Math.PI * 2); ctx.fill();
        // 怒目
        ctx.fillStyle = "#fff";
        ctx.beginPath(); ctx.arc(-r * 0.2, -r * 0.05, r * 0.12, 0, Math.PI * 2); ctx.fill();
        ctx.beginPath(); ctx.arc(r * 0.2, -r * 0.05, r * 0.12, 0, Math.PI * 2); ctx.fill();
        ctx.fillStyle = "#1a0a04";
        ctx.beginPath(); ctx.arc(-r * 0.18, -r * 0.05, r * 0.06, 0, Math.PI * 2); ctx.fill();
        ctx.beginPath(); ctx.arc(r * 0.18, -r * 0.05, r * 0.06, 0, Math.PI * 2); ctx.fill();
        // 虎须
        ctx.strokeStyle = "#1a0a04"; ctx.lineWidth = 2;
        for (let i = -2; i <= 2; i++) {
            ctx.beginPath();
            ctx.moveTo(0, r * 0.2);
            ctx.lineTo(i * r * 0.18, r * 0.55);
            ctx.stroke();
        }
        // 头巾
        ctx.fillStyle = g.accent;
        ctx.beginPath();
        ctx.moveTo(-r * 0.6, -r * 0.4); ctx.quadraticCurveTo(0, -r * 0.85, r * 0.6, -r * 0.4);
        ctx.lineTo(r * 0.6, -r * 0.55); ctx.quadraticCurveTo(0, -r * 0.95, -r * 0.6, -r * 0.55);
        ctx.fill();
    },

    _hero_zhaoyun(ctx, r, g) {
        // 赵云：白袍银甲俊朗
        ctx.fillStyle = "#d2dde0";
        ctx.beginPath();
        ctx.moveTo(-r, r); ctx.lineTo(r, r);
        ctx.lineTo(r * 0.7, r * 0.2); ctx.lineTo(0, r * 0.4);
        ctx.lineTo(-r * 0.7, r * 0.2); ctx.closePath(); ctx.fill();
        // 银甲护肩
        ctx.fillStyle = "#9aa6b0";
        ctx.beginPath(); ctx.arc(-r * 0.7, r * 0.5, r * 0.3, 0, Math.PI * 2); ctx.fill();
        ctx.beginPath(); ctx.arc(r * 0.7, r * 0.5, r * 0.3, 0, Math.PI * 2); ctx.fill();
        // 脸
        ctx.fillStyle = "#f3d6ad";
        ctx.beginPath(); ctx.arc(0, -r * 0.05, r * 0.5, 0, Math.PI * 2); ctx.fill();
        // 眉
        ctx.strokeStyle = "#1a0a04"; ctx.lineWidth = 1.5;
        ctx.beginPath(); ctx.moveTo(-r * 0.32, -r * 0.18); ctx.lineTo(-r * 0.08, -r * 0.22); ctx.stroke();
        ctx.beginPath(); ctx.moveTo(r * 0.08, -r * 0.22); ctx.lineTo(r * 0.32, -r * 0.18); ctx.stroke();
        // 眼
        ctx.fillStyle = "#1a0a04";
        ctx.beginPath(); ctx.arc(-r * 0.18, -r * 0.05, 2, 0, Math.PI * 2); ctx.fill();
        ctx.beginPath(); ctx.arc(r * 0.18, -r * 0.05, 2, 0, Math.PI * 2); ctx.fill();
        // 嘴
        ctx.beginPath(); ctx.moveTo(-r * 0.06, r * 0.18); ctx.lineTo(r * 0.06, r * 0.18); ctx.strokeStyle = "#7a3a2a"; ctx.stroke();
        // 银盔
        ctx.fillStyle = "#c8cfd6";
        ctx.beginPath();
        ctx.moveTo(-r * 0.55, -r * 0.35); ctx.quadraticCurveTo(0, -r * 0.85, r * 0.55, -r * 0.35);
        ctx.lineTo(r * 0.55, -r * 0.5); ctx.quadraticCurveTo(0, -r * 0.95, -r * 0.55, -r * 0.5);
        ctx.fill();
        // 红缨
        ctx.fillStyle = "#d22a2a";
        ctx.fillRect(-r * 0.06, -r * 0.95, r * 0.12, r * 0.2);
    },

    _hero_zhugeliang(ctx, r, g) {
        // 诸葛亮：羽扇纶巾
        ctx.fillStyle = "#5a6f8a";
        ctx.beginPath();
        ctx.moveTo(-r, r); ctx.lineTo(r, r);
        ctx.lineTo(r * 0.7, r * 0.2); ctx.lineTo(0, r * 0.45);
        ctx.lineTo(-r * 0.7, r * 0.2); ctx.closePath(); ctx.fill();
        // 脸
        ctx.fillStyle = "#f0d4ad";
        ctx.beginPath(); ctx.arc(0, -r * 0.02, r * 0.48, 0, Math.PI * 2); ctx.fill();
        // 长髯
        ctx.fillStyle = "#1a0a04";
        ctx.beginPath();
        ctx.moveTo(-r * 0.18, r * 0.15);
        ctx.bezierCurveTo(-r * 0.05, r * 0.65, r * 0.05, r * 0.65, r * 0.18, r * 0.15);
        ctx.fill();
        // 眼（智者半眯）
        ctx.strokeStyle = "#1a0a04"; ctx.lineWidth = 1.5;
        ctx.beginPath(); ctx.moveTo(-r * 0.28, -r * 0.05); ctx.lineTo(-r * 0.08, -r * 0.05); ctx.stroke();
        ctx.beginPath(); ctx.moveTo(r * 0.08, -r * 0.05); ctx.lineTo(r * 0.28, -r * 0.05); ctx.stroke();
        // 纶巾
        ctx.fillStyle = "#e6e6e6";
        ctx.beginPath();
        ctx.moveTo(-r * 0.65, -r * 0.35); ctx.quadraticCurveTo(0, -r * 0.95, r * 0.65, -r * 0.35);
        ctx.lineTo(r * 0.55, -r * 0.2); ctx.lineTo(-r * 0.55, -r * 0.2); ctx.fill();
        // 八卦点缀
        ctx.fillStyle = g.accent;
        ctx.beginPath(); ctx.arc(0, -r * 0.55, r * 0.12, 0, Math.PI * 2); ctx.fill();
        ctx.fillStyle = "#222";
        ctx.beginPath(); ctx.arc(0, -r * 0.55, r * 0.06, 0, Math.PI * 2); ctx.fill();
    },

    _hero_lvbu(ctx, r, g) {
        // 吕布：金甲飞将
        ctx.fillStyle = U.shade(g.color, -10);
        ctx.beginPath();
        ctx.moveTo(-r, r); ctx.lineTo(r, r);
        ctx.lineTo(r * 0.7, r * 0.2); ctx.lineTo(0, r * 0.4);
        ctx.lineTo(-r * 0.7, r * 0.2); ctx.closePath(); ctx.fill();
        // 金甲
        ctx.fillStyle = g.accent;
        ctx.fillRect(-r * 0.55, r * 0.25, r * 1.1, r * 0.18);
        // 脸
        ctx.fillStyle = "#f0d4ad";
        ctx.beginPath(); ctx.arc(0, -r * 0.05, r * 0.5, 0, Math.PI * 2); ctx.fill();
        // 桀骜剑眉
        ctx.strokeStyle = "#1a0a04"; ctx.lineWidth = 2.5;
        ctx.beginPath(); ctx.moveTo(-r * 0.34, -r * 0.22); ctx.lineTo(-r * 0.04, -r * 0.12); ctx.stroke();
        ctx.beginPath(); ctx.moveTo(r * 0.04, -r * 0.12); ctx.lineTo(r * 0.34, -r * 0.22); ctx.stroke();
        // 利眼
        ctx.fillStyle = "#1a0a04";
        ctx.beginPath(); ctx.ellipse(-r * 0.18, -r * 0.02, 3, 2, 0, 0, Math.PI * 2); ctx.fill();
        ctx.beginPath(); ctx.ellipse(r * 0.18, -r * 0.02, 3, 2, 0, 0, Math.PI * 2); ctx.fill();
        // 髭
        ctx.strokeStyle = "#1a0a04"; ctx.lineWidth = 2;
        ctx.beginPath(); ctx.moveTo(-r * 0.18, r * 0.18); ctx.quadraticCurveTo(-r * 0.4, r * 0.05, -r * 0.42, r * 0.25); ctx.stroke();
        ctx.beginPath(); ctx.moveTo(r * 0.18, r * 0.18); ctx.quadraticCurveTo(r * 0.4, r * 0.05, r * 0.42, r * 0.25); ctx.stroke();
        // 翎冠
        ctx.fillStyle = g.accent;
        ctx.beginPath();
        ctx.moveTo(-r * 0.55, -r * 0.35); ctx.quadraticCurveTo(0, -r * 0.85, r * 0.55, -r * 0.35);
        ctx.fill();
        // 双翎
        ctx.strokeStyle = "#fff"; ctx.lineWidth = 3;
        ctx.beginPath(); ctx.moveTo(-r * 0.2, -r * 0.7); ctx.lineTo(-r * 0.5, -r * 1); ctx.stroke();
        ctx.beginPath(); ctx.moveTo(r * 0.2, -r * 0.7); ctx.lineTo(r * 0.5, -r * 1); ctx.stroke();
    },

    _hero_zhouyu(ctx, r, g) {
        // 周瑜：青年俊雅吴将
        ctx.fillStyle = U.shade(g.color, -10);
        ctx.beginPath();
        ctx.moveTo(-r, r); ctx.lineTo(r, r);
        ctx.lineTo(r * 0.7, r * 0.2); ctx.lineTo(0, r * 0.4);
        ctx.lineTo(-r * 0.7, r * 0.2); ctx.closePath(); ctx.fill();
        // 脸
        ctx.fillStyle = "#f3d6ad";
        ctx.beginPath(); ctx.arc(0, -r * 0.02, r * 0.5, 0, Math.PI * 2); ctx.fill();
        // 俊眉
        ctx.strokeStyle = "#1a0a04"; ctx.lineWidth = 1.5;
        ctx.beginPath(); ctx.moveTo(-r * 0.32, -r * 0.18); ctx.quadraticCurveTo(-r * 0.18, -r * 0.28, -r * 0.04, -r * 0.18); ctx.stroke();
        ctx.beginPath(); ctx.moveTo(r * 0.04, -r * 0.18); ctx.quadraticCurveTo(r * 0.18, -r * 0.28, r * 0.32, -r * 0.18); ctx.stroke();
        // 眼
        ctx.fillStyle = "#1a0a04";
        ctx.beginPath(); ctx.arc(-r * 0.18, -r * 0.02, 2, 0, Math.PI * 2); ctx.fill();
        ctx.beginPath(); ctx.arc(r * 0.18, -r * 0.02, 2, 0, Math.PI * 2); ctx.fill();
        // 笑唇
        ctx.strokeStyle = "#7a3a2a"; ctx.lineWidth = 1;
        ctx.beginPath(); ctx.moveTo(-r * 0.08, r * 0.18); ctx.quadraticCurveTo(0, r * 0.25, r * 0.08, r * 0.18); ctx.stroke();
        // 蓝盔
        ctx.fillStyle = U.shade(g.color, 30);
        ctx.beginPath();
        ctx.moveTo(-r * 0.6, -r * 0.35); ctx.quadraticCurveTo(0, -r * 0.85, r * 0.6, -r * 0.35);
        ctx.lineTo(r * 0.5, -r * 0.18); ctx.lineTo(-r * 0.5, -r * 0.18); ctx.fill();
        // 火焰红缨
        ctx.fillStyle = g.accent;
        ctx.beginPath();
        ctx.moveTo(0, -r * 0.85); ctx.lineTo(-r * 0.1, -r * 1.05); ctx.lineTo(r * 0.1, -r * 1.05); ctx.fill();
    },

    // ============ 敌人绘制 ============
    drawEnemy(ctx, e) {
        const t = e.type;
        const P = window.Projection;
        const iso = P && P.enabled;
        const sz = t.size;
        const sc = t.scale;

        // ===== 地面阴影层（2.5D 时画在地面原点，不随旋转） =====
        if (iso) {
            ctx.save();
            ctx.translate(e.x, e.y);
            P.drawShadow(ctx, sz * 0.7 * sc, sz * 0.25 * sc, P.SHADOW_ALPHA, 2);
            ctx.restore();
        }

        ctx.save();
        ctx.translate(e.x, e.y);
        ctx.rotate(e.angle + Math.PI / 2);

        // ===== 2.5D 高度抬升 =====
        if (iso) {
            const colH = P.heightFor("enemy", { weapon: t.weapon, size: sz });
            ctx.translate(0, P.heightOffset(colH, 1));
        }

        // 原始影子（非 2.5D 模式保留；2.5D 时已在地面层画过）
        if (!iso) {
            ctx.fillStyle = "rgba(0,0,0,.35)";
            ctx.beginPath(); ctx.ellipse(0, sz * 0.6 * sc, sz * 0.7 * sc, sz * 0.25 * sc, 0, 0, Math.PI * 2); ctx.fill();
        }

        if (t.weapon === "siege") {
            Art._drawSiege(ctx, t, sc);
        } else if (t.weapon === "baggage") {
            Art._drawBaggage(ctx, t, sc);
        } else if (t.weapon === "boss") {
            const bossId = (typeof Game !== "undefined" && Game.level && Game.level.bossGeneralId) || null;
            const img = bossId && window.ArtAssets ? ArtAssets.getPortrait(bossId) : null;
            const g = bossId && window.getGeneral ? getGeneral(bossId) : (bossId ? { id: bossId } : null);
            const pr = t.size * sc * 0.92;
            if (img && g) {
                ctx.beginPath();
                ctx.arc(0, 0, pr, 0, Math.PI * 2);
                ctx.save();
                ctx.clip();
                Art._drawPortraitImageFace(ctx, img, pr, g);
                ctx.restore();
                ctx.strokeStyle = "rgba(180, 30, 30, 0.9)";
                ctx.lineWidth = 3;
                ctx.beginPath();
                ctx.arc(0, 0, pr, 0, Math.PI * 2);
                ctx.stroke();
                ctx.strokeStyle = "rgba(247, 215, 116, 0.7)";
                ctx.lineWidth = 1.5;
                ctx.beginPath();
                ctx.arc(0, 0, pr + 5, 0, Math.PI * 2);
                ctx.stroke();
            } else {
                Art._drawBoss(ctx, t, sc * 1.12);
                ctx.strokeStyle = "rgba(247, 215, 116, 0.55)";
                ctx.lineWidth = 2.5;
                ctx.beginPath();
                ctx.ellipse(0, 0, t.size * 1.1 * sc, t.size * 0.95 * sc, 0, 0, Math.PI * 2);
                ctx.stroke();
            }
        } else {
            Art._drawSoldier(ctx, t, sc);
        }

        ctx.restore();

        // 血条
        if (e.hp < e.maxHp) {
            const w = sz * 2.0;
            const x = e.x - w / 2, y = e.y - sz - 8;
            ctx.fillStyle = "rgba(0,0,0,.6)"; ctx.fillRect(x - 1, y - 1, w + 2, 5);
            ctx.fillStyle = "#aa2a2a"; ctx.fillRect(x, y, w, 3);
            ctx.fillStyle = "#6dd07a"; ctx.fillRect(x, y, w * (e.hp / e.maxHp), 3);
        }

        // 减速光环
        if (e.slowUntil > performance.now()) {
            ctx.strokeStyle = "rgba(122,216,255,.7)";
            ctx.lineWidth = 2;
            ctx.beginPath(); ctx.arc(e.x, e.y, sz + 4, 0, Math.PI * 2); ctx.stroke();
        }
        // 眩晕星
        if (e.stunUntil > performance.now()) {
            ctx.fillStyle = "#ffe468";
            ctx.font = "14px serif";
            ctx.textAlign = "center";
            ctx.fillText("✦ ✦ ✦", e.x, e.y - sz - 12);
        }
    },

    _drawSoldier(ctx, t, sc) {
        const sz = t.size;
        const col = U.shade(t.color, -20);
        const colHi = U.shade(t.color, 15);
        // 躯干（俯视人形）
        ctx.fillStyle = col;
        ctx.beginPath();
        ctx.ellipse(0, sz * 0.08 * sc, sz * 0.38 * sc, sz * 0.48 * sc, 0, 0, Math.PI * 2);
        ctx.fill();
        // 肩甲
        ctx.fillStyle = colHi;
        ctx.beginPath();
        ctx.ellipse(-sz * 0.34 * sc, sz * 0.02 * sc, sz * 0.18 * sc, sz * 0.14 * sc, 0, 0, Math.PI * 2);
        ctx.fill();
        ctx.beginPath();
        ctx.ellipse(sz * 0.34 * sc, sz * 0.02 * sc, sz * 0.18 * sc, sz * 0.14 * sc, 0, 0, Math.PI * 2);
        ctx.fill();
        // 头
        ctx.fillStyle = "#f3d6ad";
        ctx.beginPath();
        ctx.arc(0, -sz * 0.38 * sc, sz * 0.26 * sc, 0, Math.PI * 2);
        ctx.fill();
        // 头盔
        ctx.fillStyle = U.shade(t.color, 25);
        ctx.beginPath();
        ctx.arc(0, -sz * 0.42 * sc, sz * 0.3 * sc, Math.PI * 1.08, Math.PI * 1.92);
        ctx.fill();
        // 武器（略）
        ctx.strokeStyle = "#b8a888";
        ctx.lineWidth = 1.8;
        if (t.weapon === "spear" || t.weapon === "lance") {
            ctx.beginPath();
            ctx.moveTo(sz * 0.6 * sc, sz * 0.0 * sc);
            ctx.lineTo(sz * 0.6 * sc, -sz * 1.1 * sc);
            ctx.stroke();
            ctx.fillStyle = "#dddddd";
            ctx.beginPath();
            ctx.moveTo(sz * 0.6 * sc, -sz * 1.15 * sc);
            ctx.lineTo(sz * 0.5 * sc, -sz * 1.0 * sc);
            ctx.lineTo(sz * 0.7 * sc, -sz * 1.0 * sc);
            ctx.fill();
        } else if (t.weapon === "shield") {
            ctx.fillStyle = U.shade(t.color, 50);
            ctx.beginPath();
            ctx.ellipse(-sz * 0.7 * sc, sz * 0.05 * sc, sz * 0.32 * sc, sz * 0.5 * sc, 0, 0, Math.PI * 2);
            ctx.fill();
            ctx.fillStyle = "#cccccc";
            ctx.beginPath(); ctx.arc(-sz * 0.7 * sc, sz * 0.05 * sc, sz * 0.1 * sc, 0, Math.PI * 2); ctx.fill();
        } else if (t.weapon === "halberd") {
            ctx.strokeStyle = "#c0c0c0"; ctx.lineWidth = 2;
            ctx.beginPath();
            ctx.moveTo(sz * 0.55 * sc, sz * 0.05 * sc);
            ctx.lineTo(sz * 0.55 * sc, -sz * 1.15 * sc);
            ctx.stroke();
            ctx.beginPath();
            ctx.moveTo(sz * 0.55 * sc, -sz * 0.55 * sc);
            ctx.lineTo(sz * 0.95 * sc, -sz * 0.45 * sc);
            ctx.stroke();
            ctx.fillStyle = "#dddddd";
            ctx.beginPath();
            ctx.moveTo(sz * 0.55 * sc, -sz * 1.2 * sc);
            ctx.lineTo(sz * 0.45 * sc, -sz * 1.0 * sc);
            ctx.lineTo(sz * 0.65 * sc, -sz * 1.0 * sc);
            ctx.fill();
        } else if (t.weapon === "fangji_halberd") {
            ctx.strokeStyle = "#e8d89a"; ctx.lineWidth = 3;
            ctx.beginPath();
            ctx.moveTo(sz * 0.58 * sc, sz * 0.08 * sc);
            ctx.lineTo(sz * 0.58 * sc, -sz * 1.35 * sc);
            ctx.stroke();
            ctx.beginPath();
            ctx.moveTo(sz * 0.58 * sc, -sz * 0.62 * sc);
            ctx.lineTo(sz * 1.05 * sc, -sz * 0.48 * sc);
            ctx.moveTo(sz * 0.58 * sc, -sz * 0.85 * sc);
            ctx.lineTo(sz * 0.95 * sc, -sz * 0.95 * sc);
            ctx.stroke();
            ctx.fillStyle = "#f7f0d0";
            ctx.beginPath();
            ctx.moveTo(sz * 0.58 * sc, -sz * 1.42 * sc);
            ctx.lineTo(sz * 0.42 * sc, -sz * 1.12 * sc);
            ctx.lineTo(sz * 0.74 * sc, -sz * 1.12 * sc);
            ctx.fill();
        } else if (t.weapon === "rocket_bow") {
            ctx.strokeStyle = "#6a5040"; ctx.lineWidth = 1.5;
            ctx.beginPath();
            ctx.arc(sz * 0.52 * sc, -sz * 0.32 * sc, sz * 0.42 * sc, -Math.PI * 0.65, Math.PI * 0.12);
            ctx.stroke();
            ctx.beginPath();
            ctx.moveTo(sz * 0.18 * sc, -sz * 0.32 * sc);
            ctx.lineTo(sz * 0.92 * sc, -sz * 0.32 * sc);
            ctx.stroke();
            ctx.fillStyle = "#ff6a2a";
            ctx.beginPath();
            ctx.moveTo(sz * 0.95 * sc, -sz * 0.32 * sc);
            ctx.lineTo(sz * 1.12 * sc, -sz * 0.38 * sc);
            ctx.lineTo(sz * 1.12 * sc, -sz * 0.26 * sc);
            ctx.closePath();
            ctx.fill();
        } else if (t.weapon === "medic_satchel") {
            ctx.fillStyle = "#5a4030";
            ctx.fillRect(sz * 0.15 * sc, sz * 0.05 * sc, sz * 0.38 * sc, sz * 0.32 * sc);
            ctx.strokeStyle = "#ddd";
            ctx.lineWidth = 2;
            ctx.beginPath();
            ctx.moveTo(sz * 0.34 * sc, sz * 0.12 * sc);
            ctx.lineTo(sz * 0.34 * sc, sz * 0.3 * sc);
            ctx.moveTo(sz * 0.24 * sc, sz * 0.21 * sc);
            ctx.lineTo(sz * 0.44 * sc, sz * 0.21 * sc);
            ctx.stroke();
        } else if (t.weapon === "desperado") {
            ctx.fillStyle = "#8a1a1a";
            ctx.fillRect(-sz * 0.45 * sc, sz * 0.12 * sc, sz * 0.9 * sc, sz * 0.12 * sc);
            ctx.strokeStyle = "#ddd"; ctx.lineWidth = 2;
            ctx.beginPath();
            ctx.moveTo(sz * 0.5 * sc, sz * 0.08 * sc);
            ctx.lineTo(sz * 0.85 * sc, -sz * 0.15 * sc);
            ctx.stroke();
        } else if (t.weapon === "saber") {
            ctx.strokeStyle = "#d8d8e8"; ctx.lineWidth = 2.5; ctx.lineCap = "round";
            ctx.beginPath();
            ctx.moveTo(sz * 0.45 * sc, sz * 0.15 * sc);
            ctx.quadraticCurveTo(sz * 1.0 * sc, -sz * 0.2 * sc, sz * 0.55 * sc, -sz * 0.95 * sc);
            ctx.stroke();
        } else if (t.weapon === "bow") {
            ctx.strokeStyle = "#8a6a4a"; ctx.lineWidth = 1.5;
            ctx.beginPath();
            ctx.arc(sz * 0.55 * sc, -sz * 0.35 * sc, sz * 0.45 * sc, -Math.PI * 0.65, Math.PI * 0.15);
            ctx.stroke();
            ctx.beginPath();
            ctx.moveTo(sz * 0.2 * sc, -sz * 0.35 * sc);
            ctx.lineTo(sz * 0.95 * sc, -sz * 0.35 * sc);
            ctx.stroke();
        } else if (t.weapon === "crossbow") {
            ctx.fillStyle = "#5a4a38";
            ctx.fillRect(sz * 0.35 * sc, -sz * 0.55 * sc, sz * 0.5 * sc, sz * 0.35 * sc);
            ctx.strokeStyle = "#aaa"; ctx.lineWidth = 1;
            ctx.beginPath();
            ctx.moveTo(sz * 0.85 * sc, -sz * 0.45 * sc);
            ctx.lineTo(sz * 1.05 * sc, -sz * 0.38 * sc);
            ctx.stroke();
        } else if (t.weapon === "tiger_lance") {
            ctx.strokeStyle = "#f7d774"; ctx.lineWidth = 3;
            ctx.beginPath();
            ctx.moveTo(sz * 0.65 * sc, sz * 0.02 * sc);
            ctx.lineTo(sz * 0.65 * sc, -sz * 1.15 * sc);
            ctx.stroke();
            ctx.fillStyle = "#eeeecc";
            ctx.beginPath();
            ctx.moveTo(sz * 0.65 * sc, -sz * 1.22 * sc);
            ctx.lineTo(sz * 0.52 * sc, -sz * 1.02 * sc);
            ctx.lineTo(sz * 0.78 * sc, -sz * 1.02 * sc);
            ctx.fill();
        } else if (t.weapon === "rattan") {
            ctx.strokeStyle = "#1a3018"; ctx.lineWidth = 2;
            for (let i = -2; i <= 2; i++) {
                ctx.beginPath();
                ctx.moveTo(-sz * 0.35 * sc + i * sz * 0.12 * sc, sz * 0.5 * sc);
                ctx.lineTo(-sz * 0.35 * sc + i * sz * 0.12 * sc, -sz * 0.55 * sc);
                ctx.stroke();
            }
            ctx.strokeStyle = "#cccccc"; ctx.lineWidth = 1.5;
            ctx.beginPath();
            ctx.moveTo(sz * 0.55 * sc, sz * 0.05 * sc);
            ctx.lineTo(sz * 0.55 * sc, -sz * 0.75 * sc);
            ctx.stroke();
        } else { // club
            ctx.fillStyle = "#5a3a1a";
            ctx.fillRect(sz * 0.5 * sc, -sz * 0.1 * sc, sz * 0.18 * sc, sz * 0.6 * sc);
        }

        if (t.plumeWhite) {
            ctx.fillStyle = "#f0f0f0";
            ctx.beginPath();
            ctx.moveTo(-sz * 0.08 * sc, -sz * 0.75 * sc);
            ctx.lineTo(0, -sz * 1.15 * sc);
            ctx.lineTo(sz * 0.08 * sc, -sz * 0.75 * sc);
            ctx.closePath();
            ctx.fill();
        }
    },

    _drawBaggage(ctx, t, sc) {
        const sz = t.size * 0.85;
        ctx.fillStyle = U.shade(t.color, -10);
        ctx.fillRect(-sz * 0.55, -sz * 0.25, sz * 1.1, sz * 0.65);
        ctx.fillStyle = "#3a2410";
        ctx.beginPath(); ctx.arc(-sz * 0.42, sz * 0.42, sz * 0.18, 0, Math.PI * 2); ctx.fill();
        ctx.beginPath(); ctx.arc(sz * 0.42, sz * 0.42, sz * 0.18, 0, Math.PI * 2); ctx.fill();
        ctx.fillStyle = "#f7d774";
        ctx.font = "bold 11px serif";
        ctx.textAlign = "center";
        ctx.fillText("粮", 0, sz * 0.12);
    },

    _drawSiege(ctx, t, sc) {
        const sz = t.size;
        // 车体
        ctx.fillStyle = U.shade(t.color, 0);
        ctx.fillRect(-sz * 0.6, -sz * 0.5, sz * 1.2, sz);
        ctx.fillStyle = U.shade(t.color, -30);
        ctx.fillRect(-sz * 0.6, -sz * 0.5, sz * 1.2, 6);
        // 木轮
        ctx.fillStyle = "#1a0a04";
        ctx.beginPath(); ctx.arc(-sz * 0.5, sz * 0.4, sz * 0.25, 0, Math.PI * 2); ctx.fill();
        ctx.beginPath(); ctx.arc(sz * 0.5, sz * 0.4, sz * 0.25, 0, Math.PI * 2); ctx.fill();
        // 撞角
        ctx.fillStyle = "#7a3a2a";
        ctx.beginPath();
        ctx.moveTo(0, -sz * 1.0);
        ctx.lineTo(-sz * 0.18, -sz * 0.5);
        ctx.lineTo(sz * 0.18, -sz * 0.5);
        ctx.fill();
        // 旗帜
        ctx.fillStyle = "#aa2a2a";
        ctx.fillRect(-sz * 0.05, -sz * 0.9, sz * 0.4, sz * 0.3);
    },

    _drawBoss(ctx, t, sc) {
        const sz = t.size;
        // 战马
        ctx.fillStyle = "#3a2410";
        ctx.beginPath();
        ctx.ellipse(0, sz * 0.4, sz * 0.9, sz * 0.45, 0, 0, Math.PI * 2);
        ctx.fill();
        ctx.fillStyle = "#1a0a04";
        ctx.fillRect(-sz * 0.7, sz * 0.6, sz * 0.15, sz * 0.4);
        ctx.fillRect(sz * 0.55, sz * 0.6, sz * 0.15, sz * 0.4);
        // 盔甲身
        ctx.fillStyle = U.shade(t.color, 0);
        ctx.beginPath();
        ctx.ellipse(0, -sz * 0.05, sz * 0.55, sz * 0.6, 0, 0, Math.PI * 2);
        ctx.fill();
        // 头
        ctx.fillStyle = "#f3d6ad";
        ctx.beginPath(); ctx.arc(0, -sz * 0.55, sz * 0.3, 0, Math.PI * 2); ctx.fill();
        // 凤翅头盔
        ctx.fillStyle = "#5a2a1a";
        ctx.beginPath();
        ctx.arc(0, -sz * 0.6, sz * 0.36, Math.PI * 1.05, Math.PI * 1.95);
        ctx.fill();
        ctx.fillStyle = "#aa2a2a";
        ctx.fillRect(-sz * 0.04, -sz * 1.1, sz * 0.08, sz * 0.3);
        // 长戟
        ctx.strokeStyle = "#cccccc"; ctx.lineWidth = 3;
        ctx.beginPath();
        ctx.moveTo(sz * 0.7, sz * 0.0);
        ctx.lineTo(sz * 0.7, -sz * 1.4);
        ctx.stroke();
        ctx.fillStyle = "#dddddd";
        ctx.beginPath();
        ctx.moveTo(sz * 0.7, -sz * 1.5);
        ctx.lineTo(sz * 0.55, -sz * 1.25);
        ctx.lineTo(sz * 0.85, -sz * 1.25);
        ctx.fill();
        // 凶眼
        ctx.fillStyle = "#aa0000";
        ctx.fillRect(-sz * 0.13, -sz * 0.55, sz * 0.06, sz * 0.06);
        ctx.fillRect(sz * 0.07, -sz * 0.55, sz * 0.06, sz * 0.06);
    },

    _drawMapAmbience(ctx, level, W, H) {
        const theme = level.mapTheme || "";
        const weather = level.weather || "";
        const terrain = level.terrain || "";

        if (theme.indexOf("snow") >= 0 || theme.indexOf("winter") >= 0 || weather === "snow") {
            ctx.fillStyle = "rgba(255,255,255,0.1)";
            for (let i = 0; i < 40; i++) {
                const x = (i * 131) % W;
                const y = (i * 47) % Math.floor(H * 0.5);
                ctx.beginPath();
                ctx.arc(x, y, 2 + (i % 3), 0, Math.PI * 2);
                ctx.fill();
            }
            ctx.fillStyle = "rgba(220,230,255,0.12)";
            ctx.fillRect(0, H * 0.72, W, H * 0.28);
            for (let i = 0; i < 5; i++) {
                const bx = 60 + i * (W / 5);
                ctx.fillStyle = "rgba(180,190,210,0.2)";
                ctx.beginPath();
                ctx.moveTo(bx, H * 0.78);
                ctx.lineTo(bx + 18, H * 0.55);
                ctx.lineTo(bx + 36, H * 0.78);
                ctx.closePath();
                ctx.fill();
            }
        }

        if (theme === "east_fire" || theme === "south_fire" || theme.indexOf("fire") >= 0) {
            const embers = theme === "east_fire" ? 32 : 24;
            for (let i = 0; i < embers; i++) {
                const x = (i * 89) % W;
                const y = H * 0.32 + (i * 53) % Math.floor(H * 0.4);
                const a = 0.14 + 0.1 * Math.sin(i * 1.7 + (theme === "east_fire" ? 0.5 : 0));
                ctx.fillStyle = `rgba(255,${theme === "south_fire" ? 60 : 100},30,${a})`;
                ctx.beginPath();
                ctx.arc(x, y, 4 + (i % 6), 0, Math.PI * 2);
                ctx.fill();
            }
            if (theme === "east_fire" || theme.indexOf("river") >= 0) {
                const wg = ctx.createLinearGradient(0, H * 0.55, 0, H);
                wg.addColorStop(0, "rgba(40,90,180,0)");
                wg.addColorStop(1, "rgba(60,120,200,0.28)");
                ctx.fillStyle = wg;
                ctx.fillRect(0, H * 0.5, W, H * 0.5);
                for (let i = 0; i < 8; i++) {
                    const rx = (i * 97) % W;
                    ctx.strokeStyle = `rgba(120,200,255,${0.15 + 0.08 * Math.sin(i)})`;
                    ctx.lineWidth = 2;
                    ctx.beginPath();
                    ctx.moveTo(rx, H * 0.62);
                    ctx.quadraticCurveTo(rx + 20, H * 0.58, rx + 40, H * 0.64);
                    ctx.stroke();
                }
            }
            if (theme === "east_fire") {
                ctx.fillStyle = "rgba(80,40,20,0.18)";
                for (let i = 0; i < 6; i++) {
                    ctx.fillRect((i * 71) % W, H * 0.2 + (i * 19) % 80, 24, 8);
                }
            }
        }

        if (weather === "rain" || terrain === "river" || theme.indexOf("river") >= 0
            || theme.indexOf("flood") >= 0 || theme.indexOf("coast") >= 0 || theme === "central_river") {
            const wg = ctx.createLinearGradient(0, H * 0.45, 0, H);
            wg.addColorStop(0, "rgba(30,80,160,0)");
            wg.addColorStop(1, "rgba(30,80,160,0.26)");
            ctx.fillStyle = wg;
            ctx.fillRect(0, H * 0.45, W, H * 0.55);
            for (let i = 0; i < 12; i++) {
                const fx = (i * 113) % W;
                ctx.strokeStyle = `rgba(100,180,240,${0.12 + 0.06 * Math.sin(i * 2)})`;
                ctx.lineWidth = 1.5;
                ctx.beginPath();
                ctx.moveTo(fx, H * 0.5);
                ctx.lineTo(fx + 30, H * 0.54);
                ctx.stroke();
            }
        }

        if (terrain === "city" || terrain === "mountain" || theme.indexOf("pass") >= 0) {
            ctx.fillStyle = "rgba(0,0,0,0.15)";
            for (let i = 0; i < 4; i++) {
                const bx = 40 + i * (W / 4);
                ctx.fillRect(bx, H * 0.12, 28 + (i % 2) * 12, H * 0.22);
            }
        }

        if (theme === "central_capital") {
            ctx.fillStyle = "rgba(247,215,116,0.06)";
            ctx.fillRect(W * 0.35, H * 0.08, W * 0.3, H * 0.2);
        }
    },

    _mapThemePalette(theme) {
        const t = theme || "central_plain";
        if (t.indexOf("north") === 0 || t.indexOf("west_snow") >= 0 || t === "central_winter") {
            return { top: "#4a4a52", bot: "#1a1820", bush: "rgba(120,120,140,.35)", grass: "rgba(200,210,220,.2)", path: ["#4a3a2a", "#6a5040", "#8a7058"] };
        }
        if (t.indexOf("south") === 0) {
            return { top: "#1a3020", bot: "#0a1408", bush: "rgba(30,80,40,.55)", grass: "rgba(50,120,60,.5)", path: ["#3a4a28", "#4a5a32", "#5a6a3a"] };
        }
        if (t.indexOf("east") === 0) {
            return { top: "#2a3a48", bot: "#101820", bush: "rgba(40,70,90,.45)", grass: "rgba(60,100,130,.35)", path: ["#4a4030", "#5a5040", "#6a6050"] };
        }
        if (t.indexOf("west") === 0) {
            return { top: "#4a3828", bot: "#1a1008", bush: "rgba(90,60,30,.5)", grass: "rgba(70,50,25,.4)", path: ["#5a4030", "#6a5040", "#7a6050"] };
        }
        return { top: "#3d2a14", bot: "#1a0f06", bush: "rgba(80,50,20,.5)", grass: "rgba(60,90,40,.45)", path: ["#6a4820", "#8a5c28", "#c09040"] };
    },

    // ============ 2.5D 视差背景 ============
    /**
     * 绘制 3 层视差背景：天空层 + 远景层 + 近景层。
     * 每层根据相机平移位置以不同速率滚动，产生空间纵深。
     * 程序绘制，覆盖全部 27 个 mapTheme，无需额外资产。
     */
    _drawParallaxLayers(ctx, level, W, H, pal) {
        const P = window.Projection;
        if (!P || !P.enabled) return; // 纯俯视时跳过
        const theme = level.mapTheme || "central_plain";
        const v = (window.Game && Game.view) || { panX: 0, panY: 0 };

        // === 天空层（视差 0.15，几乎不动） ===
        const skyOffX = -v.panX * 0.15;
        const skyOffY = -v.panY * 0.15 * P.Y_SQUASH;
        ctx.save();
        ctx.translate(skyOffX, skyOffY);
        this._drawSkyLayer(ctx, theme, W, H, pal);
        ctx.restore();

        // === 远景层（视差 0.4，慢速滚动） ===
        const farOffX = -v.panX * 0.4;
        const farOffY = -v.panY * 0.4 * P.Y_SQUASH;
        ctx.save();
        ctx.translate(farOffX, farOffY);
        this._drawFarLayer(ctx, theme, W, H, pal);
        ctx.restore();
    },

    /** 天空层：渐变天空 + 云 + 太阳/月亮 */
    _drawSkyLayer(ctx, theme, W, H, pal) {
        // 天空渐变（比地图底色更亮）
        const skyTop = U.shade(pal.top, 35);
        const skyBot = U.shade(pal.top, 10);
        const grd = ctx.createLinearGradient(0, 0, 0, H * 0.65);
        grd.addColorStop(0, skyTop);
        grd.addColorStop(1, skyBot);
        ctx.fillStyle = grd;
        ctx.fillRect(-50, -50, W + 100, H * 0.7);

        // 太阳/月亮光晕
        const sunX = W * 0.78, sunY = H * 0.18;
        const sunColor = theme.indexOf("snow") >= 0 || theme.indexOf("winter") >= 0
            ? "rgba(200,220,255,0.4)" : "rgba(255,230,160,0.35)";
        const sg = ctx.createRadialGradient(sunX, sunY, 0, sunX, sunY, 120);
        sg.addColorStop(0, sunColor);
        sg.addColorStop(1, "rgba(255,230,160,0)");
        ctx.fillStyle = sg;
        ctx.beginPath();
        ctx.arc(sunX, sunY, 120, 0, Math.PI * 2);
        ctx.fill();

        // 飘云（3 朵，随主题色调）
        const cloudColor = theme.indexOf("snow") >= 0 || theme.indexOf("winter") >= 0
            ? "rgba(220,225,240,0.25)" : "rgba(255,245,220,0.18)";
        for (let i = 0; i < 3; i++) {
            const cx = (i * 340 + 80) % (W + 100) - 50;
            const cy = H * (0.1 + i * 0.06);
            const cw = 100 + i * 30;
            ctx.fillStyle = cloudColor;
            for (let j = 0; j < 3; j++) {
                ctx.beginPath();
                ctx.arc(cx + j * cw * 0.3, cy, cw * 0.22 + j * 4, 0, Math.PI * 2);
                ctx.fill();
            }
        }
    },

    /** 远景层：山脉/城墙/树林轮廓（视差 0.4） */
    _drawFarLayer(ctx, theme, W, H, pal) {
        const horizonY = H * 0.5;
        const farColor = U.shade(pal.bot, 20);
        const farColor2 = U.shade(pal.bot, 35);

        // 远山轮廓（2 层，后层更淡）
        for (let layer = 0; layer < 2; layer++) {
            const baseY = horizonY - layer * 20;
            const amp = 50 + layer * 20;
            const col = layer === 0 ? farColor2 : farColor;
            ctx.fillStyle = col;
            ctx.beginPath();
            ctx.moveTo(-20, baseY + amp);
            for (let x = 0; x <= W + 20; x += 30) {
                const y = baseY - Math.sin(x * 0.008 + layer * 1.3) * amp * 0.5
                    - Math.sin(x * 0.02 + layer * 0.7) * amp * 0.3;
                ctx.lineTo(x, y);
            }
            ctx.lineTo(W + 20, baseY + amp);
            ctx.closePath();
            ctx.fill();
        }

        // 远景装饰（按主题）
        if (theme.indexOf("city") >= 0 || theme.indexOf("capital") >= 0 || theme === "central_capital") {
            // 城墙轮廓
            ctx.fillStyle = U.shade(pal.bot, 25);
            for (let i = 0; i < 8; i++) {
                const bx = (i * 130 + 40) % (W + 40) - 20;
                const bh = 30 + (i % 3) * 12;
                ctx.fillRect(bx, horizonY - bh, 24, bh + 20);
                // 城垛
                ctx.fillRect(bx + 2, horizonY - bh - 6, 6, 6);
                ctx.fillRect(bx + 14, horizonY - bh - 6, 6, 6);
            }
        } else if (theme.indexOf("forest") >= 0 || theme.indexOf("south") >= 0) {
            // 远景树林
            ctx.fillStyle = U.shade(pal.bush.replace("rgba(", "").replace(")", "").split(",").slice(0, 3).join("") || "#2a4a1a", 15);
            for (let i = 0; i < 12; i++) {
                const tx = (i * 85 + 30) % (W + 40) - 20;
                const th = 25 + (i % 4) * 8;
                ctx.beginPath();
                ctx.moveTo(tx, horizonY);
                ctx.lineTo(tx - 12, horizonY - th * 0.4);
                ctx.lineTo(tx - 6, horizonY - th);
                ctx.lineTo(tx + 6, horizonY - th);
                ctx.lineTo(tx + 12, horizonY - th * 0.4);
                ctx.closePath();
                ctx.fill();
            }
        }

        // 远景雾气（融合远近层）
        const fog = ctx.createLinearGradient(0, horizonY - 30, 0, horizonY + 30);
        fog.addColorStop(0, "rgba(0,0,0,0)");
        fog.addColorStop(0.5, U.shade(pal.top, 15).replace("rgb", "rgba").replace(")", ",0.15)"));
        fog.addColorStop(1, "rgba(0,0,0,0)");
        ctx.fillStyle = fog;
        ctx.fillRect(-20, horizonY - 30, W + 40, 60);
    },

    // ============ 路径地图 ============
    drawMap(ctx, level) {
        const W = (window.Viewport && Viewport.WORLD_W) || (window.Game && Game.WORLD_W) || 960;
        const H = (window.Viewport && Viewport.WORLD_H) || (window.Game && Game.WORLD_H) || 600;
        const pal = this._mapThemePalette(level.mapTheme);
        const mapBg = window.ArtAssets ? ArtAssets.getMapBg(level.mapTheme) : null;

        // 2.5D：先画视差远景层（在地图底图之前）
        this._drawParallaxLayers(ctx, level, W, H, pal);

        if (mapBg) {
            Art._coverImage(ctx, mapBg, W, H);
            ctx.fillStyle = "rgba(18, 12, 8, 0.18)";
            ctx.fillRect(0, 0, W, H);
            Art._drawMapVignette(ctx, W, H);
            this._drawMapAmbience(ctx, level, W, H);
        } else {
            const grd = ctx.createLinearGradient(0, 0, 0, H);
            grd.addColorStop(0, pal.top);
            grd.addColorStop(1, pal.bot);
            ctx.fillStyle = grd; ctx.fillRect(0, 0, W, H);

            this._drawMapAmbience(ctx, level, W, H);

            if (level.mapTheme && level.mapTheme.indexOf("east") >= 0) {
                for (let i = 0; i < 25; i++) {
                    const x = (i * 173) % W, y = H - 40 - (i * 41) % 120;
                    ctx.fillStyle = "rgba(40,80,120,.25)";
                    ctx.fillRect(x, y, 80 + (i % 3) * 40, 30);
                }
            }

            const dens = Art._decorDensity();
            const bushN = Math.floor(60 * dens);
            const grassN = Math.floor(40 * dens);
            for (let i = 0; i < bushN; i++) {
                const x = (i * 137) % W, y = (i * 73 + 37) % H;
                ctx.fillStyle = pal.bush;
                ctx.beginPath(); ctx.arc(x, y, 18 + (i % 5) * 4, 0, Math.PI * 2); ctx.fill();
            }
            for (let i = 0; i < grassN; i++) {
                const x = (i * 211 + 60) % W, y = (i * 97 + 17) % H;
                ctx.fillStyle = pal.grass;
                ctx.beginPath(); ctx.arc(x, y, 6 + (i % 3) * 2, 0, Math.PI * 2); ctx.fill();
            }
        }

        Art._strokeBattlePath(ctx, level, pal, !!mapBg);
        this._drawMapMarkers(ctx, level);
    },

    _drawMapVignette(ctx, W, H) {
        const g = ctx.createRadialGradient(W * 0.5, H * 0.45, Math.min(W, H) * 0.2, W * 0.5, H * 0.5, Math.max(W, H) * 0.72);
        g.addColorStop(0, "rgba(0,0,0,0)");
        g.addColorStop(1, "rgba(8, 4, 2, 0.38)");
        ctx.fillStyle = g;
        ctx.fillRect(0, 0, W, H);
    },

    _strokeBattlePath(ctx, level, pal, painted) {
        const pathPts = level.path;
        const trace = fn => {
            ctx.beginPath();
            for (let i = 0; i < pathPts.length; i++) {
                const p = pathPts[i];
                if (i === 0) ctx.moveTo(p.x, p.y);
                else ctx.lineTo(p.x, p.y);
            }
            fn();
        };
        ctx.lineCap = "round";
        ctx.lineJoin = "round";

        if (painted) {
            const earth = ["rgba(42, 28, 14, 0.72)", "rgba(88, 62, 32, 0.78)", "rgba(120, 88, 48, 0.82)"];
            trace(() => { ctx.strokeStyle = "rgba(0,0,0,0.38)"; ctx.lineWidth = 38; ctx.stroke(); });
            trace(() => { ctx.strokeStyle = earth[0]; ctx.lineWidth = 32; ctx.stroke(); });
            trace(() => { ctx.strokeStyle = earth[1]; ctx.lineWidth = 26; ctx.stroke(); });
            trace(() => { ctx.strokeStyle = earth[2]; ctx.lineWidth = 20; ctx.stroke(); });
            trace(() => { ctx.strokeStyle = "rgba(255, 228, 160, 0.35)"; ctx.lineWidth = 6; ctx.stroke(); });
            // 极淡车辙点，替代公路虚线
            ctx.fillStyle = "rgba(90, 68, 38, 0.22)";
            for (let i = 1; i < pathPts.length - 1; i += 2) {
                const p = pathPts[i];
                ctx.beginPath();
                ctx.ellipse(p.x, p.y, 3, 5, 0, 0, Math.PI * 2);
                ctx.fill();
            }
        } else {
            const strokePath = (w, col) => {
                trace(() => { ctx.strokeStyle = col; ctx.lineWidth = w; ctx.stroke(); });
            };
            strokePath(58, "rgba(0,0,0,.22)");
            strokePath(50, pal.path[0]);
            strokePath(42, pal.path[1]);
            strokePath(34, pal.path[2]);
            ctx.setLineDash([5, 10]);
            trace(() => {
                ctx.strokeStyle = "rgba(180,140,80,.2)";
                ctx.lineWidth = 1.5;
                ctx.stroke();
            });
            ctx.setLineDash([]);
        }
    },

    _drawMapMarkers(ctx, level) {
        const start = level.path[0];
        const end = level.path[level.path.length - 1];
        const theme = level.mapTheme || "";
        const isCapital = theme === "central_capital";

        ctx.save();
        // 敌营：旗墩 + 赤旗
        ctx.shadowColor = "rgba(200,40,40,0.4)";
        ctx.shadowBlur = 10;
        ctx.fillStyle = "#5a3820";
        ctx.beginPath();
        ctx.moveTo(start.x - 14, start.y + 18);
        ctx.lineTo(start.x + 14, start.y + 18);
        ctx.lineTo(start.x + 10, start.y - 6);
        ctx.lineTo(start.x - 10, start.y - 6);
        ctx.closePath();
        ctx.fill();
        ctx.fillStyle = "#8a1a1a";
        ctx.fillRect(start.x - 2, start.y - 38, 4, 34);
        ctx.fillStyle = "#c43222";
        ctx.beginPath();
        ctx.moveTo(start.x + 2, start.y - 36);
        ctx.lineTo(start.x + 28, start.y - 28);
        ctx.lineTo(start.x + 2, start.y - 18);
        ctx.closePath();
        ctx.fill();
        ctx.shadowBlur = 0;
        ctx.fillStyle = "#f7d774";
        ctx.font = "bold 14px serif";
        ctx.textAlign = "center";
        ctx.fillText("敌营", start.x, start.y + 14);

        // 皇城：城楼剪影
        const px = end.x - (isCapital ? 44 : 36);
        const py = end.y - (isCapital ? 38 : 32);
        const pw = isCapital ? 88 : 72;
        const ph = isCapital ? 68 : 58;
        ctx.shadowColor = "rgba(247,215,116,0.35)";
        ctx.shadowBlur = isCapital ? 18 : 12;
        ctx.fillStyle = "#6a4828";
        ctx.fillRect(px, py + ph * 0.35, pw, ph * 0.65);
        ctx.fillStyle = "#8a6030";
        for (let i = 0; i < (isCapital ? 5 : 3); i++) {
            const bx = px + 8 + i * (pw / (isCapital ? 5 : 3));
            ctx.fillRect(bx, py, 10, ph * 0.55);
            ctx.fillStyle = "#a87838";
            ctx.beginPath();
            ctx.moveTo(bx - 2, py);
            ctx.lineTo(bx + 5, py - 10);
            ctx.lineTo(bx + 12, py);
            ctx.fill();
            ctx.fillStyle = "#8a6030";
        }
        ctx.fillStyle = "#4a2913";
        ctx.fillRect(end.x - 12, end.y - 10, 24, 38);
        ctx.shadowBlur = 0;
        ctx.strokeStyle = "rgba(247,215,116,0.55)";
        ctx.lineWidth = isCapital ? 2.5 : 2;
        ctx.strokeRect(px, py, pw, ph);
        ctx.fillStyle = "#f7d774";
        ctx.font = isCapital ? "bold 14px serif" : "bold 13px serif";
        ctx.fillText(isCapital ? "帝都" : "皇城", end.x, end.y + 8);

        if (theme.indexOf("river") >= 0 || theme.indexOf("flood") >= 0 || theme === "east_fire") {
            const mid = level.path[Math.floor(level.path.length * 0.45)];
            if (mid) {
                ctx.fillStyle = "rgba(60,120,200,0.35)";
                ctx.fillRect(mid.x - 14, mid.y - 6, 28, 12);
                ctx.fillStyle = "#a8d4ff";
                ctx.font = "11px serif";
                ctx.fillText("渡", mid.x, mid.y + 4);
            }
        }
        ctx.restore();
    },

    // ============ 障碍物 ============
    drawObstacle(ctx, ob) {
        const P = window.Projection;
        const iso = P && P.enabled;
        ctx.save();
        ctx.translate(ob.x, ob.y);

        // 2.5D：地面阴影
        if (iso) P.drawShadow(ctx, 22, 8, P.SHADOW_ALPHA, 4);
        // 原始影子
        ctx.fillStyle = "rgba(0,0,0,.4)";
        ctx.beginPath(); ctx.ellipse(0, 16, 22, 7, 0, 0, Math.PI * 2); ctx.fill();

        // 2.5D 高度抬升
        if (iso) {
            const colH = P.heightFor("obstacle", { kind: ob.kind });
            ctx.translate(0, P.heightOffset(colH, 1));
        }

        if (ob.kind === "barrel") {
            // 粮车
            ctx.fillStyle = "#4a3018";
            ctx.fillRect(-20, -4, 40, 16);
            ctx.fillStyle = "#6a4828";
            ctx.beginPath();
            ctx.moveTo(-22, -4);
            ctx.lineTo(-16, -18);
            ctx.lineTo(16, -18);
            ctx.lineTo(22, -4);
            ctx.closePath();
            ctx.fill();
            ctx.fillStyle = "#3a2410";
            ctx.beginPath();
            ctx.arc(-14, 14, 5, 0, Math.PI * 2);
            ctx.arc(14, 14, 5, 0, Math.PI * 2);
            ctx.fill();
            ctx.fillStyle = "#f7d774";
            ctx.font = "bold 13px serif";
            ctx.textAlign = "center";
            ctx.fillText("粮", 0, 8);
        } else {
            ctx.fillStyle = "#7a7a7a";
            ctx.beginPath();
            ctx.moveTo(-22, 12); ctx.lineTo(-16, -16); ctx.lineTo(8, -22);
            ctx.lineTo(22, -8); ctx.lineTo(18, 14); ctx.closePath(); ctx.fill();
            ctx.fillStyle = "#9a9a9a";
            ctx.beginPath(); ctx.moveTo(-12, -10); ctx.lineTo(-4, -18); ctx.lineTo(2, -8); ctx.fill();
        }
        // 血条
        if (ob.hp < ob.maxHp) {
            ctx.fillStyle = "rgba(0,0,0,.6)"; ctx.fillRect(-16, -28, 32, 4);
            ctx.fillStyle = "#6dd07a"; ctx.fillRect(-15, -27, 30 * (ob.hp / ob.maxHp), 2);
        }
        ctx.restore();
    },

    _hexToRgb(hex) {
        if (!hex || hex[0] !== "#") return { r: 200, g: 200, b: 200 };
        const h = hex.slice(1);
        const n = parseInt(h.length === 3 ? h.split("").map(c => c + c).join("") : h, 16);
        return { r: (n >> 16) & 255, g: (n >> 8) & 255, b: n & 255 };
    },

    _drawProjectileTrail(ctx, p) {
        const tr = p._trail;
        if (!tr || tr.length < 2) return;
        const rgb = this._hexToRgb(p.color);
        ctx.save();
        ctx.lineCap = "round";
        ctx.lineJoin = "round";
        for (let i = 0; i < tr.length - 1; i++) {
            const a = tr[i], b = tr[i + 1];
            const prog = (i + 1) / tr.length;
            const alpha = prog * (p.kind === "fire" ? 0.5 : 0.42);
            ctx.globalAlpha = alpha;
            ctx.lineWidth = 1.5 + prog * 4;
            if (p.kind === "fire") {
                const g = (120 + prog * 80) | 0;
                ctx.strokeStyle = `rgba(255,${g},60,${Math.min(1, alpha * 1.2)})`;
            } else if (p.kind === "slash") {
                ctx.strokeStyle = `rgba(255,230,180,${alpha})`;
            } else {
                ctx.strokeStyle = `rgba(${rgb.r},${rgb.g},${rgb.b},${alpha + 0.15})`;
            }
            ctx.beginPath();
            ctx.moveTo(a.x, a.y);
            ctx.lineTo(b.x, b.y);
            ctx.stroke();
        }
        ctx.restore();
    },

    // ============ 投射物 ============
    drawProjectile(ctx, p) {
        this._drawProjectileTrail(ctx, p);

        const flick = Math.sin(p.x * 0.08 + p.y * 0.07 + performance.now() * 0.012);

        ctx.save();
        ctx.translate(p.x, p.y);

        // 2.5D：投射物飞行弧线高度（按飞行进度正弦起伏）
        const P = window.Projection;
        if (P && P.enabled) {
            // 用 _trail 长度估算飞行进度（越短 = 越接近发射点 = 越高）
            const tr = p._trail;
            const flightProg = tr && tr.length > 2 ? Math.min(1, tr.length / 12) : 0.5;
            const arcH = Math.sin(flightProg * Math.PI) * 18; // 0→18→0 弧线
            ctx.translate(0, P.heightOffset(arcH, 1));
        }

        ctx.rotate(p.angle);
        switch (p.kind) {
            case "slash": {
                ctx.strokeStyle = "rgba(255,255,255,.35)";
                ctx.lineWidth = 7; ctx.lineCap = "round";
                ctx.beginPath();
                ctx.arc(0, 0, 16, -0.65, 0.65);
                ctx.stroke();
                ctx.strokeStyle = p.color;
                ctx.lineWidth = 4; ctx.lineCap = "round";
                ctx.beginPath();
                ctx.arc(0, 0, 14, -0.6, 0.6);
                ctx.stroke();
                ctx.strokeStyle = `rgba(255,255,255,${0.45 + flick * 0.15})`;
                ctx.lineWidth = 1.5;
                ctx.beginPath();
                ctx.arc(0, 0, 11, -0.45, 0.45);
                ctx.stroke();
                break;
            }
            case "shock": {
                const pulse = 1 + flick * 0.12;
                ctx.fillStyle = "rgba(255,255,255,.25)";
                ctx.beginPath();
                ctx.moveTo(-10 * pulse, 0); ctx.lineTo(0, -5 * pulse); ctx.lineTo(10 * pulse, 0); ctx.lineTo(0, 5 * pulse);
                ctx.fill();
                ctx.fillStyle = p.color;
                ctx.beginPath();
                ctx.moveTo(-8, 0); ctx.lineTo(0, -4); ctx.lineTo(8, 0); ctx.lineTo(0, 4);
                ctx.fill();
                break;
            }
            case "spear": {
                ctx.fillStyle = "rgba(255,255,255,.2)";
                ctx.fillRect(-12, -2.5, 22, 5);
                ctx.fillStyle = p.color;
                ctx.fillRect(-10, -1.5, 20, 3);
                ctx.beginPath();
                ctx.moveTo(10, -3); ctx.lineTo(18, 0); ctx.lineTo(10, 3); ctx.fill();
                ctx.fillStyle = "rgba(255,255,255,.5)";
                ctx.fillRect(-6, -0.8, 8, 1.6);
                break;
            }
            case "fan": {
                ctx.fillStyle = "rgba(255,255,255,.2)";
                ctx.beginPath();
                ctx.moveTo(0, 0);
                ctx.arc(0, 0, 12, -0.65, 0.65);
                ctx.closePath(); ctx.fill();
                ctx.fillStyle = p.color;
                ctx.beginPath();
                ctx.moveTo(0, 0);
                ctx.arc(0, 0, 10, -0.6, 0.6);
                ctx.closePath(); ctx.fill();
                ctx.strokeStyle = "#fff"; ctx.lineWidth = 1;
                ctx.beginPath(); ctx.moveTo(0, 0); ctx.lineTo(10, 0); ctx.stroke();
                break;
            }
            case "halberd": {
                ctx.fillStyle = "rgba(255,240,200,.25)";
                ctx.fillRect(-16, -2.5, 30, 5);
                ctx.fillStyle = p.color;
                ctx.fillRect(-14, -2, 28, 4);
                ctx.beginPath();
                ctx.moveTo(14, -6); ctx.lineTo(24, 0); ctx.lineTo(14, 6); ctx.fill();
                ctx.beginPath();
                ctx.moveTo(8, -8); ctx.lineTo(14, 0); ctx.lineTo(8, 0); ctx.fill();
                ctx.strokeStyle = "rgba(255,255,255,.4)"; ctx.lineWidth = 1;
                ctx.beginPath(); ctx.moveTo(-10, 0); ctx.lineTo(8, 0); ctx.stroke();
                break;
            }
            case "fire": {
                const r0 = 11 + flick * 2;
                const grd = ctx.createRadialGradient(0, 0, 0, 0, 0, r0 + 4);
                grd.addColorStop(0, "#fffce8");
                grd.addColorStop(0.35, p.color);
                grd.addColorStop(0.75, "rgba(255,80,0,.6)");
                grd.addColorStop(1, "rgba(255,40,0,0)");
                ctx.fillStyle = grd;
                ctx.beginPath(); ctx.arc(0, 0, r0 + 4, 0, Math.PI * 2); ctx.fill();
                ctx.fillStyle = "rgba(255,200,80,.75)";
                ctx.beginPath(); ctx.arc(-3 + flick * 2, -2, 3, 0, Math.PI * 2); ctx.fill();
                ctx.beginPath(); ctx.arc(4, 3, 2.2, 0, Math.PI * 2); ctx.fill();
                break;
            }
        }
        ctx.restore();
    },

    // ============ 大招特效 ============
    drawEffect(ctx, ef) {
        const t = ef.elapsed / ef.duration;
        const P = window.Projection;
        const iso = P && P.enabled;
        ctx.save();
        switch (ef.kind) {
            case "flood": {
                // 2.5D 水淹七军：水位从地面向上抬升，多层水面 + 水墙侧面 + 波浪粒子
                const waterLevel = iso ? Math.min(1, ef.elapsed / 0.8) : 0;
                const wH = waterLevel * 40; // 世界单位高度（加大，水位更明显）
                const liftY = iso ? P.heightOffset(wH, 1) : 0;

                ctx.lineCap = "round"; ctx.lineJoin = "round";

                // 地面层：暗色深水底
                ctx.strokeStyle = `rgba(20,60,120,${0.55 + 0.1 * waterLevel})`;
                ctx.lineWidth = 58;
                ctx.beginPath();
                for (let i = 0; i < ef.path.length; i++) {
                    const p = ef.path[i];
                    if (i === 0) ctx.moveTo(p.x, p.y);
                    else ctx.lineTo(p.x, p.y);
                }
                ctx.stroke();

                if (iso) {
                    // 2.5D 水墙侧面：从地面连到水面，形成"实际水深"的立体感
                    if (liftY < 0) {
                        ctx.save();
                        ctx.fillStyle = `rgba(30,90,170,${0.35 * waterLevel})`;
                        for (let i = 0; i < ef.path.length - 1; i++) {
                            const a = ef.path[i], b = ef.path[i + 1];
                            ctx.fillStyle = `rgba(30,90,170,${0.35 * waterLevel})`;
                            ctx.beginPath();
                            const w2 = 26;
                            ctx.moveTo(a.x - w2, a.y);
                            ctx.lineTo(b.x - w2, b.y);
                            ctx.lineTo(b.x - w2 * 0.92, b.y + liftY);
                            ctx.lineTo(a.x - w2 * 0.92, a.y + liftY);
                            ctx.closePath();
                            ctx.fill();
                        }
                        // 右侧面略暗
                        ctx.fillStyle = `rgba(25,75,150,${0.4 * waterLevel})`;
                        for (let i = 0; i < ef.path.length - 1; i++) {
                            const a = ef.path[i], b = ef.path[i + 1];
                            ctx.beginPath();
                            const w2 = 26;
                            ctx.moveTo(a.x + w2, a.y);
                            ctx.lineTo(b.x + w2, b.y);
                            ctx.lineTo(b.x + w2 * 0.92, b.y + liftY);
                            ctx.lineTo(a.x + w2 * 0.92, a.y + liftY);
                            ctx.closePath();
                            ctx.fill();
                        }
                        ctx.restore();
                    }

                    // 抬升的水面层（半透明，可见"水深"）
                    ctx.save();
                    ctx.translate(0, liftY);
                    ctx.strokeStyle = `rgba(40,120,220,${0.4 + 0.15 * Math.sin(ef.elapsed * 6)})`;
                    ctx.lineWidth = 52;
                    ctx.stroke();
                    ctx.strokeStyle = `rgba(80,170,255,${0.55 + 0.2 * Math.sin(ef.elapsed * 8)})`;
                    ctx.lineWidth = 40;
                    ctx.stroke();
                    ctx.strokeStyle = `rgba(200,240,255,0.75)`;
                    ctx.lineWidth = 14;
                    ctx.stroke();
                    ctx.restore();

                    // 水面波浪溅起粒子（在抬升高度）
                    for (let i = 0; i < 32; i++) {
                        const seed = (i * 53 + ef.elapsed * 120) % U.pathLength(ef.path);
                        const pt = U.pointOnPath(ef.path, seed);
                        const wave = Math.sin(ef.elapsed * 10 + i * 0.5) * 5;
                        const r = 3.5 + Math.sin(ef.elapsed * 10 + i) * 2;
                        ctx.fillStyle = "rgba(255,255,255,.9)";
                        ctx.beginPath(); ctx.arc(pt.x, pt.y + liftY + wave, r, 0, Math.PI * 2); ctx.fill();
                    }
                } else {
                    // 回退原始绘制
                    ctx.strokeStyle = `rgba(40,120,220,${0.25 + 0.15 * Math.sin(ef.elapsed * 6)})`;
                    ctx.lineWidth = 58;
                    ctx.stroke();
                    ctx.strokeStyle = `rgba(80,170,255,${0.55 + 0.25 * Math.sin(ef.elapsed * 8)})`;
                    ctx.lineWidth = 44;
                    ctx.stroke();
                    ctx.strokeStyle = `rgba(200,240,255,0.85)`;
                    ctx.lineWidth = 16;
                    ctx.stroke();
                    for (let i = 0; i < 32; i++) {
                        const seed = (i * 53 + ef.elapsed * 120) % U.pathLength(ef.path);
                        const pt = U.pointOnPath(ef.path, seed);
                        const r = 4 + Math.sin(ef.elapsed * 10 + i) * 2;
                        ctx.fillStyle = "rgba(255,255,255,.9)";
                        ctx.beginPath(); ctx.arc(pt.x, pt.y, r, 0, Math.PI * 2); ctx.fill();
                    }
                }
                break;
            }
            case "blaze":
                // 2.5D 火烧赤壁：火焰沿路径蔓延 + 立体火柱向上延伸
                for (let i = 0; i < ef.path.length - 1; i++) {
                    const a = ef.path[i], b = ef.path[i + 1];
                    const segLen = U.dist(a.x, a.y, b.x, b.y);
                    const steps = Math.max(3, segLen / 10);
                    for (let s = 0; s < steps; s++) {
                        const x = U.lerp(a.x, b.x, s / steps);
                        const y = U.lerp(a.y, b.y, s / steps);
                        const r = 20 + Math.sin(ef.elapsed * 8 + s) * 6;
                        // 地面火光
                        const grd = ctx.createRadialGradient(x, y, 0, x, y, r);
                        grd.addColorStop(0, "rgba(255,255,180,.95)");
                        grd.addColorStop(0.4, "rgba(255,140,30,.7)");
                        grd.addColorStop(1, "rgba(180,40,0,0)");
                        ctx.fillStyle = grd;
                        ctx.beginPath(); ctx.arc(x, y, r, 0, Math.PI * 2); ctx.fill();

                        // 2.5D：立体火柱
                        if (iso && s % 2 === 0) {
                            const flameH = 55 + Math.sin(ef.elapsed * 12 + s * 0.5) * 15;
                            const liftY = P.heightOffset(flameH, 1);
                            const fg = ctx.createLinearGradient(x, y, x, y + liftY);
                            fg.addColorStop(0, "rgba(255,200,60,0.18)");
                            fg.addColorStop(0.3, "rgba(255,160,40,0.5)");
                            fg.addColorStop(0.7, "rgba(255,100,20,0.4)");
                            fg.addColorStop(1, "rgba(200,40,0,0)");
                            ctx.fillStyle = fg;
                            const fw = 16 + Math.sin(ef.elapsed * 10 + s) * 4;
                            ctx.beginPath();
                            ctx.moveTo(x - fw, y);
                            ctx.quadraticCurveTo(x - fw * 0.5, y + liftY * 0.5, x, y + liftY);
                            ctx.quadraticCurveTo(x + fw * 0.5, y + liftY * 0.5, x + fw, y);
                            ctx.closePath();
                            ctx.fill();
                            ctx.fillStyle = "rgba(255,255,200,0.6)";
                            ctx.beginPath();
                            ctx.arc(x, y + liftY, 4, 0, Math.PI * 2);
                            ctx.fill();
                        }
                    }
                }
                break;
            case "stun": {
                const life = Math.min(1, ef.elapsed / ef.duration);
                const r = U.lerp(20, 520, life);
                const a = 1 - life;
                for (let ring = 0; ring < 3; ring++) {
                    const rr = r * (0.55 + ring * 0.22);
                    ctx.strokeStyle = `rgba(255,200,80,${a * (0.9 - ring * 0.2)})`;
                    ctx.lineWidth = 8 - ring * 2;
                    ctx.beginPath(); ctx.arc(ef.x, ef.y, rr, 0, Math.PI * 2); ctx.stroke();
                }
                // 2.5D：震波穹顶 — 从地面向上隆起的半球光罩
                if (iso) {
                    const domeR = r * 1.0;
                    const domeH = P.heightOffset(r * 0.45, 1);
                    // 绘制 3 层向上叠起的上半球弧（穹顶轮廓）
                    for (let arc = 0; arc < 3; arc++) {
                        const lift = domeH * (0.35 + arc * 0.32);
                        const arcScale = 1 - arc * 0.16;
                        ctx.strokeStyle = `rgba(255,230,130,${a * (0.6 - arc * 0.15)})`;
                        ctx.lineWidth = 5 - arc;
                        ctx.beginPath();
                        ctx.arc(ef.x, ef.y + lift, domeR * arcScale, Math.PI, 0); // 上半圆
                        ctx.stroke();
                    }
                    // 垂直光丝（穹顶框架）
                    ctx.strokeStyle = `rgba(255,210,120,${a * 0.4})`;
                    ctx.lineWidth = 2;
                    for (let i = 0; i < 8; i++) {
                        const ang = i / 8 * Math.PI;
                        const sx = ef.x + Math.cos(ang) * domeR * 0.4;
                        const sy = ef.y;
                        ctx.beginPath();
                        ctx.moveTo(sx, sy);
                        ctx.quadraticCurveTo(sx + Math.cos(ang) * domeR * 0.2, sy + domeH * 0.5, ef.x, ef.y + domeH);
                        ctx.stroke();
                    }
                }
                ctx.fillStyle = `rgba(255,255,220,${a * 0.35})`;
                ctx.beginPath(); ctx.arc(ef.x, ef.y, 28, 0, Math.PI * 2); ctx.fill();
                break;
            }
            case "maze":
                // 2.5D 八阵图：旋转阵法 + 立体光柱
                ctx.translate(ef.x, ef.y);
                // 地面阵法圆环
                ctx.save();
                ctx.rotate(ef.elapsed * 1.2);
                for (let i = 0; i < 8; i++) {
                    const ang = i / 8 * Math.PI * 2;
                    const x = Math.cos(ang) * 60, y = Math.sin(ang) * 60;
                    ctx.fillStyle = `rgba(122,216,255,${0.4 + 0.4 * Math.sin(ef.elapsed * 6 + i)})`;
                    ctx.beginPath(); ctx.arc(x, y, 12, 0, Math.PI * 2); ctx.fill();
                }
                ctx.strokeStyle = "rgba(122,216,255,.5)";
                ctx.lineWidth = 2;
                ctx.beginPath(); ctx.arc(0, 0, 60, 0, Math.PI * 2); ctx.stroke();
                ctx.strokeStyle = "#fff"; ctx.lineWidth = 1;
                ctx.beginPath(); ctx.arc(0, 0, 30, 0, Math.PI * 2); ctx.stroke();
                ctx.restore();

                // 2.5D：8 根光柱从阵法节点向上射出
                if (iso) {
                    const pulse = 0.5 + 0.5 * Math.sin(ef.elapsed * 4);
                    const pillarH = P.heightOffset(95 + pulse * 25, 1);
                    for (let i = 0; i < 8; i++) {
                        const ang = i / 8 * Math.PI * 2 + ef.elapsed * 0.8;
                        const x = Math.cos(ang) * 60, y = Math.sin(ang) * 60;
                        // 光柱本体（渐变向上渐隐，更宽更亮）
                        const grd = ctx.createLinearGradient(x, y, x, y + pillarH);
                        grd.addColorStop(0, `rgba(122,216,255,${0.85 + pulse * 0.15})`);
                        grd.addColorStop(0.4, `rgba(170,235,255,${0.45 + pulse * 0.2})`);
                        grd.addColorStop(1, "rgba(200,240,255,0)");
                        ctx.fillStyle = grd;
                        const pw = 12 + pulse * 4;
                        ctx.beginPath();
                        ctx.moveTo(x - pw, y);
                        ctx.lineTo(x + pw, y);
                        ctx.lineTo(x + pw * 0.5, y + pillarH);
                        ctx.lineTo(x - pw * 0.5, y + pillarH);
                        ctx.closePath();
                        ctx.fill();
                        // 光柱底部亮点
                        ctx.fillStyle = `rgba(180,240,255,${0.7 + pulse * 0.3})`;
                        ctx.beginPath();
                        ctx.arc(x, y, 5, 0, Math.PI * 2);
                        ctx.fill();
                    }
                }
                break;
            case "execute":
                // 2.5D 处决一击：金色十字 + 立体光柱冲向天空
                if (P && P.enabled) {
                    // 立体光柱：从中点向上延伸的十字剑光
                    ctx.save();
                    ctx.translate(ef.x, ef.y);
                    const colH = P.heightOffset(90 + (Math.sin(ef.elapsed * 20) * 10), 1); // 震荡上升
                    const fg = ctx.createLinearGradient(0, 0, 0, colH);
                    const bright = Math.max(0, 1 - t);
                    fg.addColorStop(0, `rgba(255,230,140,${0.75 * bright})`);
                    fg.addColorStop(0.4, `rgba(255,200,80,${0.45 * bright})`);
                    fg.addColorStop(1, "rgba(255,180,60,0)");
                    ctx.fillStyle = fg;
                    // 十字光柱本体（宽阔渐细向上）
                    const colW = 26 + Math.sin(ef.elapsed * 20) * 4;
                    ctx.beginPath();
                    ctx.moveTo(-colW, 10);
                    ctx.lineTo(colW, 10);
                    ctx.lineTo(28, colH);
                    ctx.lineTo(-28, colH);
                    ctx.closePath();
                    ctx.fill();
                    // 十字横向光刃
                    const hw = 46;
                    ctx.strokeStyle = `rgba(255,220,100,${0.85 * bright})`;
                    ctx.lineWidth = 5;
                    ctx.beginPath();
                    ctx.moveTo(-hw, 0); ctx.lineTo(hw, 0);
                    ctx.moveTo(0, -hw * 0.4); ctx.lineTo(0, hw * 0.4);
                    ctx.stroke();
                    // 核心亮点
                    ctx.fillStyle = `rgba(255,255,255,${0.9 * bright})`;
                    ctx.beginPath(); ctx.arc(0, 0, 9, 0, Math.PI * 2); ctx.fill();
                    ctx.restore();
                } else {
                    ctx.translate(ef.x, ef.y);
                    ctx.rotate(ef.elapsed * 6);
                    ctx.strokeStyle = `rgba(255,213,90,${1 - t})`;
                    ctx.lineWidth = 6;
                    ctx.beginPath();
                    ctx.moveTo(-50, 0); ctx.lineTo(50, 0);
                    ctx.moveTo(0, -50); ctx.lineTo(0, 50);
                    ctx.stroke();
                    ctx.fillStyle = `rgba(255,255,255,${0.8 - 0.8 * t})`;
                    ctx.beginPath(); ctx.arc(0, 0, 10, 0, Math.PI * 2); ctx.fill();
                }
                break;
            case "hex": {
                // 2.5D 奇门阵：紫色阵法路径 + 旋转光点 + 立体光柱
                if (ef.path) {
                    ctx.strokeStyle = `rgba(160,100,255,${0.45 + 0.2 * Math.sin(ef.elapsed * 4)})`;
                    ctx.lineWidth = 36;
                    ctx.lineCap = "round";
                    ctx.beginPath();
                    for (let i = 0; i < ef.path.length; i++) {
                        const p = ef.path[i];
                        if (i === 0) ctx.moveTo(p.x, p.y);
                        else ctx.lineTo(p.x, p.y);
                    }
                    ctx.stroke();
                    // 阵法底部亮紫色描边（增强可见性）
                    ctx.strokeStyle = `rgba(220,170,255,${0.3 + 0.2 * Math.sin(ef.elapsed * 5)})`;
                    ctx.lineWidth = 14;
                    ctx.stroke();
                }
                const base = (ef.path && ef.path[0]) ? ef.path[0] : { x: 480, y: 300 };
                for (let i = 0; i < 6; i++) {
                    const ang = i / 6 * Math.PI * 2 + ef.elapsed * 0.8;
                    const cx = base.x + Math.cos(ang) * 80;
                    const cy = base.y + Math.sin(ang) * 80;
                    // 地面光点
                    ctx.fillStyle = `rgba(200,160,255,${0.45 + 0.25 * Math.sin(ef.elapsed * 5 + i)})`;
                    ctx.beginPath();
                    ctx.arc(cx, cy, 10, 0, Math.PI * 2);
                    ctx.fill();

                    // 2.5D：每点向上射一道紫色光柱
                    if (P && P.enabled) {
                        const colH = P.heightOffset(55 + Math.sin(ef.elapsed * 6 + i) * 8, 1);
                        const cg = ctx.createLinearGradient(cx, cy, cx, cy + colH);
                        cg.addColorStop(0, `rgba(210,160,255,${0.7 + 0.2 * Math.sin(ef.elapsed * 5 + i)})`);
                        cg.addColorStop(0.45, `rgba(170,120,255,${0.42 + 0.2 * Math.sin(ef.elapsed * 5 + i)})`);
                        cg.addColorStop(1, "rgba(150,100,255,0)");
                        ctx.fillStyle = cg;
                        const lw = 9;
                        ctx.beginPath();
                        ctx.moveTo(cx - lw, cy);
                        ctx.lineTo(cx + lw, cy);
                        ctx.lineTo(cx + lw * 0.45, cy + colH);
                        ctx.lineTo(cx - lw * 0.45, cy + colH);
                        ctx.closePath();
                        ctx.fill();
                        // 光柱核心亮线
                        ctx.strokeStyle = `rgba(230,200,255,${0.4 + 0.2 * Math.sin(ef.elapsed * 6 + i)})`;
                        ctx.lineWidth = 2;
                        ctx.beginPath();
                        ctx.moveTo(cx, cy);
                        ctx.lineTo(cx, cy + colH * 0.85);
                        ctx.stroke();
                    }
                }
                break;
            }
            case "tide": {
                // 2.5D 潮汐涌动：潮水冲刷路径 + 动态抬升水面
                const surge = 0.5 + 0.5 * Math.sin(ef.elapsed * 3.2); // 潮汐涨落周期
                const tideH = iso ? P.heightOffset(16 + surge * 24, 1) : 0;
                ctx.lineCap = "round";
                ctx.strokeStyle = `rgba(50,140,220,${0.4 + 0.15 * Math.sin(ef.elapsed * 6)})`;
                ctx.lineWidth = 50;
                ctx.beginPath();
                for (let i = 0; i < ef.path.length; i++) {
                    const p = ef.path[i];
                    if (i === 0) ctx.moveTo(p.x, p.y);
                    else ctx.lineTo(p.x, p.y);
                }
                ctx.stroke();
                ctx.strokeStyle = `rgba(180,230,255,${0.7})`;
                ctx.lineWidth = 18;
                ctx.stroke();

                if (iso && tideH < 0) {
                    // 潮汐抬升水层 + 前进水墙（模拟潮头）
                    ctx.fillStyle = `rgba(40,120,210,${0.3 + surge * 0.15})`;
                    for (let i = 0; i < ef.path.length - 1; i++) {
                        const a = ef.path[i], b = ef.path[i + 1];
                        ctx.beginPath();
                        const w2 = 24;
                        ctx.moveTo(a.x - w2, a.y); ctx.lineTo(b.x - w2, b.y);
                        ctx.lineTo(b.x - w2 * 0.9, b.y + tideH); ctx.lineTo(a.x - w2 * 0.9, a.y + tideH);
                        ctx.closePath(); ctx.fill();
                    }
                    // 浪花白沫（潮头）
                    for (let i = 0; i < 20; i++) {
                        const seed = (i * 61 + ef.elapsed * 100) % U.pathLength(ef.path);
                        const pt = U.pointOnPath(ef.path, seed);
                        const f = 0.6 + 0.4 * Math.sin(ef.elapsed * 8 + i);
                        ctx.fillStyle = `rgba(220,245,255,${0.6 * f})`;
                        ctx.beginPath();
                        ctx.arc(pt.x, pt.y + tideH + Math.sin(ef.elapsed * 6 + i) * 3, 3 + f * 2, 0, Math.PI * 2);
                        ctx.fill();
                    }
                }
                break;
            }
            case "rally": {
                const life = Math.min(1, ef.elapsed / ef.duration);
                const pulse = 0.5 + 0.5 * Math.sin(ef.elapsed * 5);
                if (ef.path) {
                    ctx.strokeStyle = `rgba(212,175,90,${0.35 + pulse * 0.35})`;
                    ctx.lineWidth = 22;
                    ctx.lineCap = "round";
                    ctx.beginPath();
                    for (let i = 0; i < ef.path.length; i++) {
                        const p = ef.path[i];
                        if (i === 0) ctx.moveTo(p.x, p.y);
                        else ctx.lineTo(p.x, p.y);
                    }
                    ctx.stroke();
                }
                const bursts = ef.bursts || [];
                for (let i = 0; i < bursts.length; i++) {
                    const b = bursts[i];
                    const r = 24 + life * 40 + pulse * 8;
                    // 地面光圈
                    const grd = ctx.createRadialGradient(b.x, b.y, 0, b.x, b.y, r);
                    grd.addColorStop(0, `rgba(255,240,180,${0.7 * (1 - life)})`);
                    grd.addColorStop(1, "rgba(212,175,90,0)");
                    ctx.fillStyle = grd;
                    ctx.beginPath();
                    ctx.arc(b.x, b.y, r, 0, Math.PI * 2);
                    ctx.fill();

                    // 2.5D：金色光柱从塔位向上射出
                    if (iso) {
                        const colH = P.heightOffset(60 + pulse * 15, 1);
                        const cg = ctx.createLinearGradient(b.x, b.y, b.x, b.y + colH);
                        cg.addColorStop(0, `rgba(255,230,140,${0.5 * (1 - life) + pulse * 0.2})`);
                        cg.addColorStop(0.4, `rgba(255,210,100,${0.3 * (1 - life) + pulse * 0.15})`);
                        cg.addColorStop(1, "rgba(255,200,80,0)");
                        ctx.fillStyle = cg;
                        const cw2 = 16 + pulse * 4;
                        ctx.beginPath();
                        ctx.moveTo(b.x - cw2, b.y);
                        ctx.lineTo(b.x + cw2, b.y);
                        ctx.lineTo(b.x + cw2 * 0.3, b.y + colH);
                        ctx.lineTo(b.x - cw2 * 0.3, b.y + colH);
                        ctx.closePath();
                        ctx.fill();
                    }
                }
                break;
            }
            case "charge":
                // 2.5D 七进七出：银影残像 + 拖尾抬升
                for (let i = 0; i < ef.trail.length; i++) {
                    const tr = ef.trail[i];
                    const a = i / ef.trail.length;
                    if (iso && i < ef.trail.length - 2) {
                        // 残像有高度，模拟冲锋腾空
                        const trailLift = P.heightOffset((1 - a) * 12, 1);
                        ctx.fillStyle = `rgba(220,230,255,${a * 0.5})`;
                        ctx.beginPath(); ctx.arc(tr.x, tr.y + trailLift, 12 * a, 0, Math.PI * 2); ctx.fill();
                    } else {
                        ctx.fillStyle = `rgba(220,230,255,${a * 0.6})`;
                        ctx.beginPath(); ctx.arc(tr.x, tr.y, 12 * a, 0, Math.PI * 2); ctx.fill();
                    }
                }
                // 主体（2.5D 略微抬升）
                if (iso) ctx.translate(0, P.heightOffset(8, 1));
                ctx.fillStyle = "#e8efff";
                ctx.beginPath(); ctx.arc(ef.x, ef.y, 14, 0, Math.PI * 2); ctx.fill();
                ctx.strokeStyle = "#fff"; ctx.lineWidth = 2;
                ctx.beginPath(); ctx.moveTo(ef.x - 18, ef.y); ctx.lineTo(ef.x + 18, ef.y); ctx.stroke();
                break;
            case "impact": {
                const life = Math.min(1, ef.elapsed / ef.duration);
                const out = 1 - life;
                const sm = ef.small ? 0.55 : 1;
                const cx = ef.x, cy = ef.y;
                const maxR = (ef.small ? 12 : 18) + life * (ef.small ? 16 : 26);
                ctx.save();
                ctx.translate(cx, cy);
                if (ef.fire) {
                    const grd = ctx.createRadialGradient(0, 0, 0, 0, 0, maxR);
                    grd.addColorStop(0, `rgba(255,255,230,${out * 0.95})`);
                    grd.addColorStop(0.28, `rgba(255,160,50,${out * 0.7})`);
                    grd.addColorStop(0.65, `rgba(255,70,20,${out * 0.35})`);
                    grd.addColorStop(1, "rgba(180,30,0,0)");
                    ctx.fillStyle = grd;
                    ctx.beginPath(); ctx.arc(0, 0, maxR, 0, Math.PI * 2); ctx.fill();
                } else {
                    ctx.strokeStyle = `rgba(255,255,255,${out * 0.55})`;
                    ctx.lineWidth = 2.5 * sm;
                    ctx.beginPath(); ctx.arc(0, 0, 5 + life * 22 * sm, 0, Math.PI * 2); ctx.stroke();
                    ctx.fillStyle = `rgba(255,230,160,${out * 0.45})`;
                    ctx.beginPath(); ctx.arc(0, 0, 4 * sm + life * 6 * sm, 0, Math.PI * 2); ctx.fill();
                }
                if (ef.projKind === "slash") {
                    ctx.strokeStyle = `rgba(255,255,255,${out * 0.5})`;
                    ctx.lineWidth = 2.5 * sm;
                    ctx.beginPath(); ctx.arc(0, 0, 10 + life * 18 * sm, -0.75, 0.75); ctx.stroke();
                } else if (ef.projKind === "shock") {
                    ctx.strokeStyle = `rgba(200,230,255,${out * 0.65})`;
                    ctx.lineWidth = 2 * sm;
                    ctx.beginPath();
                    ctx.moveTo(-9 * sm, 0); ctx.lineTo(0, -5 * sm); ctx.lineTo(9 * sm, 0); ctx.lineTo(0, 5 * sm);
                    ctx.closePath(); ctx.stroke();
                }
                ctx.restore();
                break;
            }
            case "pierceFlash": {
                const pulse = 1 - t;
                ctx.strokeStyle = `rgba(200,240,255,${pulse * 0.9})`;
                ctx.lineWidth = 3;
                ctx.beginPath();
                ctx.arc(ef.x, ef.y, 6 + t * 32, 0, Math.PI * 2);
                ctx.stroke();
                ctx.strokeStyle = `rgba(255,255,255,${pulse * 0.55})`;
                ctx.lineWidth = 1.2;
                ctx.beginPath();
                ctx.arc(ef.x, ef.y, 3 + t * 14, 0, Math.PI * 2);
                ctx.stroke();
                break;
            }
            case "hit": {
                const fade = 1 - t;
                ctx.strokeStyle = `rgba(255,200,100,${fade * 0.65})`;
                ctx.lineWidth = 2;
                ctx.beginPath(); ctx.arc(ef.x, ef.y, 7 + t * 22, 0, Math.PI * 2); ctx.stroke();
                ctx.fillStyle = `rgba(255,220,80,${fade * 0.72})`;
                ctx.beginPath(); ctx.arc(ef.x, ef.y, 5 + t * 11, 0, Math.PI * 2); ctx.fill();
                ctx.fillStyle = `rgba(255,255,255,${fade * 0.38})`;
                ctx.beginPath(); ctx.arc(ef.x, ef.y, 3, 0, Math.PI * 2); ctx.fill();
                break;
            }
            case "coin": {
                const a = 1 - t;
                Art._drawFloatingText(ctx, "+" + ef.value + "金", ef.x, ef.y - 18 * t,
                    `rgba(247,215,116,${a})`, `rgba(74,41,19,${a * 0.85})`);
                break;
            }
            case "damage": {
                const a = 1 - t;
                Art._drawFloatingText(ctx, String(ef.value), ef.x, ef.y - 14 * t,
                    `rgba(232,90,50,${a})`, `rgba(40,12,8,${a * 0.9})`);
                break;
            }
            case "mergeBurst": {
                const life = ef.elapsed / ef.duration;
                const tier = ef.mergeTier || 1;
                const col = tier === 2 ? [247, 215, 116] : [232, 80, 50];
                for (let i = 0; i < 5; i++) {
                    const p = Math.min(1, life * 1.6 - i * 0.12);
                    if (p <= 0) continue;
                    const rad = 12 + p * 70 + i * 8;
                    ctx.strokeStyle = `rgba(${col[0]},${col[1]},${col[2]},${(1 - p) * 0.8})`;
                    ctx.lineWidth = 4 - i * 0.5;
                    ctx.beginPath();
                    ctx.arc(ef.x, ef.y, rad, 0, Math.PI * 2);
                    ctx.stroke();
                }
                ctx.fillStyle = `rgba(255,255,255,${0.5 * (1 - life)})`;
                ctx.font = "bold 14px serif";
                ctx.textAlign = "center";
                ctx.fillText(tier === 2 ? "金将" : "红将", ef.x, ef.y - 20 * life);
                break;
            }
            case "ultBurst": {
                const life = ef.elapsed / ef.duration;
                const burstPal = {
                    flood: [100, 180, 255],
                    blaze: [255, 140, 60],
                    stun: [255, 220, 100],
                    maze: [140, 220, 255],
                    execute: [255, 220, 120],
                    charge: [220, 235, 255],
                    hex: [180, 120, 255],
                    tide: [80, 160, 240],
                    rally: [212, 180, 90]
                };
                const bc = burstPal[ef.ultType] || [247, 215, 116];
                // 地面扩散环
                const rings = 6;
                for (let i = 0; i < rings; i++) {
                    const p = Math.min(1, life * 1.5 - i * 0.14);
                    if (p <= 0) continue;
                    const rad = 14 + p * 110 + i * 10;
                    const a = (1 - p) * 0.82;
                    ctx.strokeStyle = `rgba(${bc[0]},${bc[1]},${bc[2]},${a})`;
                    ctx.lineWidth = 5.5 - i * 0.65;
                    ctx.beginPath(); ctx.arc(ef.x, ef.y, rad, 0, Math.PI * 2); ctx.stroke();
                }
                // 2.5D：能量光柱从武将位置向上爆发
                if (iso) {
                    const burstH = P.heightOffset(60 + life * 40, 1);
                    const bg = ctx.createLinearGradient(ef.x, ef.y, ef.x, ef.y + burstH);
                    bg.addColorStop(0, `rgba(${bc[0]},${bc[1]},${bc[2]},${0.6 * (1 - life)})`);
                    bg.addColorStop(0.5, `rgba(255,240,200,${0.35 * (1 - life)})`);
                    bg.addColorStop(1, `rgba(${bc[0]},${bc[1]},${bc[2]},0)`);
                    ctx.fillStyle = bg;
                    const bw = 22 + life * 8;
                    ctx.beginPath();
                    ctx.moveTo(ef.x - bw, ef.y);
                    ctx.lineTo(ef.x + bw, ef.y);
                    ctx.lineTo(ef.x + bw * 0.2, ef.y + burstH);
                    ctx.lineTo(ef.x - bw * 0.2, ef.y + burstH);
                    ctx.closePath();
                    ctx.fill();
                }
                // 地面核心光球
                const grd = ctx.createRadialGradient(ef.x, ef.y, 0, ef.x, ef.y, 40 + life * 30);
                grd.addColorStop(0, `rgba(255,255,255,${0.7 * (1 - life)})`);
                grd.addColorStop(0.5, `rgba(255,220,120,${0.45 * (1 - life)})`);
                grd.addColorStop(1, "rgba(255,120,40,0)");
                ctx.fillStyle = grd;
                ctx.beginPath(); ctx.arc(ef.x, ef.y, 40 + life * 30, 0, Math.PI * 2); ctx.fill();
                break;
            }
        }
        ctx.restore();
    },

    drawUltOverlay(ctx, flash, cw, ch, game) {
        if (!flash) return;
        const life = flash.elapsed / flash.duration;
        const fade = 1 - life;
        const pal = {
            flood: [60, 140, 255],
            blaze: [255, 120, 40],
            stun: [255, 220, 120],
            maze: [120, 220, 255],
            execute: [255, 215, 120],
            charge: [200, 220, 255],
            rally: [212, 180, 90],
            hex: [160, 100, 255],
            tide: [70, 150, 230]
        };
        const c = pal[flash.type] || [247, 215, 116];
        const alpha = fade * 0.42;
        ctx.fillStyle = `rgba(${c[0]},${c[1]},${c[2]},${alpha})`;
        ctx.fillRect(0, 0, cw, ch);
        if (game && game.level && game.level.path) {
            const v = game.view;
            const { w: vw, h: vh } = game._visibleSize();
            const sx = cw / vw;
            const sy = ch / vh;
            ctx.strokeStyle = `rgba(${c[0]},${c[1]},${c[2]},${fade * 0.65})`;
            ctx.lineWidth = 10;
            ctx.lineCap = "round";
            ctx.lineJoin = "round";
            ctx.beginPath();
            let started = false;
            for (let i = 0; i < game.level.path.length; i++) {
                const p = game.level.path[i];
                const px = (p.x - v.panX) * sx;
                const py = (p.y - v.panY) * sy;
                if (px < -80 || px > cw + 80 || py < -80 || py > ch + 80) continue;
                if (!started) { ctx.moveTo(px, py); started = true; }
                else ctx.lineTo(px, py);
            }
            if (started) ctx.stroke();
        }
        const tx = flash.x != null ? (flash.x - game.view.panX) * (cw / game._visibleSize().w) : cw * 0.5;
        const ty = flash.y != null ? (flash.y - game.view.panY) * (ch / game._visibleSize().h) : ch * 0.5;
        const grd = ctx.createRadialGradient(tx, ty, 0, tx, ty, Math.max(cw, ch) * 0.55);
        grd.addColorStop(0, `rgba(255,255,255,${fade * 0.35})`);
        grd.addColorStop(0.35, `rgba(${c[0]},${c[1]},${c[2]},${fade * 0.2})`);
        grd.addColorStop(1, "rgba(0,0,0,0)");
        ctx.fillStyle = grd;
        ctx.fillRect(0, 0, cw, ch);
    },

    // ============ 武将放置预览 / 范围 ============
    drawPlacePreview(ctx, x, y, range, valid, general) {
        ctx.save();
        ctx.fillStyle = valid ? "rgba(110,208,122,.18)" : "rgba(220,80,40,.18)";
        ctx.beginPath(); ctx.arc(x, y, range, 0, Math.PI * 2); ctx.fill();
        ctx.strokeStyle = valid ? "rgba(110,208,122,.7)" : "rgba(220,80,40,.7)";
        ctx.lineWidth = 2; ctx.stroke();
        if (general) {
            Art.drawPortrait(ctx, x, y, 22, general);
        }
        ctx.restore();
    },

    drawRange(ctx, x, y, range) {
        ctx.save();
        ctx.fillStyle = "rgba(247,215,116,.10)";
        ctx.beginPath(); ctx.arc(x, y, range, 0, Math.PI * 2); ctx.fill();
        ctx.strokeStyle = "rgba(247,215,116,.7)";
        ctx.setLineDash([4, 4]);
        ctx.lineWidth = 1.5; ctx.stroke();
        ctx.setLineDash([]);
        ctx.restore();
    },

    drawSlot(ctx, slot, hovered) {
        ctx.save();
        ctx.translate(slot.x, slot.y);
        ctx.fillStyle = hovered ? "rgba(90, 60, 30, 0.72)" : "rgba(50, 34, 18, 0.62)";
        ctx.beginPath();
        ctx.ellipse(0, 4, 26, 14, 0, 0, Math.PI * 2);
        ctx.fill();
        ctx.strokeStyle = hovered ? "rgba(247,215,116,.95)" : "rgba(247,215,116,.75)";
        ctx.lineWidth = hovered ? 2.5 : 2;
        ctx.beginPath();
        ctx.ellipse(0, 0, 24, 24, 0, 0, Math.PI * 2);
        ctx.stroke();
        ctx.fillStyle = hovered ? "#fff8d0" : "#f7d774";
        ctx.font = "bold 20px serif";
        ctx.textAlign = "center";
        ctx.textBaseline = "middle";
        ctx.fillText("+", 0, 0);
        ctx.restore();
    },

    _drawFloatingText(ctx, text, x, y, fill, stroke) {
        ctx.font = "bold 14px serif";
        ctx.textAlign = "center";
        ctx.lineWidth = 3;
        ctx.strokeStyle = stroke;
        ctx.strokeText(text, x, y);
        ctx.fillStyle = fill;
        ctx.fillText(text, x, y);
    }
};
