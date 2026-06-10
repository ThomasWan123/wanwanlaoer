/** 第 5 关 小沛据守：道路 + 塔位 + pan 真机验收 */
import { execSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";

const ADB = process.env.ADB || "C:\\Users\\HMM\\AppData\\Local\\Android\\Sdk\\platform-tools\\adb.exe";
const DEVICE = process.env.ADB_DEVICE || "RFCY71HFAYX";
const PKG = "com.sanguotd.towerdefense2";
const LEVEL_IDX = 4;
const OUT = path.join(process.cwd(), "tools", "acceptance-captures");

function adb(...args) {
    return execSync(`"${ADB}" -s ${DEVICE} ${args.join(" ")}`, { encoding: "utf8" }).trim();
}

function sleep(ms) {
    return new Promise(r => setTimeout(r, ms));
}

async function cdpEval(wsUrl, expression) {
    return new Promise((resolve, reject) => {
        const ws = new WebSocket(wsUrl);
        const evalId = 2;
        const fail = err => { try { ws.close(); } catch {} reject(err); };
        ws.addEventListener("error", e => fail(e.error || e));
        ws.addEventListener("open", () => {
            ws.send(JSON.stringify({ id: 1, method: "Runtime.enable" }));
            ws.send(JSON.stringify({
                id: evalId,
                method: "Runtime.evaluate",
                params: { expression, awaitPromise: true, returnByValue: true }
            }));
        });
        ws.addEventListener("message", ev => {
            const msg = JSON.parse(ev.data.toString());
            if (msg.id === evalId) {
                try { ws.close(); } catch {}
                if (msg.result?.exceptionDetails) reject(new Error(JSON.stringify(msg.result.exceptionDetails)));
                else resolve(msg.result?.result?.value);
            }
        });
        setTimeout(() => fail(new Error("CDP timeout")), 45000);
    });
}

function setupForward() {
    try { adb("forward", "--remove-all"); } catch {}
    const sockets = adb("shell", "cat", "/proc/net/unix");
    const m = sockets.match(/webview_devtools_remote_\d+/);
    if (!m) throw new Error("no webview devtools socket");
    adb("forward", "tcp:9222", `localabstract:${m[0]}`);
}

function saveScreen(name) {
    fs.mkdirSync(OUT, { recursive: true });
    const remote = `/sdcard/td2_${name}.png`;
    adb("shell", "screencap", "-p", remote);
    execSync(`"${ADB}" -s ${DEVICE} pull ${remote} "${path.join(OUT, `${name}.png`)}"`, { stdio: "ignore" });
}

async function main() {
    adb("shell", "am", "force-stop", PKG);
    await sleep(400);
    adb("shell", "am", "start", "-n", `${PKG}/com.sanguotd.MainActivity`);
    await sleep(9000);
    setupForward();
    await sleep(600);

    const targets = JSON.parse(await fetch("http://127.0.0.1:9222/json/list").then(r => r.text()));
    const page = targets.find(t => t.type === "page" && t.url?.includes("android_asset"))
        || targets.find(t => t.type === "page");

    const metrics = await cdpEval(page.webSocketDebuggerUrl, `(async () => {
        const idx = ${LEVEL_IDX};
        Main.startGame(idx, ["guanyu","zhangfei","liubei"]);
        await new Promise(r => setTimeout(r, 1500));
        if (window.MobileBridge) MobileBridge.applyLevelViewport();
        await new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r)));
        await new Promise(r => setTimeout(r, 200));
        Game.gold = 500;
        Game.state = "running";
        document.getElementById("banner")?.classList.add("hidden");
        Game.render();

        const lv = LEVELS[idx];
        const mapOk = !!(ArtAssets.getMapBg && ArtAssets.getMapBg(lv.mapTheme));
        const layoutMode = MobileBridge._layoutMode;
        const pan0 = { x: Game.view.panX, y: Game.view.panY };

        function worldSample(wx, wy) {
            const cvs = document.getElementById("canvas");
            const ctx = cvs.getContext("2d");
            const scr = Viewport.worldToScreen(Game, wx, wy);
            const rect = cvs.getBoundingClientRect();
            const nx = Math.round(scr.x - rect.left);
            const ny = Math.round(scr.y - rect.top);
            const sx = Math.max(0, Math.min(cvs.width - 1, Math.round(nx * (cvs.width / rect.width))));
            const sy = Math.max(0, Math.min(cvs.height - 1, Math.round(ny * (cvs.height / rect.height))));
            const d = ctx.getImageData(sx, sy, 1, 1).data;
            return { wx, wy, nx, ny, rgb: [d[0], d[1], d[2]], dark: d[0] < 20 && d[1] < 15 && d[2] < 12 };
        }

        const mid = lv.path[Math.floor(lv.path.length / 2)];
        let minX = lv.path[0].x, maxX = lv.path[0].x, minY = lv.path[0].y, maxY = lv.path[0].y;
        for (const p of lv.path) {
            minX = Math.min(minX, p.x);
            maxX = Math.max(maxX, p.x);
            minY = Math.min(minY, p.y);
            maxY = Math.max(maxY, p.y);
        }
        const pathCy = (minY + maxY) * 0.5;
        const pathCenteredInWorld = pathCy >= 240 && pathCy <= 360;
        const pathSample = worldSample(mid.x, mid.y);
        const slot = lv.slots[0];
        const slotSample = slot ? worldSample(slot.x, slot.y) : null;
        const startSample = worldSample(lv.path[0].x, lv.path[0].y);

        const panBefore = { x: Game.view.panX, y: Game.view.panY };
        Game._applyPan(0, 40);
        const panAfter = { x: Game.view.panX, y: Game.view.panY };
        const panMoved = Math.abs(panAfter.x - panBefore.x) > 5 || Math.abs(panAfter.y - panBefore.y) > 5;
        Game.view.panX = panBefore.x;
        Game.view.panY = panBefore.y;

        const topbar = document.querySelector(".topbar");
        const genBar = document.getElementById("general-bar");
        const cRect = document.getElementById("canvas").getBoundingClientRect();
        const topInset = topbar ? Math.max(0, topbar.getBoundingClientRect().bottom - cRect.top) : 0;
        const bottomInset = genBar ? Math.max(0, cRect.bottom - genBar.getBoundingClientRect().top) : 0;
        const safeH = Math.max(1, cRect.height - topInset - bottomInset);
        const playfieldFocusY = (topInset + safeH * 0.5) / cRect.height;

        const frameDefault = Viewport.viewFrame(Game);
        const pathMidScr = Viewport.worldToScreen(Game, mid.x, mid.y);
        const pathCenterScr = Viewport.worldToScreen(Game, (minX + maxX) * 0.5, pathCy);
        const pathMidScreenRatio = cRect.height > 0
            ? (pathMidScr.y - cRect.top) / cRect.height
            : 0;
        const pathCenterScreenRatio = cRect.height > 0
            ? (pathCenterScr.y - cRect.top) / cRect.height
            : 0;
        const defaultZoom = Game.view.zoom;

        const minZoom = MobileBridge.getMinZoom();
        Game._setZoom(minZoom, pathCy, (minY + maxY) * 0.5);
        MobileBridge._panFitFullContent(Game.level.path);
        Game.render();
        const pathForCheck = Game.level.path;
        function onScreen(wx, wy) {
            const s = Viewport.worldToScreen(Game, wx, wy);
            return s.x >= cRect.left - 6 && s.x <= cRect.right + 6
                && s.y >= cRect.top - 6 && s.y <= cRect.bottom + 6;
        }
        const pathEndsVisible = onScreen(pathForCheck[0].x, pathForCheck[0].y)
            && onScreen(pathForCheck[pathForCheck.length - 1].x, pathForCheck[pathForCheck.length - 1].y);
        const pathAllVisible = pathForCheck.every(p => onScreen(p.x, p.y));
        const slots = Game.level.slots || [];
        const slotsAllVisible = slots.length > 0
            ? slots.every(s => onScreen(s.x, s.y))
            : true;
        const frameMin = Viewport.viewFrame(Game);

        return {
            levelName: lv.name,
            mapTheme: lv.mapTheme,
            mapLoaded: mapOk,
            layoutMode,
            ultraWide: MobileBridge._isUltraWide(),
            zoom: defaultZoom,
            minZoom: Math.round(minZoom * 1000) / 1000,
            pathCenteredInWorld,
            pathCy: Math.round(pathCy),
            pathAllVisibleAtMinZoom: pathAllVisible,
            slotsAllVisibleAtMinZoom: slotsAllVisible,
            pathEndsVisibleAtMinZoom: pathEndsVisible,
            pathPoints: lv.path.length,
            slotCount: lv.slots.length,
            pan: pan0,
            viewFrame: { vw: Math.round(frameDefault.vw), vh: Math.round(frameDefault.vh) },
            viewFrameAtMin: { vw: Math.round(frameMin.vw), vh: Math.round(frameMin.vh) },
            pathMidScreenRatio: Math.round(pathMidScreenRatio * 100) / 100,
            pathCenterScreenRatio: Math.round(pathCenterScreenRatio * 100) / 100,
            playfieldFocusY: Math.round(playfieldFocusY * 100) / 100,
            pathCenterDelta: Math.round(Math.abs(pathCenterScreenRatio - playfieldFocusY) * 100) / 100,
            canvas: { w: Math.round(cRect.width), h: Math.round(cRect.height) },
            samples: { start: startSample, pathMid: pathSample, slot0: slotSample },
            panMoved,
            pass: lv.name === "小沛据守"
                && mapOk
                && lv.path.length >= 8
                && lv.slots.length >= 8
                && !pathSample.dark
                && slotSample && !slotSample.dark
                && panMoved
                && Math.abs(pathCenterScreenRatio - playfieldFocusY) <= 0.1
                && pathCenteredInWorld
                && pathAllVisible
                && slotsAllVisible
        };
    })()`);

    saveScreen("10_level5_xiaopei_battle");

    fs.writeFileSync(path.join(OUT, "level5-battle-report.json"), JSON.stringify({ time: new Date().toISOString(), metrics }, null, 2));
    console.log("\n=== 第5关 小沛据守 道路/塔位验收 ===");
    console.log("通过:", metrics.pass ? "是" : "否");
    console.log(JSON.stringify(metrics, null, 2));
    process.exit(metrics.pass ? 0 : 1);
}

main().catch(e => { console.error(e); process.exit(1); });
