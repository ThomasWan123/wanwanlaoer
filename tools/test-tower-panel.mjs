/** 右侧武将操作面板验收：选中塔后检查 tp-actions 是否完整可见 */
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
        await new Promise(r => setTimeout(r, 1200));
        Game.gold = 999;
        Game.state = "running";
        document.getElementById("banner")?.classList.add("hidden");
        const slot = Game.slots.find(s => !s.occupied);
        const g = getGeneral("guanyu");
        if (slot && g) {
            const tower = new Tower(g, slot.x, slot.y, Game.slots.indexOf(slot));
            slot.occupied = tower;
            Game.towers.push(tower);
            Game.selectedTower = tower;
            if (Game.onUpdate) Game.onUpdate();
        }
        await new Promise(r => setTimeout(r, 300));
        const panel = document.getElementById("tower-panel");
        const actions = document.querySelector(".tp-actions");
        const upgrade = document.getElementById("tp-upgrade");
        const pr = panel.getBoundingClientRect();
        const ar = actions.getBoundingClientRect();
        const ur = upgrade.getBoundingClientRect();
        const vh = window.innerHeight;
        const buttons = [...actions.querySelectorAll("button")].map(b => ({
            text: b.textContent.trim().slice(0, 8),
            height: Math.round(b.getBoundingClientRect().height),
            bottom: Math.round(b.getBoundingClientRect().bottom)
        }));
        const marginBottom = Math.round(vh - pr.bottom);
        const actionsInPanel = Math.round(ar.bottom) <= Math.round(pr.bottom) + 2;
        return {
            panelVisible: !panel.classList.contains("hidden"),
            panel: { top: Math.round(pr.top), bottom: Math.round(pr.bottom), height: Math.round(pr.height) },
            actions: { top: Math.round(ar.top), bottom: Math.round(ar.bottom), height: Math.round(ar.height) },
            upgradeBtn: { height: Math.round(ur.height), text: upgrade?.textContent?.trim() },
            buttons,
            viewportH: vh,
            marginFromScreenBottom: marginBottom,
            actionsInPanel,
            pass: !panel.classList.contains("hidden")
                && ar.height >= 40
                && ur.height >= 36
                && Math.round(vh - ar.bottom) >= 100
                && actionsInPanel
                && pr.height >= 120
        };
    })()`);

    saveScreen("07_tower_panel_dock");

    fs.writeFileSync(path.join(OUT, "tower-panel-report.json"), JSON.stringify({ time: new Date().toISOString(), metrics }, null, 2));
    console.log("\n=== 武将操作面板验收 ===");
    console.log("通过:", metrics.pass ? "是" : "否");
    console.log(JSON.stringify(metrics, null, 2));
    process.exit(metrics.pass ? 0 : 1);
}

main().catch(e => { console.error(e); process.exit(1); });
