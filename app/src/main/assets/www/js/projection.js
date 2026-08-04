/**
 * 2.5D 斜投影系统 — 倾斜俯视（3/4 视角）
 *
 * 设计：
 *   X 轴不变（横向仍为横向）
 *   Y 轴压缩 Y_SQUASH 倍（模拟相机前倾）
 *   Z 轴（高度）沿屏幕 Y 轴向上偏移
 *
 * 游戏逻辑层零改动：所有碰撞/射程/寻路仍用原始 {x, y}。
 * 本模块仅在渲染端提供投影变换和高度偏移。
 *
 * 详见 docs/2.5D_PLAN.md
 */
window.Projection = {

    // ===== 投影参数 =====

    /** 地面 Y 压缩比 — 仅用于参考计算，不用于全局变换 */
    Y_SQUASH: 1.0,

    /** 世界 Z 高度 → 屏幕像素偏移系数（加大，因为无全局压缩） */
    HEIGHT_SCALE: 1.0,

    /** 阴影透明度（加大以在无压缩模式下更明显） */
    SHADOW_ALPHA: 0.42,

    /** 阴影水平偏移（模拟光源方向，正值=右） */
    SHADOW_OFFSET_X: 5,

    /** 阴影垂直偏移（模拟光源高度） */
    SHADOW_OFFSET_Y: 8,

    /** 是否启用 2.5D 投影（可在设置中关闭以回退纯俯视） */
    enabled: true,

    // ===== 坐标变换 =====

    /**
     * 世界坐标 (x, y, z) → 屏幕相对坐标（未含 zoom/pan）
     * @returns {{ sx: number, sy: number }}
     */
    project(x, y, z) {
        if (z === undefined) z = 0;
        return {
            sx: x,
            sy: y * this.Y_SQUASH - z * this.HEIGHT_SCALE
        };
    },

    /**
     * 屏幕坐标 → 世界地面坐标 (z=0)，用于鼠标/触摸输入
     * 输入为 "未缩放" 的世界空间坐标（即已经除以 zoom、减去 pan 之后的值）
     * @returns {{ x: number, y: number }}
     */
    unproject(sx, sy) {
        return {
            x: sx,
            y: sy / this.Y_SQUASH
        };
    },

    // ===== 画布变换矩阵 =====

    /**
     * 设置画布变换矩阵，包含投影 + 缩放 + 平移。
     *
     * 等效于：先将世界坐标做斜投影，再缩放 zoom 倍，再平移 -pan。
     *
     * 矩阵 [a, b, c, d, e, f]：
     *   screenX = a*x + c*y + e
     *   screenY = b*x + d*y + f
     *
     * 投影后：sx = x, sy = y * Y_SQUASH
     * 加缩放平移后：
     *   screenX = x * zoom - panX * zoom
     *   screenY = y * Y_SQUASH * zoom - panY * Y_SQUASH * zoom
     *
     * @param {CanvasRenderingContext2D} ctx
     * @param {number} zoom
     * @param {number} panX
     * @param {number} panY
     */
    applyTransform(ctx, zoom, panX, panY) {
        if (!this.enabled) {
            ctx.setTransform(zoom, 0, 0, zoom, -panX * zoom, -panY * zoom);
            return;
        }
        const sq = this.Y_SQUASH;
        ctx.setTransform(
            zoom,         // a: x → screenX 系数
            0,            // b: x → screenY 系数（无耦合）
            0,            // c: y → screenX 系数（无耦合）
            zoom * sq,    // d: y → screenY 系数（压缩）
            -panX * zoom, // e: X 平移
            -panY * zoom * sq // f: Y 平移（也要压缩）
        );
    },

    /**
     * 计算在当前变换矩阵下，沿世界 Y 轴平移多少才能在屏幕上产生
     * 指定像素的垂直偏移。
     *
     * 用于在 drawXxx() 中实现高度抬升：
     *   ctx.translate(0, Projection.worldYForScreenOffset(-pixelOffset))
     *
     * @param {number} screenDy 期望的屏幕垂直偏移（正=下，负=上）
     * @param {number} zoom 当前缩放
     * @returns {number} 对应的世界 Y 平移量
     */
    worldYForScreenOffset(screenDy, zoom) {
        if (!this.enabled) return screenDy / zoom;
        return screenDy / (zoom * this.Y_SQUASH);
    },

    /**
     * 在实体位置绘制高度抬升后的偏移量（世界单位）。
     *
     * 用法：
     *   ctx.save();
     *   ctx.translate(tower.x, tower.y);      // 地面位置
     *   Art._drawShadow(ctx, radius);          // 影子画在地面上
     *   ctx.translate(0, Projection.heightOffset(tower.height, zoom)); // 抬升
     *   // ... 画立体本体 ...
     *   ctx.restore();
     *
     * @param {number} height 世界高度单位
     * @param {number} zoom 当前缩放
     * @returns {number} 世界 Y 偏移（负值=向上）
     */
    heightOffset(height, zoom) {
        if (!this.enabled || !height) return 0;
        // 在无全局压缩模式下，直接用 HEIGHT_SCALE 作为像素偏移系数
        // 负值 = 向上（屏幕 Y 轴向下为正）
        return -height * this.HEIGHT_SCALE;
    },

    // ===== 视口计算辅助 =====

    /**
     * 计算世界包围盒在投影后的屏幕宽高（用于 fitZoom 计算）。
     * @param {number} worldW  世界宽
     * @param {number} worldH  世界高
     * @returns {{ w: number, h: number }} 投影后宽高
     */
    projectedSize(worldW, worldH) {
        if (!this.enabled) return { w: worldW, h: worldH };
        return { w: worldW, h: worldH * this.Y_SQUASH };
    },

    /**
     * 给定可用屏幕空间，计算让世界完全可见所需的最小缩放。
     * @param {number} availW
     * @param {number} availH
     * @param {number} worldW
     * @param {number} worldH
     * @returns {number} zoom
     */
    fitZoom(availW, availH, worldW, worldH) {
        const ps = this.projectedSize(worldW, worldH);
        return Math.min(availW / ps.w, availH / ps.h);
    },

    // ===== 阴影绘制 =====

    /**
     * 在当前 ctx 原点（地面位置）绘制椭圆地面阴影。
     * @param {CanvasRenderingContext2D} ctx
     * @param {number} rx     水平半径
     * @param {number} ry     垂直半径（通常 = rx * 0.35~0.45）
     * @param {number} [alpha]  透明度覆盖（默认 SHADOW_ALPHA）
     * @param {number} [offsetY] Y 偏移（正值向下）
     */
    drawShadow(ctx, rx, ry, alpha, offsetY) {
        if (!this.enabled) return; // 纯俯视时不画额外阴影（原有底盘已含）
        if (alpha === undefined) alpha = this.SHADOW_ALPHA;
        if (offsetY === undefined) offsetY = 0;
        ctx.save();
        // 方向性偏移：模拟斜上方光源投射
        const ox = this.SHADOW_OFFSET_X;
        const oy = this.SHADOW_OFFSET_Y + offsetY;
        ctx.fillStyle = `rgba(0,0,0,${alpha})`;
        ctx.beginPath();
        ctx.ellipse(ox, oy, rx, ry, 0, 0, Math.PI * 2);
        ctx.fill();
        // 内层更深的阴影核心
        ctx.fillStyle = `rgba(0,0,0,${alpha * 0.5})`;
        ctx.beginPath();
        ctx.ellipse(ox * 0.5, oy * 0.5, rx * 0.6, ry * 0.6, 0, 0, Math.PI * 2);
        ctx.fill();
        ctx.restore();
    },

    // ===== 实体高度查表 =====

    /**
     * 根据实体类型和等级获取建议高度（世界单位）。
     * @param {string} kind 'tower' | 'enemy' | 'obstacle' | 'boss'
     * @param {object} [ctx] 附加上下文 { level, mergeTier, type }
     * @returns {number}
     */
    heightFor(kind, ctx) {
        ctx = ctx || {};
        switch (kind) {
            case "tower": {
                const lv = ctx.level || 1;
                const mt = ctx.mergeTier || 0;
                return 14 + lv * 4 + mt * 5;  // 适中高度，避免遮挡
            }
            case "enemy": {
                const weapon = ctx.weapon;
                if (weapon === "boss") return 20;
                if (weapon === "siege") return 14;
                if (weapon === "baggage") return 10;
                return 8 + (ctx.size ? Math.min(6, ctx.size * 0.2) : 0);
            }
            case "obstacle":
                return ctx.kind === "rock" ? 14 : 10;
            default:
                return 0;
        }
    },

    /**
     * 绘制"垂直侧面"：从地面向上延伸的梯形墙体，给 2D 精灵立体厚度。
     *
     * 用于塔底盘、障碍物等需要"底座厚度"的场景。
     *
     * @param {CanvasRenderingContext2D} ctx  （已 translate 到实体地面位置）
     * @param {number} halfW    底面半宽
     * @param {number} halfD    底面半深
     * @param {number} height   侧面高度（世界单位）
     * @param {string} topColor 顶面颜色
     * @param {string} sideColor 侧面颜色（通常 = topColor 的暗化版）
     */
    drawBaseExtrusion(ctx, halfW, halfD, height, topColor, sideColor) {
        if (!this.enabled || height <= 0) return;
        const liftY = this.heightOffset(height, 1);

        // 前侧面（梯形：底宽 → 顶宽略窄，模拟透视）
        ctx.fillStyle = sideColor;
        ctx.beginPath();
        ctx.moveTo(-halfW, halfD);
        ctx.lineTo(halfW, halfD);
        ctx.lineTo(halfW * 0.88, halfD + liftY);
        ctx.lineTo(-halfW * 0.88, halfD + liftY);
        ctx.closePath();
        ctx.fill();

        // 右侧面
        ctx.fillStyle = U.shade ? U.shade(sideColor, -12) : sideColor;
        ctx.beginPath();
        ctx.moveTo(halfW, halfD);
        ctx.lineTo(halfW, -halfD);
        ctx.lineTo(halfW * 0.88, -halfD + liftY);
        ctx.lineTo(halfW * 0.88, halfD + liftY);
        ctx.closePath();
        ctx.fill();

        // 顶面（椭圆，模拟透视压缩）
        ctx.fillStyle = topColor;
        ctx.beginPath();
        ctx.ellipse(0, halfD + liftY, halfW * 0.9, halfD * 0.55, 0, 0, Math.PI * 2);
        ctx.fill();
    }
};
