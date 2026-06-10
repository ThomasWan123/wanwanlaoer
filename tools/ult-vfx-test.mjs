import { execSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";

const ADB = process.env.ADB || "C:\\Users\\HMM\\AppData\\Local\\Android\\Sdk\\platform-tools\\adb.exe";
const DEVICE = process.env.ADB_DEVICE || "RFCY71HFAYX";
const OUT = path.join(process.cwd(), "tools", "ult-vfx-captures");
const CDP = "http://127.0.0.1:9222";

function sleep(ms) {
    return new Promise(r => setTimeout(r, ms));
}

function saveScreen(name) {
    const remote = `/sdcard/ult_${name}.png`;
    execSync(`"${ADB}" -s ${DEVICE} shell screencap -p ${remote}`, { stdio: "ignore" });
    execSync(`"${ADB}" -s ${DEVICE} pull ${remote} "${path.join(OUT, `${name}.png`)}"`, { stdio: "ignore" });
}

async function cdpEval(wsUrl, expression) {
    return new Promise((resolve, reject) => {
        const ws = new WebSocket(wsUrl);
        let evalId = 2;
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
        setTimeout(() => fail(new Error("CDP timeout")), 15000);
    });
}

const PKG = "com.sanguotd.towerdefense2";

function adb(...args) {
    return execSync(`"${ADB}" -s ${DEVICE} ${args.join(" ")}`, { encoding: "utf8" }).trim();
}

function setupForward() {
    try { adb("forward", "--remove-all"); } catch {}
    const sockets = adb("shell", "cat", "/proc/net/unix");
    const m = sockets.match(/webview_devtools_remote_\d+/);
    if (!m) throw new Error("no webview devtools socket — start debug app on device");
    adb("forward", "tcp:9222", `localabstract:${m[0]}`);
    return m[0];
}

async function main() {
    fs.mkdirSync(OUT, { recursive: true });
    try {
        setupForward();
    } catch (e) {
        adb("shell", "am", "start", "-n", `${PKG}/com.sanguotd.MainActivity`);
        await sleep(9000);
        setupForward();
    }
    await sleep(600);
    const targets = JSON.parse(await fetch(`${CDP}/json/list`).then(r => r.text()));
    const page = targets.find(t => t.type === "page");
    if (!page) throw new Error("No WebView page target");

    const setup = `
        (async () => {
            Main.startGame(0);
            await new Promise(r => setTimeout(r, 800));
            if (window.MobileBridge) MobileBridge.applyLevelViewport();
            Game.gold = 9999;
            Game._demoMode = true;
            Game.state = "running";
            const slot = Game.slots.find(s => !s.occupied);
            if (!slot) return "no slot";
            return "ok:" + slot.x + "," + slot.y;
        })()
    `;
    const setupRes = await cdpEval(page.webSocketDebuggerUrl, setup);
    console.log("setup:", setupRes);

    const ults = [
        { kind: "flood", hero: "guanyu", peakMs: 1200 },
        { kind: "blaze", hero: "zhouyu", peakMs: 1500 },
        { kind: "stun", hero: "zhangfei", peakMs: 120 },
        { kind: "maze", hero: "zhugeliang", peakMs: 1500 },
        { kind: "execute", hero: "lvbu", peakMs: 200 },
        { kind: "charge", hero: "zhaoyun", peakMs: 1800 }
    ];

    const notes = [];

    for (const u of ults) {
        const expr = `
            (async () => {
                const g = GENERALS.find(x => x.id === "${u.hero}");
                Game.towers.length = 0;
                Game.slots.forEach(s => { s.occupied = null; });
                Game._scheduledTicks.length = 0;
                Game.effects.length = 0;
                const slot = Game.slots.find(s => !s.occupied) || Game.slots[0];
                const t = new Tower(g, slot.x, slot.y, Game.slots.indexOf(slot));
                slot.occupied = t;
                Game.towers.push(t);
                Game.selectedTower = t;
                t.rage = t.maxRage;
                Game.enemies.length = 0;
                Game._spawnEnemy("junyi");
                await new Promise(r => setTimeout(r, 200));
                const e = Game.enemies[0];
                if (e) { e.hp = e.maxHp; e.alive = true; }
                Ult.cast(t, Game);
                return "${u.kind}:" + Game.effects.filter(e => e.kind === "${u.kind}").length;
            })()
        `;
        try {
            const res = await cdpEval(page.webSocketDebuggerUrl, expr);
            const count = parseInt(String(res).split(":")[1] || "0", 10);
            const ok = !String(res).startsWith("no ") && count > 0;
            console.log(u.kind, res, ok ? "OK" : "FAIL");
            await sleep(u.peakMs);
            saveScreen(u.kind);
            const shot = path.join(OUT, `${u.kind}.png`);
            await sleep(2200);
            notes.push({ kind: u.kind, result: res, ok, shot });
        } catch (e) {
            notes.push({ kind: u.kind, error: String(e) });
            console.error(u.kind, e.message);
        }
    }

    const pass = notes.every(n => n.ok && !n.error);
    fs.writeFileSync(path.join(OUT, "results.json"), JSON.stringify({ pass, notes }, null, 2));
    console.log("\n=== 大招 VFX 验收 ===");
    console.log("通过:", pass ? "是" : "否", `(${notes.filter(n => n.ok).length}/${notes.length})`);
    console.log("截屏:", OUT);
    process.exit(pass ? 0 : 1);
}

main().catch(e => { console.error(e); process.exit(1); });
