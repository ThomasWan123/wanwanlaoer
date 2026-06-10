/** 战役关卡：道路居中 + 进关聚焦 + 最小缩放全程可见（外屏） */
import { execSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";

const ADB = process.env.ADB || "C:\\Users\\HMM\\AppData\\Local\\Android\\Sdk\\platform-tools\\adb.exe";
const DEVICE = process.env.ADB_DEVICE || "RFCY71HFAYX";
const PKG = "com.sanguotd.towerdefense2";
const OUT = path.join(process.cwd(), "tools", "acceptance-captures");
const START = parseInt(process.env.LEVEL_START || "0", 10);
const END = parseInt(process.env.LEVEL_END || "29", 10);

function adb(...args) {
    return execSync(`"${ADB}" -s ${DEVICE} ${args.join(" ")}`, { encoding: "utf8" }).trim();
}

function sleep(ms) {
    return new Promise(r => setTimeout(r, ms));
}

async function cdpEval(wsUrl, expression, timeoutMs = 180000) {
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
        setTimeout(() => fail(new Error("CDP timeout")), timeoutMs);
    });
}

function setupForward() {
    try { adb("forward", "--remove-all"); } catch {}
    const sockets = adb("shell", "cat", "/proc/net/unix");
    const m = sockets.match(/webview_devtools_remote_\d+/);
    if (!m) throw new Error("no webview devtools socket");
    adb("forward", "tcp:9222", `localabstract:${m[0]}`);
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

    const report = await cdpEval(page.webSocketDebuggerUrl, `(async () => {
        const START = ${START};
        const END = ${END};

        function auditLevel(idx) {
            Main.startGame(idx, ["guanyu", "zhangfei", "liubei"]);
            return new Promise(resolve => {
                setTimeout(() => {
                    if (window.MobileBridge) MobileBridge.applyLevelViewport();
                    requestAnimationFrame(() => requestAnimationFrame(() => {
                        Game.gold = 500;
                        Game.state = "running";
                        document.getElementById("banner")?.classList.add("hidden");
                        Game.render();

                        const lv = LEVELS[idx];
                        const path = Game.level.path;
                        const slots = Game.level.slots || [];
                        const mapOk = !!(ArtAssets.getMapBg && ArtAssets.getMapBg(lv.mapTheme));

                        let minX = path[0].x, maxX = path[0].x, minY = path[0].y, maxY = path[0].y;
                        for (const p of path) {
                            minX = Math.min(minX, p.x);
                            maxX = Math.max(maxX, p.x);
                            minY = Math.min(minY, p.y);
                            maxY = Math.max(maxY, p.y);
                        }
                        const pathCy = (minY + maxY) * 0.5;
                        const playBox = Viewport.playContentBBox(path, slots);
                        const contentCy = playBox.cy;

                        const cRect = document.getElementById("canvas").getBoundingClientRect();
                        const topbar = document.querySelector(".topbar");
                        const genBar = document.getElementById("general-bar");
                        const topInset = topbar ? Math.max(0, topbar.getBoundingClientRect().bottom - cRect.top) : 0;
                        const bottomInset = genBar ? Math.max(0, cRect.bottom - genBar.getBoundingClientRect().top) : 0;
                        const playfieldFocusY = (topInset + Math.max(1, cRect.height - topInset - bottomInset) * 0.5) / cRect.height;

                        const pathCenterScr = Viewport.worldToScreen(Game, playBox.cx, contentCy);
                        const pathCenterScreenRatio = cRect.height > 0
                            ? (pathCenterScr.y - cRect.top) / cRect.height
                            : 0;

                        const panBefore = { x: Game.view.panX, y: Game.view.panY };
                        Game._applyPan(0, 40);
                        const panMoved = Math.abs(Game.view.panX - panBefore.x) > 5
                            || Math.abs(Game.view.panY - panBefore.y) > 5;
                        Game.view.panX = panBefore.x;
                        Game.view.panY = panBefore.y;

                        const minZoom = MobileBridge.getMinZoom();
                        Game._setZoom(minZoom, contentCy, contentCy);
                        MobileBridge._panFitFullContent(path);
                        Game.render();

                        function onScreen(wx, wy) {
                            const s = Viewport.worldToScreen(Game, wx, wy);
                            return s.x >= cRect.left - 6 && s.x <= cRect.right + 6
                                && s.y >= cRect.top - 6 && s.y <= cRect.bottom + 6;
                        }

                        const pathAllVisible = path.every(p => onScreen(p.x, p.y));
                        const slotsAllVisible = slots.length > 0
                            ? slots.every(s => onScreen(s.x, s.y))
                            : true;

                        const contentCenteredInWorld = contentCy >= 220 && contentCy <= 380;
                        const entryFocused = Math.abs(pathCenterScreenRatio - playfieldFocusY) <= 0.12;

                        const pass = mapOk
                            && path.length >= 8
                            && slots.length >= 8
                            && contentCenteredInWorld
                            && entryFocused
                            && pathAllVisible
                            && slotsAllVisible
                            && panMoved;

                        resolve({
                            index: idx,
                            level: idx + 1,
                            name: lv.name,
                            mapTheme: lv.mapTheme,
                            legacy: !!CAMPAIGN_META[idx].useLegacyPath,
                            pathPoints: path.length,
                            slotCount: slots.length,
                            contentCy: Math.round(contentCy),
                            pathCy: Math.round(pathCy),
                            zoom: Math.round(Game.view.zoom * 1000) / 1000,
                            minZoom: Math.round(minZoom * 1000) / 1000,
                            pathCenterScreenRatio: Math.round(pathCenterScreenRatio * 100) / 100,
                            playfieldFocusY: Math.round(playfieldFocusY * 100) / 100,
                            pathAllVisibleAtMinZoom: pathAllVisible,
                            slotsAllVisibleAtMinZoom: slotsAllVisible,
                            panMoved,
                            pass
                        });
                    }));
                }, 900);
            });
        }

        const results = [];
        for (let i = START; i <= END && i < LEVELS.length; i++) {
            results.push(await auditLevel(i));
        }
        const failed = results.filter(r => !r.pass);
        return {
            range: { start: START + 1, end: Math.min(END, LEVELS.length - 1) + 1 },
            total: results.length,
            passed: results.filter(r => r.pass).length,
            failed: failed.length,
            ultraWide: MobileBridge._isUltraWide(),
            canvas: document.getElementById("canvas")?.getBoundingClientRect(),
            results,
            pass: failed.length === 0
        };
    })()`);

    fs.mkdirSync(OUT, { recursive: true });
    fs.writeFileSync(
        path.join(OUT, "campaign-viewport-report.json"),
        JSON.stringify({ time: new Date().toISOString(), report }, null, 2)
    );

    console.log(`\n=== 战役关卡视口验收（第 ${report.range.start}–${report.range.end} 关）===`);
    console.log(`外屏: ${report.ultraWide ? "是" : "否"} | 通过: ${report.passed}/${report.total}`);
    for (const r of report.results) {
        const mark = r.pass ? "OK" : "FAIL";
        console.log(
            `[${mark}] 第${r.level}关 ${r.name} | cy=${r.contentCy} 屏中=${r.pathCenterScreenRatio} 路=${r.pathAllVisibleAtMinZoom ? "全" : "缺"} 位=${r.slotsAllVisibleAtMinZoom ? "全" : "缺"}`
        );
    }
    if (report.failed > 0) {
        console.log("\n未通过详情:");
        console.log(JSON.stringify(report.results.filter(r => !r.pass), null, 2));
    }
    process.exit(report.pass ? 0 : 1);
}

main().catch(e => { console.error(e); process.exit(1); });
