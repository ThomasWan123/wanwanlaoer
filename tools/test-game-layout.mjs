/** 战斗屏是否铺满视口（避免 game-screen 塌成顶栏+画布条带） */
import { execSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";

const ADB = process.env.ADB || "C:\\Users\\HMM\\AppData\\Local\\Android\\Sdk\\platform-tools\\adb.exe";
const DEVICE = process.env.ADB_DEVICE || "RFCY71HFAYX";
const PKG = "com.sanguotd.towerdefense2";
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
        setTimeout(() => fail(new Error("CDP timeout")), 30000);
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
    await sleep(500);

    const targets = JSON.parse(await fetch("http://127.0.0.1:9222/json/list").then(r => r.text()));
    const page = targets.find(t => t.type === "page" && t.url?.includes("android_asset"))
        || targets.find(t => t.type === "page");

    const metrics = await cdpEval(page.webSocketDebuggerUrl, `(async () => {
        Main.startGame(3, ["guanyu","zhangfei","zhaoyun"]);
        await new Promise(r => setTimeout(r, 1500));
        if (window.MobileBridge) MobileBridge._resizeCanvas();
        await new Promise(r => setTimeout(r, 200));
        const gs = document.getElementById("game-screen");
        const stage = document.querySelector(".stage");
        const canvas = document.getElementById("canvas");
        const vh = window.innerHeight;
        const gsR = gs.getBoundingClientRect();
        const stR = stage.getBoundingClientRect();
        const cvR = canvas.getBoundingClientRect();
        const gsStyle = getComputedStyle(gs);
        return {
            viewportH: vh,
            gameScreen: {
                height: Math.round(gsR.height),
                bottom: Math.round(gsR.bottom),
                position: gsStyle.position
            },
            stage: {
                height: Math.round(stR.height),
                bottom: Math.round(stR.bottom)
            },
            canvas: {
                height: Math.round(cvR.height),
                bottom: Math.round(cvR.bottom)
            },
            fillRatio: Math.round((gsR.height / vh) * 100),
            stageRatio: Math.round((stR.height / vh) * 100),
            pass: gsStyle.position === "absolute"
                && gsR.height >= vh * 0.92
                && stR.height >= vh * 0.92
                && cvR.height >= vh * 0.88
                && Math.abs(gsR.bottom - vh) <= 8
                && Math.abs(stR.bottom - vh) <= 8
        };
    })()`);

    saveScreen("08_game_layout_fullscreen");

    fs.writeFileSync(path.join(OUT, "game-layout-report.json"), JSON.stringify({ time: new Date().toISOString(), metrics }, null, 2));
    console.log("\n=== 战斗屏全屏布局验收 ===");
    console.log("通过:", metrics.pass ? "是" : "否");
    console.log(JSON.stringify(metrics, null, 2));
    process.exit(metrics.pass ? 0 : 1);
}

main().catch(e => { console.error(e); process.exit(1); });
