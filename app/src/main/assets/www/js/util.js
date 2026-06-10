window.U = {
    dist(ax, ay, bx, by) { const dx = ax - bx, dy = ay - by; return Math.hypot(dx, dy); },
    lerp(a, b, t) { return a + (b - a) * t; },
    clamp(v, mn, mx) { return Math.max(mn, Math.min(mx, v)); },
    rand(a, b) { return a + Math.random() * (b - a); },
    rint(a, b) { return Math.floor(a + Math.random() * (b - a + 1)); },
    angleTo(ax, ay, bx, by) { return Math.atan2(by - ay, bx - ax); },
    // 沿路径插值得到坐标，progress 是 [0, totalLen] 距离
    pointOnPath(path, progress) {
        let remain = progress;
        for (let i = 0; i < path.length - 1; i++) {
            const a = path[i], b = path[i + 1];
            const seg = U.dist(a.x, a.y, b.x, b.y);
            if (remain <= seg) {
                const t = remain / seg;
                return {
                    x: a.x + (b.x - a.x) * t,
                    y: a.y + (b.y - a.y) * t,
                    angle: Math.atan2(b.y - a.y, b.x - a.x)
                };
            }
            remain -= seg;
        }
        const last = path[path.length - 1], prev = path[path.length - 2];
        return { x: last.x, y: last.y, angle: Math.atan2(last.y - prev.y, last.x - prev.x) };
    },
    pathLength(path) {
        let len = 0;
        for (let i = 0; i < path.length - 1; i++) len += U.dist(path[i].x, path[i].y, path[i + 1].x, path[i + 1].y);
        return len;
    },
    // 将 hex 颜色调整亮度
    shade(hex, amt) {
        const c = hex.replace("#", "");
        const num = parseInt(c, 16);
        let r = (num >> 16) + amt;
        let g = ((num >> 8) & 0xff) + amt;
        let b = (num & 0xff) + amt;
        r = U.clamp(r, 0, 255); g = U.clamp(g, 0, 255); b = U.clamp(b, 0, 255);
        return "#" + ((r << 16) | (g << 8) | b).toString(16).padStart(6, "0");
    }
};
