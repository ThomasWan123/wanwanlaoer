/** 武将栏布局验收：第 4 关三将 + 几何测量 + 截屏 */
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
        setTimeout(() => fail(new Error("CDP timeout")), 25000);
    });
}

function setupForward() {
    try { adb("forward", "--remove-all"); } catch {}
    const sockets = adb("shell", "cat", "/proc/net/unix");
    const m = sockets.match(/webview_devtools_remote_\d+/);
    if (!m) throw new Error("no webview devtools socket");
    adb("forward", "tcp:9222", `localabstract:${m[0]}`);
    return m[0];
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
    if (!page) throw new Error("no CDP page");

    const metrics = await cdpEval(page.webSocketDebuggerUrl, `(async () => {
        Main.startGame(3, ["guanyu","zhangfei","zhaoyun"]);
        await new Promise(r => setTimeout(r, 1800));
        Game.state = "running";
        document.getElementById("banner")?.classList.add("hidden");
        const bar = document.getElementById("general-bar");
        const stage = document.querySelector(".stage");
        const br = bar.getBoundingClientRect();
        const sr = stage.getBoundingClientRect();
        const vh = window.innerHeight;
        const badges = [...bar.querySelectorAll(".gen-cost-badge")].map(b => {
            const r = b.getBoundingClientRect();
            return { text: b.textContent.trim(), bottom: Math.round(r.bottom), inBar: r.bottom <= br.bottom + 2 };
        });
        return {
            level: LEVELS[3].name,
            chips: bar.querySelectorAll(".gen-chip").length,
            bar: { top: Math.round(br.top), bottom: Math.round(br.bottom), height: Math.round(br.height), width: Math.round(br.width) },
            stageBottom: Math.round(sr.bottom),
            viewportH: vh,
            marginFromScreenBottom: Math.round(vh - br.bottom),
            marginFromStageBottom: Math.round(sr.bottom - br.bottom),
            badges,
            pass: Math.round(vh - br.bottom) >= 16 && Math.round(sr.bottom - br.bottom) >= 8 && badges.every(b => b.inBar)
        };
    })()`);

    saveScreen("06_battle_xuzhou_genbar");

    const report = { time: new Date().toISOString(), metrics, shot: "06_battle_xuzhou_genbar.png" };
    fs.writeFileSync(path.join(OUT, "genbar-report.json"), JSON.stringify(report, null, 2));
    console.log("\n=== 武将栏布局验收 ===");
    console.log("通过:", metrics.pass ? "是" : "否");
    console.log(JSON.stringify(metrics, null, 2));
    console.log("截屏:", path.join(OUT, "06_battle_xuzhou_genbar.png"));
    process.exit(metrics.pass ? 0 : 1);
}

main().catch(e => { console.error(e); process.exit(1); });
