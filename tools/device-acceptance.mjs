/**
 * 真机验收：WebView CDP + adb 截屏 + logcat
 * 用法：手机 USB 连接，先启动三国塔防2，再 node tools/device-acceptance.mjs
 */
import { execSync, spawnSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";

const ADB = process.env.ADB || "C:\\Users\\HMM\\AppData\\Local\\Android\\Sdk\\platform-tools\\adb.exe";
const DEVICE = process.env.ADB_DEVICE || "RFCY71HFAYX";
const PKG = "com.sanguotd.towerdefense2";
const OUT = path.join(process.cwd(), "tools", "acceptance-captures");
const CDP = "http://127.0.0.1:9222";

function adb(...args) {
    return execSync(`"${ADB}" -s ${DEVICE} ${args.join(" ")}`, { encoding: "utf8" }).trim();
}

function sleep(ms) {
    return new Promise(r => setTimeout(r, ms));
}

function saveScreen(name) {
    const remote = `/sdcard/td2_accept_${name}.png`;
    adb("shell", "screencap", "-p", remote);
    execSync(`"${ADB}" -s ${DEVICE} pull ${remote} "${path.join(OUT, `${name}.png`)}"`, { stdio: "ignore" });
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
                if (msg.result?.exceptionDetails) {
                    reject(new Error(JSON.stringify(msg.result.exceptionDetails)));
                } else {
                    resolve(msg.result?.result?.value);
                }
            }
        });
        setTimeout(() => fail(new Error("CDP timeout")), 20000);
    });
}

function setupForward() {
    try { adb("forward", "--remove-all"); } catch {}
    const sockets = adb("shell", "cat", "/proc/net/unix");
    const m = sockets.match(/webview_devtools_remote_\d+/);
    if (m) {
        adb("forward", "tcp:9222", `localabstract:${m[0]}`);
        return m[0];
    }
    adb("forward", "tcp:9222", "localabstract:chrome_devtools_remote");
    return "chrome_devtools_remote (fallback)";
}

function scanLogcat() {
    const log = adb("logcat", "-d", "-t", "200");
    const lines = log.split("\n");
    const hits = lines.filter(l =>
        l.includes(PKG) && (
            /chromium.*(ERROR|CONSOLE)/i.test(l) ||
            /Console.*error/i.test(l) ||
            /AndroidRuntime.*FATAL/i.test(l)
        )
    );
    return hits.slice(-20);
}

async function main() {
    fs.mkdirSync(OUT, { recursive: true });
    const report = { time: new Date().toISOString(), device: DEVICE, pkg: PKG, checks: [] };

    adb("logcat", "-c");
    adb("shell", "am", "force-stop", PKG);
    await sleep(500);
    adb("shell", "am", "start", "-n", `${PKG}/com.sanguotd.MainActivity`);
    await sleep(10000);

    const sock = setupForward();
    report.devtoolsSocket = sock;
    await sleep(800);

    let page;
    try {
        const targets = JSON.parse(await fetch(`${CDP}/json/list`).then(r => r.text()));
        page = targets.find(t => t.type === "page" && t.url?.includes("android_asset"));
        if (!page) page = targets.find(t => t.type === "page");
    } catch (e) {
        report.checks.push({ id: "cdp", ok: false, detail: "CDP 连接失败: " + e.message });
        report.logcat = scanLogcat();
        fs.writeFileSync(path.join(OUT, "report.json"), JSON.stringify(report, null, 2));
        console.log(JSON.stringify(report, null, 2));
        process.exit(1);
    }

    const ws = page.webSocketDebuggerUrl;

    // 1. 资产加载
    const assets = await cdpEval(ws, `({
        ready: !!(window.ArtAssets && ArtAssets._ready),
        failed: !!(window.ArtAssets && ArtAssets._failed),
        hasClass: document.body.classList.contains("has-art-assets"),
        imageCount: ArtAssets._images ? Object.keys(ArtAssets._images).length : 0,
        portraits: ArtAssets._manifestPortraits ? Object.keys(ArtAssets._manifestPortraits).length : 0,
        maps: ArtAssets._manifestMaps ? Object.keys(ArtAssets._manifestMaps).length : 0,
        menuBg: !!(ArtAssets.getUi && ArtAssets.getUi("menu_bg")),
        guanyu: !!(ArtAssets.getPortrait && ArtAssets.getPortrait("guanyu")),
        chibiMap: !!(ArtAssets.getMapBg && ArtAssets.getMapBg("east_fire"))
    })`);
    report.checks.push({
        id: "assets",
        ok: assets.ready && assets.imageCount >= 50,
        detail: assets
    });

    // 2. 主菜单
    await cdpEval(ws, `UI.show("menu")`);
    await sleep(600);
    saveScreen("01_menu");
    report.checks.push({ id: "menu_screen", ok: true, shot: "01_menu.png" });

    // 3. 选关
    await cdpEval(ws, `(UI.buildLevelList(), UI.show("level-select"), "ok")`);
    await sleep(800);
    saveScreen("02_level_select");
    const levelCards = await cdpEval(ws, `document.querySelectorAll(".level-card.has-map-art").length`);
    report.checks.push({
        id: "level_select",
        ok: levelCards >= 27,
        mapArtCards: levelCards,
        shot: "02_level_select.png"
    });

    // 4. 图鉴（BOSS 立绘）
    await cdpEval(ws, `(UI.buildCodex(), UI.show("codex"), "ok")`);
    await sleep(1200);
    saveScreen("03_codex");
    const codexCards = await cdpEval(ws, `document.querySelectorAll(".codex-card-v canvas").length`);
    report.checks.push({ id: "codex", ok: codexCards >= 5, portraitCards: codexCards, shot: "03_codex.png" });

    // 4b. 编队（立绘裁剪）
    await cdpEval(ws, `(UI.openLineup(3), "ok")`);
    await sleep(800);
    saveScreen("03b_lineup");
    report.checks.push({ id: "lineup", ok: true, shot: "03b_lineup.png" });

    // 5. 赤壁战斗地图 (index 14)
    await cdpEval(ws, `
        (async () => {
            Main.startGame(14, ["guanyu","zhouyu","zhugeliang"]);
            await new Promise(r => setTimeout(r, 1200));
            Game.gold = 500;
            Game.state = "running";
            return LEVELS[14].mapTheme + ":" + (ArtAssets.getMapBg(LEVELS[14].mapTheme) ? "map_ok" : "map_missing");
        })()
    `);
    await sleep(500);
    saveScreen("04_battle_chibi");
    report.checks.push({ id: "battle_chibi", ok: true, shot: "04_battle_chibi.png" });

    // 6. 虎牢关 (index 2)
    await cdpEval(ws, `
        (async () => {
            Main.startGame(2, ["guanyu","zhangfei","zhaoyun"]);
            await new Promise(r => setTimeout(r, 1000));
            Game.state = "running";
            return LEVELS[2].mapTheme;
        })()
    `);
    await sleep(500);
    saveScreen("05_battle_huluguan");
    report.checks.push({ id: "battle_huluguan", ok: true, shot: "05_battle_huluguan.png" });

    report.logcatErrors = scanLogcat();
    report.pass = report.checks.every(c => c.ok !== false) && report.logcatErrors.length === 0;

    fs.writeFileSync(path.join(OUT, "report.json"), JSON.stringify(report, null, 2));
    console.log("\n=== 三国塔防2 真机验收 ===");
    console.log("通过:", report.pass ? "是" : "否");
    for (const c of report.checks) {
        console.log(`  [${c.ok ? "OK" : "FAIL"}] ${c.id}`, c.detail ?? c.mapArtCards ?? "");
    }
    console.log("截屏目录:", OUT);
    if (report.logcatErrors.length) {
        console.log("Logcat 异常:", report.logcatErrors.length, "条");
    }
    process.exit(report.pass ? 0 : 1);
}

main().catch(e => { console.error(e); process.exit(1); });
