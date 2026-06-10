/**
 * MobileBridge — 触摸适配、视口手势、响应式缩放、原生桥接
 */
window.MobileBridge = {
    _isMobile: ("ontouchstart" in window) || (navigator.maxTouchPoints > 0),
    _touchId: null,
    _longPressTimer: null,
    _touchStartPos: null,
    _lastTouchTime: 0,
    _gesture: null,       // tap | pan | pinch
    _suppressClick: false,
    _pinchStartDist: 0,
    _pinchStartZoom: 1,
    _pinchLastMid: null,
    _panLastWorld: null,
    _lastTapTime: 0,
    _lastTapPos: null,
    _layoutMode: "contain",
    _wideDefaultZoom: 1,
    _shortPlayfield: false,
    _ultraWide: false,
    ZOOM_STEP: 1.18,

    _isUltraWide() {
        if (this._ultraWide) return true;
        const gs = document.getElementById("game-screen");
        const w = gs ? gs.clientWidth : window.innerWidth;
        const h = gs ? gs.clientHeight : window.innerHeight;
        return window.Viewport
            ? Viewport.detectUltraWide(w, h)
            : (w / Math.max(1, h) >= 2.0 && h < 520);
    },

    _applyEntryZoom(path) {
        if (!Game || !window.Viewport) return;
        const gs = document.getElementById("game-screen");
        const aw = gs ? gs.clientWidth : window.innerWidth;
        const ah = gs ? gs.clientHeight : window.innerHeight;
        const focus = this._getPlayfieldFocus();
        const slots = Game.level && Game.level.slots;
        const z = Viewport.fitEntryZoom(aw, ah, path, slots, focus.focusY);
        const box = this._playBBox(path, slots);
        Game._setZoom(z, box.cx, box.cy);
    },

    _measureChrome() {
        const bar = document.getElementById("general-bar");
        const topbar = document.querySelector(".topbar");
        const barH = bar ? bar.offsetHeight : 80;
        const topH = topbar ? topbar.offsetHeight : 72;
        return { barH, topH };
    },

    /** 顶栏与底栏之间的可视焦点（相对 canvas 高度 0~1） */
    _getPlayfieldFocus() {
        const canvas = document.getElementById("canvas");
        const topbar = document.querySelector(".topbar");
        const bar = document.getElementById("general-bar");
        if (!canvas) {
            const h = window.innerHeight || 600;
            const fy = window.Viewport ? Viewport.estimatePlayfieldFocusY(h) : 0.52;
            return { focusX: 0.5, focusY: fy };
        }
        const cR = canvas.getBoundingClientRect();
        const top = topbar
            ? Math.max(0, topbar.getBoundingClientRect().bottom - cR.top + 4)
            : Math.min(108, cR.height * 0.25);
        const bottom = bar
            ? Math.max(0, cR.bottom - bar.getBoundingClientRect().top + 6)
            : Math.min(104, cR.height * 0.24);
        const safeH = Math.max(48, cR.height - top - bottom);
        const focusY = U.clamp((top + safeH * 0.5) / Math.max(1, cR.height), 0.42, 0.62);
        return { focusX: 0.5, focusY, top, bottom, canvasH: cR.height };
    },

    _playBBox(path, slots) {
        return window.Viewport
            ? Viewport.playContentBBox(path, slots)
            : this._pathBBox(path, 1);
    },

    /** 路径前段包围盒（出兵线区域） */
    _pathBBox(path, fraction) {
        if (!path || !path.length) return { minX: 0, minY: 0, maxX: 960, maxY: 600, cx: 480, cy: 300 };
        const n = Math.max(2, Math.ceil(path.length * (fraction || 0.55)));
        let minX = path[0].x, maxX = path[0].x, minY = path[0].y, maxY = path[0].y;
        for (let i = 0; i < n; i++) {
            const p = path[i];
            minX = Math.min(minX, p.x);
            maxX = Math.max(maxX, p.x);
            minY = Math.min(minY, p.y);
            maxY = Math.max(maxY, p.y);
        }
        return {
            minX, minY, maxX, maxY,
            cx: (minX + maxX) * 0.5,
            cy: (minY + maxY) * 0.5
        };
    },

    _pathViewFocal(path) {
        const b = this._pathBBox(path, 0.4);
        return { x: b.cx, y: b.cy };
    },

    /** 矮宽外屏进关默认缩放：让路径前段完整落在视野内 */
    _defaultLevelZoom(path) {
        const pad = 88;
        const box = this._pathBBox(path, 0.55);
        const bw = Math.max(220, box.maxX - box.minX + pad * 2);
        const bh = Math.max(180, box.maxY - box.minY + pad * 2);
        const zx = 960 / bw;
        const zy = 600 / bh;
        return U.clamp(Math.min(zx, zy), 1.12, 1.48);
    },

    _resizeCanvas() {
        const canvas = document.getElementById("canvas");
        if (!canvas) return;
        const container = canvas.parentElement;
        const gameScreen = document.getElementById("game-screen");
        if (!container) return;

        /* 战斗区按 game-screen 全屏尺寸，顶栏/底栏为浮层不再占高度 */
        const availW = Math.max(1, gameScreen ? gameScreen.clientWidth : container.clientWidth);
        const availH = Math.max(1, gameScreen ? gameScreen.clientHeight : container.clientHeight);
        const layout = window.Viewport
            ? Viewport.computeLayout(availW, availH, { mobile: this._isMobile })
            : null;

        let w, h, isShortPlayfield;
        if (layout) {
            this._layoutMode = layout.layoutMode;
            this._shortPlayfield = layout.shortPlayfield;
            this._ultraWide = !!layout.ultraWide;
            this._wideDefaultZoom = layout.wideDefaultZoom;
            w = layout.cssW;
            h = layout.cssH;
            isShortPlayfield = layout.shortPlayfield;
        } else {
            const aspect = availW / availH;
            isShortPlayfield = this._isMobile || availH < 380 || (availH / availW) < 0.42;
            const useCover = aspect >= 2.0 && !isShortPlayfield;
            this._shortPlayfield = isShortPlayfield;
            if (isShortPlayfield) {
                this._layoutMode = "fill-pan";
                this._wideDefaultZoom = 1;
                w = Math.floor(availW);
                h = Math.floor(availH);
            } else if (useCover) {
                const scale = Math.max(availW / 960, availH / 600);
                const containScale = Math.min(availW / 960, availH / 600);
                this._layoutMode = "cover";
                this._wideDefaultZoom = U.clamp(scale / containScale, 1, Game.MAX_ZOOM);
                w = Math.floor(960 * scale);
                h = Math.floor(600 * scale);
            } else {
                this._layoutMode = "fill-pan";
                this._wideDefaultZoom = 1;
                w = Math.floor(availW);
                h = Math.floor(availH);
            }
        }

        canvas.style.width = w + "px";
        canvas.style.height = h + "px";
        canvas.style.marginTop = "0";
        canvas.style.marginLeft = "0";
        canvas.style.marginRight = "0";
        canvas.style.maxWidth = isShortPlayfield ? "100%" : "";
        canvas.dataset.layoutMode = this._layoutMode;
        if (container.dataset) {
            container.dataset.layoutMode = this._layoutMode;
        }

        this._updateZoomChrome();

        if (typeof Game !== "undefined" && Game.syncCanvasResolution) {
            Game.syncCanvasResolution(w, h);
        }

        if (isShortPlayfield && typeof Game !== "undefined") {
            const self = this;
            requestAnimationFrame(function() {
                const rw = Math.max(1, Math.floor(canvas.clientWidth));
                const rh = Math.max(1, Math.floor(canvas.clientHeight));
                if (Math.abs(rw - w) > 2 || Math.abs(rh - h) > 2) {
                    if (Game.syncCanvasResolution) Game.syncCanvasResolution(rw, rh);
                    if (Game._clampPan) Game._clampPan();
                }
            });
        }

        if (typeof Game !== "undefined" && Game._clampPan) {
            Game._clampPan();
            if (window.UI && UI.layoutTowerPanelDock) {
                UI.layoutTowerPanelDock();
            } else if (window.UI && UI.refreshTowerPanel && Game.selectedTower) {
                UI.refreshTowerPanel();
            }
        }
    },

    getMinZoom() {
        if (typeof Game !== "undefined" && Game._minZoom) return Game._minZoom();
        if (window.Viewport) return Viewport.getMinZoom();
        if (this._shortPlayfield) return 0.55;
        return typeof Game !== "undefined" ? Game.MIN_ZOOM : 1;
    },

    _zoomFocal() {
        if (Game && Game.level && Game.level.path && Game.level.path.length) {
            return this._pathViewFocal(Game.level.path);
        }
        return { x: 480, y: 300 };
    },

    zoomIn() {
        if (!Game || !Game.level) return;
        const f = this._zoomFocal();
        Game._setZoom(Game.view.zoom * this.ZOOM_STEP, f.x, f.y);
    },

    zoomOut() {
        if (!Game || !Game.level) return;
        const f = this._zoomFocal();
        const minZ = this.getMinZoom();
        let guard = 0;
        while (Game.view.zoom > minZ * 1.008 && guard++ < 10) {
            Game._setZoom(Math.max(minZ, Game.view.zoom / this.ZOOM_STEP), f.x, f.y);
        }
        if (Game.view.zoom <= minZ * 1.03) {
            this._panFitFullContent(Game.level.path);
        }
    },

    /** 缩到最小时：道路+塔位完整落入视野 */
    _panFitFullContent(path) {
        if (!Game || !path || !path.length) return;
        const box = Viewport.playContentBBox(path, Game.level && Game.level.slots);
        const pad = 44;
        const { vw, vh } = Game._viewFrame();
        Game.view.panX = U.clamp(box.minX - pad, 0, Math.max(0, Game.WORLD_W - vw));
        Game.view.panY = U.clamp(box.minY - pad, 0, Math.max(0, Game.WORLD_H - vh));
        Game._clampPan();
    },

    zoomReset() {
        this.applyLevelViewport();
    },

    _updateZoomChrome() {
        const show = this._isMobile || this._shortPlayfield;
        const bar = document.getElementById("map-zoom-controls");
        const hint = document.getElementById("map-view-hint");
        if (bar) bar.classList.toggle("hidden", !show);
        if (hint) hint.classList.toggle("hidden", !show);
    },

    _bindZoomControls() {
        const self = this;
        const out = document.getElementById("btn-zoom-out");
        const inn = document.getElementById("btn-zoom-in");
        const rst = document.getElementById("btn-zoom-reset");
        if (out) out.onclick = function(e) { e.preventDefault(); self.zoomOut(); };
        if (inn) inn.onclick = function(e) { e.preventDefault(); self.zoomIn(); };
        if (rst) rst.onclick = function(e) { e.preventDefault(); self.zoomReset(); };
    },

    _panFillPathStart(path) {
        if (!Game || !path || !path.length) return;
        const focus = this._getPlayfieldFocus();
        const box = this._playBBox(path, Game.level && Game.level.slots);
        const { vw, vh } = Game._viewFrame();
        const cx = (box.minX + box.maxX) * 0.5;
        const cy = (box.minY + box.maxY) * 0.5;
        Game.view.panX = U.clamp(cx - vw * focus.focusX, 0, Math.max(0, Game.WORLD_W - vw));
        Game.view.panY = U.clamp(cy - vh * focus.focusY, 0, Math.max(0, Game.WORLD_H - vh));
        Game._clampPan();
    },

    _refineLevelViewport(path) {
        if (!path || !path.length) return;
        this._panFillPathStart(path);
    },

    /** 进关镜头：聚焦道路+塔位（居中在背景中部） */
    applyLevelViewport() {
        this._resizeCanvas();
        if (typeof Game === "undefined") return;
        Game.resetView();
        const path = Game.level && Game.level.path;
        if (!path || !path.length) return;
        if (this._shortPlayfield || this._isUltraWide()) {
            this._applyEntryZoom(path);
            this._panFillPathStart(path);
        } else {
            const focal = this._pathViewFocal(path);
            if (this._layoutMode === "cover" && this._wideDefaultZoom > 1.01) {
                Game._setZoom(this._wideDefaultZoom, focal.x, focal.y);
            }
            return;
        }
        const self = this;
        requestAnimationFrame(function() {
            requestAnimationFrame(function() {
                if (typeof Game === "undefined" || !Game.level) return;
                self._refineLevelViewport(Game.level.path);
            });
        });
    },

    onViewportChanged() {
        this._resizeCanvas();
        if (typeof Game !== "undefined" && Game.level && Game.state === "running") {
            this.applyLevelViewport();
        }
    },

    _getPseudoEvent(touch) {
        return { clientX: touch.clientX, clientY: touch.clientY };
    },

    _touchDist(t0, t1) {
        const dx = t0.clientX - t1.clientX;
        const dy = t0.clientY - t1.clientY;
        return Math.sqrt(dx * dx + dy * dy);
    },

    _touchMid(t0, t1) {
        return {
            clientX: (t0.clientX + t1.clientX) * 0.5,
            clientY: (t0.clientY + t1.clientY) * 0.5
        };
    },

    _clearLongPress() {
        if (this._longPressTimer) {
            clearTimeout(this._longPressTimer);
            this._longPressTimer = null;
        }
    },

    _findTouch(touches, id) {
        for (let i = 0; i < touches.length; i++) {
            if (touches[i].identifier === id) return touches[i];
        }
        return null;
    },

    _tryDoubleTap(touch) {
        const now = Date.now();
        const x = touch.clientX;
        const y = touch.clientY;
        if (this._lastTapPos && now - this._lastTapTime < 320) {
            const dx = x - this._lastTapPos.x;
            const dy = y - this._lastTapPos.y;
            if (Math.sqrt(dx * dx + dy * dy) < 24) {
                if (Game) {
                    if (this._shortPlayfield && Game.level) this.applyLevelViewport();
                    else if (Game.resetView) Game.resetView();
                }
                this._lastTapTime = 0;
                this._lastTapPos = null;
                return true;
            }
        }
        this._lastTapTime = now;
        this._lastTapPos = { x, y };
        return false;
    },

    _onTouchStart(ev) {
        ev.preventDefault();
        if (ev.touches.length >= 2) {
            this._clearLongPress();
            this._gesture = "pinch";
            this._touchId = null;
            this._suppressClick = true;
            const t0 = ev.touches[0];
            const t1 = ev.touches[1];
            this._pinchStartDist = this._touchDist(t0, t1);
            this._pinchStartZoom = Game ? Game.view.zoom : 1;
            this._pinchLastMid = this._touchMid(t0, t1);
            return;
        }

        if (ev.changedTouches.length === 0) return;
        const touch = ev.changedTouches[0];
        this._touchId = touch.identifier;
        this._touchStartPos = { x: touch.clientX, y: touch.clientY };
        this._gesture = "tap";
        this._suppressClick = false;
        this._panLastWorld = null;
        this._lastTouchTime = Date.now();

        if (Game && Game._onMove) {
            Game._onMove(this._getPseudoEvent(touch));
        }

        this._clearLongPress();
        this._longPressTimer = setTimeout(() => {
            if (this._gesture === "tap" && Game && Game._cancelSelect) {
                Game._cancelSelect();
                if (window.Native && Native.vibrate) Native.vibrate(30);
            }
            this._longPressTimer = null;
        }, 500);
    },

    _onTouchMove(ev) {
        ev.preventDefault();

        if (ev.touches.length >= 2 && Game) {
            this._gesture = "pinch";
            this._clearLongPress();
            this._suppressClick = true;
            const t0 = ev.touches[0];
            const t1 = ev.touches[1];
            const dist = this._touchDist(t0, t1);
            const mid = this._touchMid(t0, t1);
            if (this._pinchStartDist > 0) {
                const focal = Game._screenToWorld(mid.clientX, mid.clientY);
                const ratio = dist / this._pinchStartDist;
                Game._setZoom(this._pinchStartZoom * ratio, focal.x, focal.y);
            }
            if (this._pinchLastMid) {
                const w0 = Game._screenToWorld(this._pinchLastMid.clientX, this._pinchLastMid.clientY);
                const w1 = Game._screenToWorld(mid.clientX, mid.clientY);
                Game._applyPan(w0.x - w1.x, w0.y - w1.y);
            }
            this._pinchLastMid = mid;
            return;
        }

        if (this._touchId === null) return;
        const touch = this._findTouch(ev.touches, this._touchId);
        if (!touch) return;

        if (this._touchStartPos) {
            const dx = touch.clientX - this._touchStartPos.x;
            const dy = touch.clientY - this._touchStartPos.y;
            const dist = Math.sqrt(dx * dx + dy * dy);
            if (dist > 8) {
                this._clearLongPress();
                if (Game && (Game.view.zoom > 1.01 || this._layoutMode === "cover" || this._layoutMode === "fill-pan")) {
                    if (this._gesture === "tap") {
                        this._gesture = "pan";
                        this._suppressClick = true;
                        this._panLastWorld = Game._screenToWorld(touch.clientX, touch.clientY);
                    }
                }
            }
        }

        if (this._gesture === "pan" && Game && this._panLastWorld) {
            const world = Game._screenToWorld(touch.clientX, touch.clientY);
            Game._applyPan(this._panLastWorld.x - world.x, this._panLastWorld.y - world.y);
            this._panLastWorld = Game._screenToWorld(touch.clientX, touch.clientY);
            return;
        }

        if (Game && Game._onMove) {
            Game._onMove(this._getPseudoEvent(touch));
        }
    },

    _onTouchEnd(ev) {
        ev.preventDefault();

        if (ev.touches.length >= 1) {
            return;
        }

        this._clearLongPress();

        if (this._gesture === "pinch") {
            this._gesture = null;
            this._pinchStartDist = 0;
            this._pinchLastMid = null;
            this._touchId = null;
            if (Game && Game.level && Game.view.zoom <= this.getMinZoom() * 1.03) {
                this._panFitFullContent(Game.level.path);
            }
            return;
        }

        if (this._touchId === null) return;

        const touch = ev.changedTouches[0];
        const wasPan = this._gesture === "pan" || this._suppressClick;

        if (!wasPan && touch && Game) {
            if (this._tryDoubleTap(touch)) {
                this._touchId = null;
                this._gesture = null;
                return;
            }
            const now = Date.now();
            if (now - this._lastTouchTime >= 80) {
                Game._onMove(this._getPseudoEvent(touch));
                Game._onClick(this._getPseudoEvent(touch));
            }
        }

        this._touchId = null;
        this._gesture = null;
        this._suppressClick = false;
        this._panLastWorld = null;
    },

    onPause() {
        if (typeof Game === "undefined") return;
        if (Game.state === "running") {
            Game.pause();
            var pm = document.getElementById("pause-mask");
            if (pm) pm.classList.remove("hidden");
        }
    },

    onResume() {},

    onBackPressed() {
        var pauseMask = document.getElementById("pause-mask");
        var resultMask = document.getElementById("result-mask");
        var towerPanel = document.getElementById("tower-panel");

        if (pauseMask && !pauseMask.classList.contains("hidden")) {
            Game.resume();
            pauseMask.classList.add("hidden");
            return;
        }
        if (resultMask && !resultMask.classList.contains("hidden")) {
            resultMask.classList.add("hidden");
            UI.show("menu");
            return;
        }
        if (towerPanel && !towerPanel.classList.contains("hidden")) {
            Game._cancelSelect();
            return;
        }
        if (Game.mergePickSource) {
            Game.mergePickSource = null;
            if (Game.onUpdate) Game.onUpdate();
            return;
        }
        if (Game.placingGeneral) {
            Game._cancelSelect();
            return;
        }
        var gameScreen = document.getElementById("game-screen");
        if (gameScreen && gameScreen.classList.contains("active")) {
            if (Game.state === "running") {
                Game.pause();
                pauseMask.classList.remove("hidden");
            }
            return;
        }
        var lineup = document.getElementById("lineup-screen");
        if (lineup && lineup.classList.contains("active")) {
            UI.show("level-select");
            return;
        }
        var levelSelect = document.getElementById("level-select");
        if (levelSelect && levelSelect.classList.contains("active")) {
            UI.show("menu");
            return;
        }
        var codex = document.getElementById("codex");
        if (codex && codex.classList.contains("active")) {
            UI.show("menu");
            return;
        }
        var credits = document.getElementById("credits");
        if (credits && credits.classList.contains("active")) {
            UI.show("menu");
            return;
        }
        var settings = document.getElementById("settings");
        if (settings && settings.classList.contains("active")) {
            if (window.UI && UI._settingsReturnPause) {
                UI._settingsReturnPause = false;
                UI.show("game-screen");
                if (pauseMask) pauseMask.classList.remove("hidden");
            } else if (window.UI) {
                UI.show("menu");
            }
            return;
        }
        var menu = document.getElementById("menu");
        if (menu && menu.classList.contains("active")) {
            if (window.Native && Native.exitApp) Native.exitApp();
            return;
        }
        if (window.UI) UI.show("menu");
    },

    init() {
        var self = this;
        var canvas = document.getElementById("canvas");
        if (!canvas) return;

        this._bindZoomControls();
        this._resizeCanvas();
        var onLayout = function() { self._resizeCanvas(); };
        window.addEventListener("resize", onLayout);
        if (window.visualViewport) {
            window.visualViewport.addEventListener("resize", onLayout);
            window.visualViewport.addEventListener("scroll", onLayout);
        }

        if (!this._isMobile) return;

        canvas.addEventListener("touchstart", function(e) { self._onTouchStart(e); }, { passive: false });
        canvas.addEventListener("touchmove", function(e) { self._onTouchMove(e); }, { passive: false });
        canvas.addEventListener("touchend", function(e) { self._onTouchEnd(e); }, { passive: false });
        canvas.addEventListener("touchcancel", function(e) {
            e.preventDefault();
            self._clearLongPress();
            self._touchId = null;
            self._gesture = null;
            self._suppressClick = false;
        }, { passive: false });

        document.addEventListener("touchmove", function(e) {
            if (e.target === canvas || canvas.contains(e.target)) {
                e.preventDefault();
            }
        }, { passive: false });

        var hints = document.querySelectorAll(".hint");
        for (var i = 0; i < hints.length; i++) hints[i].style.display = "none";
    }
};
