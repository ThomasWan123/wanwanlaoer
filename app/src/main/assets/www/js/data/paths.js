// 30 套关卡路径布局（含现有关卡 3 条基底）
window.LEGACY_PATH_BUNDLES = [
    {
        path: [
            { x: 0, y: 120 }, { x: 140, y: 120 }, { x: 140, y: 200 }, { x: 220, y: 200 },
            { x: 220, y: 260 }, { x: 400, y: 260 }, { x: 400, y: 180 }, { x: 480, y: 180 },
            { x: 480, y: 260 }, { x: 600, y: 260 }, { x: 600, y: 120 }, { x: 720, y: 120 },
            { x: 720, y: 300 }, { x: 820, y: 300 }, { x: 820, y: 420 }, { x: 280, y: 420 },
            { x: 280, y: 520 }, { x: 960, y: 520 }
        ],
        slots: [
            { x: 120, y: 220 }, { x: 320, y: 200 }, { x: 380, y: 340 }, { x: 560, y: 200 },
            { x: 620, y: 340 }, { x: 820, y: 220 }, { x: 820, y: 380 }, { x: 180, y: 420 },
            { x: 480, y: 460 }, { x: 660, y: 480 }
        ],
        obstacles: [
            { x: 400, y: 200, hp: 75, gold: 38, kind: "barrel" },
            { x: 740, y: 300, hp: 120, gold: 65, kind: "rock" },
            { x: 600, y: 200, hp: 55, gold: 32, kind: "barrel" }
        ]
    },
    {
        path: [
            { x: 0, y: 80 }, { x: 200, y: 80 }, { x: 200, y: 200 }, { x: 360, y: 200 },
            { x: 360, y: 320 }, { x: 220, y: 320 }, { x: 220, y: 400 }, { x: 100, y: 400 },
            { x: 100, y: 500 }, { x: 420, y: 500 }, { x: 420, y: 420 }, { x: 600, y: 420 },
            { x: 600, y: 280 }, { x: 720, y: 280 }, { x: 720, y: 220 }, { x: 860, y: 220 },
            { x: 860, y: 400 }, { x: 920, y: 400 }, { x: 920, y: 540 }, { x: 960, y: 540 }
        ],
        slots: [
            { x: 200, y: 180 }, { x: 460, y: 180 }, { x: 240, y: 420 }, { x: 460, y: 380 },
            { x: 460, y: 460 }, { x: 720, y: 140 }, { x: 720, y: 380 }, { x: 760, y: 460 },
            { x: 580, y: 580 }, { x: 360, y: 580 }, { x: 60, y: 220 }, { x: 60, y: 420 }
        ],
        obstacles: [
            { x: 240, y: 140, hp: 95, gold: 48, kind: "barrel" },
            { x: 600, y: 400, hp: 145, gold: 75, kind: "rock" },
            { x: 860, y: 320, hp: 105, gold: 55, kind: "barrel" },
            { x: 420, y: 460, hp: 110, gold: 58, kind: "rock" }
        ]
    },
    {
        path: [
            { x: 0, y: 300 }, { x: 120, y: 300 }, { x: 120, y: 180 }, { x: 260, y: 180 },
            { x: 260, y: 100 }, { x: 460, y: 100 }, { x: 460, y: 220 }, { x: 380, y: 220 },
            { x: 380, y: 300 }, { x: 460, y: 300 }, { x: 520, y: 300 }, { x: 520, y: 400 },
            { x: 320, y: 400 }, { x: 320, y: 500 }, { x: 560, y: 500 }, { x: 560, y: 420 },
            { x: 720, y: 420 }, { x: 720, y: 260 }, { x: 800, y: 260 }, { x: 800, y: 200 },
            { x: 860, y: 200 }, { x: 860, y: 360 }, { x: 920, y: 360 }, { x: 920, y: 460 },
            { x: 960, y: 460 }
        ],
        slots: [
            { x: 80, y: 200 }, { x: 80, y: 420 }, { x: 280, y: 200 }, { x: 380, y: 200 },
            { x: 580, y: 200 }, { x: 580, y: 420 }, { x: 420, y: 420 }, { x: 220, y: 420 },
            { x: 800, y: 100 }, { x: 800, y: 360 }, { x: 660, y: 320 }, { x: 760, y: 580 },
            { x: 460, y: 580 }, { x: 200, y: 580 }
        ],
        obstacles: [
            { x: 310, y: 100, hp: 125, gold: 68, kind: "barrel" },
            { x: 460, y: 240, hp: 175, gold: 92, kind: "rock" },
            { x: 720, y: 360, hp: 145, gold: 78, kind: "barrel" },
            { x: 580, y: 500, hp: 195, gold: 100, kind: "rock" },
            { x: 200, y: 400, hp: 130, gold: 72, kind: "barrel" }
        ]
    }
];

window.PathGen = {
    WORLD_W: 960,
    WORLD_H: 600,

    /** 将道路+塔位+障碍整体移到战场垂直中部（保持 x=0 出兵、x=960 终点） */
    _centerPlayInWorld(bundle) {
        const H = this.WORLD_H;
        const padY = 56;
        const groups = [
            bundle.path,
            bundle.slots,
            bundle.obstacles || []
        ];
        let minY = Infinity;
        let maxY = -Infinity;
        for (let g = 0; g < groups.length; g++) {
            const list = groups[g];
            for (let i = 0; i < list.length; i++) {
                minY = Math.min(minY, list[i].y);
                maxY = Math.max(maxY, list[i].y);
            }
        }
        if (!isFinite(minY)) return bundle;
        const cy = (minY + maxY) * 0.5;
        let dy = Math.round(H * 0.5 - cy);
        dy = U.clamp(dy, padY - minY, (H - padY) - maxY);
        if (Math.abs(dy) < 1) return bundle;

        const shiftY = (p) => ({
            ...p,
            y: Math.round(U.clamp(p.y + dy, padY, H - padY))
        });
        const path = bundle.path.map((p, i) => {
            const q = shiftY(p);
            if (i === 0) return { x: 0, y: q.y };
            if (i === bundle.path.length - 1) return { x: this.WORLD_W, y: q.y };
            return q;
        });
        return {
            path,
            slots: bundle.slots.map(shiftY),
            obstacles: (bundle.obstacles || []).map(shiftY)
        };
    },

    _waypoints(seed) {
        const s = seed * 17 + 3;
        const n = 14 + (s % 6);
        /* 在战场中部生成路径，避免贴顶/贴底 */
        const startY = 240 + (s * 13) % 120;
        const pts = [{ x: 0, y: startY }];
        let x = pts[0].x, y = pts[0].y;
        const yMin = 150;
        const yMax = 450;
        for (let i = 1; i < n; i++) {
            const horiz = (s + i) % 3 !== 0;
            const step = 70 + ((s * 31 + i * 47) % 90);
            if (horiz) {
                x = Math.min(880, x + step);
                if ((s + i) % 4 === 0) {
                    y = U.clamp(y + (((s + i) % 2) ? 72 : -72), yMin, yMax);
                }
            } else {
                y = U.clamp(y + (((s + i) % 2) ? step * 0.65 : -step * 0.65), yMin, yMax);
                if ((s + i) % 5 === 0) x = U.clamp(x + 40, 40, 880);
            }
            pts.push({ x: Math.round(x), y: Math.round(y) });
        }
        pts.push({
            x: this.WORLD_W,
            y: U.clamp(y + ((s % 2) ? 36 : -36), yMin, yMax)
        });
        return pts;
    },

    _slotsForPath(path, seed) {
        const slots = [];
        for (let i = 1; i < path.length - 1; i += 2) {
            const p = path[i];
            const ox = ((seed + i) % 2) ? 50 : -50;
            const oy = ((seed + i * 3) % 2) ? 45 : -35;
            slots.push({ x: U.clamp(p.x + ox, 40, 920), y: U.clamp(p.y + oy, 56, 544) });
        }
        let fill = 0;
        while (slots.length < 8 && path.length > 2) {
            const pi = 1 + (fill * 2) % Math.max(1, path.length - 2);
            const p = path[pi];
            const ox = ((seed + fill) % 2) ? 58 : -58;
            const oy = ((seed + fill * 3) % 2) ? 48 : -42;
            slots.push({ x: U.clamp(p.x + ox, 40, 920), y: U.clamp(p.y + oy, 56, 544) });
            fill++;
        }
        return slots.slice(0, 12);
    },

    _obstacles(seed, tier, path) {
        const n = 2 + Math.min(3, tier);
        const obs = [];
        const pathPts = path && path.length ? path : [{ x: 480, y: 300 }];
        for (let i = 0; i < n; i++) {
            const anchor = pathPts[(seed + i * 3) % pathPts.length];
            obs.push({
                x: U.clamp(anchor.x + (((seed + i) % 2) ? 70 : -70), 80, 880),
                y: U.clamp(anchor.y + (((seed + i * 2) % 2) ? 55 : -55), 80, 520),
                hp: 60 + tier * 25 + (i * 20),
                gold: 30 + tier * 12,
                kind: (seed + i) % 2 ? "barrel" : "rock"
            });
        }
        return obs;
    },

    getBundle(meta, levelIndex) {
        let bundle;
        if (meta.useLegacyPath != null && LEGACY_PATH_BUNDLES[meta.useLegacyPath]) {
            const b = LEGACY_PATH_BUNDLES[meta.useLegacyPath];
            bundle = {
                path: b.path.map(p => ({ ...p })),
                slots: b.slots.map(s => ({ ...s })),
                obstacles: b.obstacles.map(o => ({ ...o }))
            };
        } else {
            const seed = meta.pathSeed != null ? meta.pathSeed : levelIndex;
            const path = this._waypoints(seed);
            bundle = {
                path,
                slots: this._slotsForPath(path, seed),
                obstacles: this._obstacles(seed, meta.tier || 1, path)
            };
        }
        return this._centerPlayInWorld(bundle);
    }
};
