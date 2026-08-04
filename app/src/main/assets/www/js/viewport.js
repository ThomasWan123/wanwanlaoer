/**
 * 画布布局与镜头坐标（contain / cover / fill-pan）
 */
window.Viewport = {
    WORLD_W: 960,
    WORLD_H: 600,
    layoutMode: "contain",
    shortPlayfield: false,
    wideDefaultZoom: 1,
    ultraWide: false,

    isUltraWide() {
        return this.ultraWide;
    },

    /** 折叠外屏等极宽矮屏：aspect≥2 且高度较矮 */
    detectUltraWide(availW, availH) {
        return availW / Math.max(1, availH) >= 2.0 && availH < 520;
    },

    /** 2.5D 投影 Y 压缩系数 — 全局禁用，仅用于实体级高度偏移参考 */
    _sq() {
        return 1; // 全局矩阵不压缩，避免 fill-pan 模式下双重变形
    },

    /** 让整个 960×600 战场尽量完整落入视口 */
    fitWorldZoom(availW, availH) {
        const z = Math.min(availW / this.WORLD_W, availH / this.WORLD_H) * 0.96;
        return U.clamp(z, 0.62, 1);
    },

    /** fill-pan 下 zoom 对应的可见世界范围 */
    approxViewSize(availW, availH, zoom) {
        const lvw = this.WORLD_W / zoom;
        const lvh = this.WORLD_H / zoom;
        const s = Math.max(availW / lvw, availH / lvh);
        return { vw: availW / s, vh: availH / s };
    },

    /** 道路 + 塔位包围盒 */
    playContentBBox(path, slots) {
        if (!path || !path.length) {
            return { minX: 0, minY: 0, maxX: this.WORLD_W, maxY: this.WORLD_H, cx: 480, cy: 300 };
        }
        let minX = path[0].x, maxX = path[0].x, minY = path[0].y, maxY = path[0].y;
        for (let i = 1; i < path.length; i++) {
            const p = path[i];
            minX = Math.min(minX, p.x);
            maxX = Math.max(maxX, p.x);
            minY = Math.min(minY, p.y);
            maxY = Math.max(maxY, p.y);
        }
        if (slots) {
            for (let i = 0; i < slots.length; i++) {
                const s = slots[i];
                minX = Math.min(minX, s.x);
                maxX = Math.max(maxX, s.x);
                minY = Math.min(minY, s.y);
                maxY = Math.max(maxY, s.y);
            }
        }
        return {
            minX, minY, maxX, maxY,
            cx: (minX + maxX) * 0.5,
            cy: (minY + maxY) * 0.5
        };
    },

    /** 最小缩放：保证整条道路（含塔位）完整落入视野 */
    fitContentMinZoom(availW, availH, box, pad) {
        pad = pad == null ? 56 : pad;
        const bw = Math.max(160, box.maxX - box.minX + pad * 2);
        const bh = Math.max(140, box.maxY - box.minY + pad * 2);
        for (let z = 1.08; z >= 0.5; z -= 0.012) {
            const { vw, vh } = this.approxViewSize(availW, availH, z);
            if (vw >= bw && vh >= bh) {
                return U.clamp(z, 0.5, 1.08);
            }
        }
        return 0.5;
    },

    /** 道路是否能在当前缩放下完整落入 HUD 安全区（可配合 pan） */
    pathFitsInPlayfield(availW, availH, path, zoom, focus) {
        if (!path || !path.length) return true;
        focus = focus || {};
        const top = focus.top != null ? focus.top : Math.min(108, availH * 0.25);
        const bottom = focus.bottom != null ? focus.bottom : Math.min(104, availH * 0.24);
        const fy = focus.focusY != null ? focus.focusY : this.estimatePlayfieldFocusY(availH);
        const { vw, vh } = this.approxViewSize(availW, availH, zoom);
        const box = this.playContentBBox(path, null);
        const panY = U.clamp(box.cy - vh * fy, 0, Math.max(0, this.WORLD_H - vh));
        const panX = U.clamp(box.cx - vw * 0.5, 0, Math.max(0, this.WORLD_W - vw));
        const marginX = 8;
        for (let i = 0; i < path.length; i++) {
            const p = path[i];
            const sx = ((p.x - panX) / vw) * availW;
            const sy = ((p.y - panY) / vh) * availH;
            if (sx < marginX || sx > availW - marginX) return false;
            if (sy < top - 4 || sy > availH - bottom + 4) return false;
        }
        return true;
    },

    /** 最小缩放：道路+塔位完整落入视野 */
    fitPathMinZoom(availW, availH, path, slots) {
        if (!path || !path.length) return this.fitWorldZoom(availW, availH);
        const box = this.playContentBBox(path, slots);
        return this.fitContentMinZoom(availW, availH, box, 48);
    },

    /** 进关默认缩放：道路+塔位完整出现在 HUD 安全区 */
    fitEntryZoom(availW, availH, path, slots, focusY) {
        const fy = focusY != null ? focusY : this.estimatePlayfieldFocusY(availH);
        const box = this.playContentBBox(path, slots);
        const pad = 64;
        const bw = Math.max(300, box.maxX - box.minX + pad * 2);
        const bh = Math.max(220, box.maxY - box.minY + pad * 2);
        for (let z = 1.35; z >= 0.72; z -= 0.025) {
            const { vw, vh } = this.approxViewSize(availW, availH, z);
            if (vw < bw || vh < bh) continue;
            const panY = box.cy - vh * fy;
            if (panY >= 0 && panY <= this.WORLD_H - vh) {
                return U.clamp(z, 0.72, 1.35);
            }
            if (panY < 0) {
                const ratio = box.cy / vh;
                if (ratio >= fy - 0.07 && ratio <= fy + 0.07) {
                    return U.clamp(z, 0.72, 1.35);
                }
            }
        }
        return U.clamp(Math.max(availW / this.WORLD_W, availH / this.WORLD_H) * 0.94, 0.72, 1.2);
    },

    /** 外屏进关估算：顶栏+底栏之间的纵向焦点（0~1） */
    estimatePlayfieldFocusY(availH) {
        const top = Math.min(108, availH * 0.25);
        const bottom = Math.min(104, availH * 0.24);
        const safeH = Math.max(48, availH - top - bottom);
        return U.clamp((top + safeH * 0.5) / Math.max(1, availH), 0.42, 0.62);
    },

    isFillPan() {
        return this.layoutMode === "fill-pan";
    },

    isShortPlayfield() {
        return this.shortPlayfield;
    },

    /** @returns {{ layoutMode, shortPlayfield, wideDefaultZoom, cssW, cssH }} */
    computeLayout(availW, availH, opts) {
        opts = opts || {};
        const mobile = !!opts.mobile;
        const aspect = availW / availH;
        const isShort = availH < 380 || (availH / availW) < 0.42;
        const useCover = aspect >= 2.0 && !isShort && !mobile;

        let layoutMode, cssW, cssH, wideDefaultZoom;

        /* 触屏 / 矮屏：战斗区边到边铺满，HUD 浮层 */
        if (mobile || isShort) {
            layoutMode = "fill-pan";
            wideDefaultZoom = 1;
            cssW = Math.floor(availW);
            cssH = Math.floor(availH);
        } else if (useCover) {
            const scale = Math.max(availW / this.WORLD_W, availH / this.WORLD_H);
            const containScale = Math.min(availW / this.WORLD_W, availH / this.WORLD_H);
            layoutMode = "cover";
            wideDefaultZoom = U.clamp(scale / containScale, 1, (window.Game && Game.MAX_ZOOM) || 2.5);
            cssW = Math.floor(this.WORLD_W * scale);
            cssH = Math.floor(this.WORLD_H * scale);
        } else {
            const scale = Math.max(availW / this.WORLD_W, availH / this.WORLD_H);
            layoutMode = "fill-pan";
            wideDefaultZoom = 1;
            cssW = Math.floor(availW);
            cssH = Math.floor(availH);
        }

        this.layoutMode = layoutMode;
        this.shortPlayfield = isShort || mobile;
        this.ultraWide = this.detectUltraWide(availW, availH);
        this.wideDefaultZoom = wideDefaultZoom;

        return { layoutMode, shortPlayfield: isShort || mobile, ultraWide: this.ultraWide, wideDefaultZoom, cssW, cssH };
    },

    getMinZoom(ctx) {
        ctx = ctx || {};
        const aw = ctx.availW || 960;
        const ah = ctx.availH || 600;
        const path = ctx.path;
        const slots = ctx.slots;
        if (path && path.length && (this.ultraWide || this.shortPlayfield)) {
            return this.fitPathMinZoom(aw, ah, path, slots);
        }
        const box = ctx.bbox;
        if (box && (this.ultraWide || this.shortPlayfield)) {
            return this.fitContentMinZoom(aw, ah, box);
        }
        if (this.ultraWide) return this.fitWorldZoom(aw, ah);
        if (this.shortPlayfield) return 0.55;
        return (window.Game && Game.MIN_ZOOM) || 1;
    },

    logicalVisibleSize(zoom) {
        const z = zoom || 1;
        return { w: this.WORLD_W / z, h: this.WORLD_H / z };
    },

    /** 屏幕实际可见世界范围（fill-pan 为等比 cover 裁切） */
    viewFrame(game) {
        const z = game.view.zoom;
        const { w: lvw, h: lvh } = this.logicalVisibleSize(z);
        if (!this.isFillPan() || !game.canvas) {
            return { vw: lvw, vh: lvh, scale: null };
        }
        const rect = game.canvas.getBoundingClientRect();
        const cssW = rect.width > 0 ? rect.width : (game._cssDisplayW || this.WORLD_W);
        const cssH = rect.height > 0 ? rect.height : (game._cssDisplayH || this.WORLD_H);
        const s = Math.max(cssW / lvw, cssH / lvh);
        return { vw: cssW / s, vh: cssH / s, scale: s };
    },

    applyRenderTransform(ctx, game, backingW, backingH) {
        const v = game.view;
        const frame = this.viewFrame(game);
        if (this.isFillPan()) {
            const lvw = this.WORLD_W / v.zoom;
            const lvh = this.WORLD_H / v.zoom;
            const s = Math.max(backingW / lvw, backingH / lvh);
            ctx.setTransform(s, 0, 0, s, -v.panX * s, -v.panY * s);
        } else {
            const { w: vw, h: vh } = this.logicalVisibleSize(v.zoom);
            ctx.setTransform(backingW / vw, 0, 0, backingH / vh, -v.panX * (backingW / vw), -v.panY * (backingH / vh));
        }
        return frame;
    },

    screenToWorld(game, clientX, clientY) {
        const rect = game.canvas.getBoundingClientRect();
        const v = game.view;
        const { vw, vh } = this.viewFrame(game);
        const nx = rect.width > 0 ? (clientX - rect.left) / rect.width : 0;
        const ny = rect.height > 0 ? (clientY - rect.top) / rect.height : 0;
        return { x: v.panX + nx * vw, y: v.panY + ny * vh };
    },

    worldToScreen(game, worldX, worldY) {
        const rect = game.canvas.getBoundingClientRect();
        const v = game.view;
        const { vw, vh } = this.viewFrame(game);
        return {
            x: rect.left + ((worldX - v.panX) / vw) * rect.width,
            y: rect.top + ((worldY - v.panY) / vh) * rect.height
        };
    },

    clampPan(game) {
        const v = game.view;
        const { vw, vh } = this.viewFrame(game);
        v.panX = U.clamp(v.panX, 0, Math.max(0, this.WORLD_W - vw));
        v.panY = U.clamp(v.panY, 0, Math.max(0, this.WORLD_H - vh));
    },

    canPanAtZoom(zoom) {
        return this.isFillPan() || this.layoutMode === "cover" || zoom > 1.001;
    }
};
