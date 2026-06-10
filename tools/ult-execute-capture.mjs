import { execSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";

const ADB = process.env.ADB || "C:\\Users\\HMM\\AppData\\Local\\Android\\Sdk\\platform-tools\\adb.exe";
const DEVICE = process.env.ADB_DEVICE || "RFCY71HFAYX";
const OUT = path.join(process.cwd(), "tools", "ult-vfx-captures", "execute-05b");
const CDP = "http://127.0.0.1:9222";

function sleep(ms) { return new Promise(r => setTimeout(r, ms)); }

function saveScreen(name) {
    const remote = `/sdcard/ult_${name}.png`;
    execSync(`"${ADB}" -s ${DEVICE} shell screencap -p ${remote}`, { stdio: "ignore" });
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
            ws.send(JSON.stringify({ id: evalId, method: "Runtime.evaluate", params: { expression, awaitPromise: true, returnByValue: true } }));
        });
        ws.addEventListener("message", ev => {
            const msg = JSON.parse(ev.data.toString());
            if (msg.id === evalId) {
                try { ws.close(); } catch {}
                if (msg.result?.exceptionDetails) reject(new Error(JSON.stringify(msg.result.exceptionDetails)));
                else resolve(msg.result?.result?.value);
            }
        });
        setTimeout(() => fail(new Error("CDP timeout")), 15000);
    });
}

async function setupExecute(wsUrl) {
    return cdpEval(wsUrl, `
        (async () => {
            Main.startGame(0);
            await new Promise(r => setTimeout(r, 600));
            Game.state = "running";
            Game.gold = 9999;
            Game._demoMode = true;
            Game.spawning = false;
            Game.waveTimer = 99;
            document.getElementById("banner")?.classList.add("hidden");
            document.getElementById("tower-panel")?.classList.add("hidden");
            Game.towers.length = 0;
            Game.slots.forEach(s => { s.occupied = null; });
            const g = GENERALS.find(x => x.id === "lvbu");
            const slot = Game.slots[Math.floor(Game.slots.length / 2)] || Game.slots[0];
            const t = new Tower(g, slot.x, slot.y, Game.slots.indexOf(slot));
            slot.occupied = t;
            Game.towers.push(t);
            Game.selectedTower = t;
            t.rage = t.maxRage;
            Game.enemies.length = 0;
            const mid = Game.level.path[Math.floor(Game.level.path.length / 2)];
            Game._spawnEnemy("junyi");
            await new Promise(r => setTimeout(r, 100));
            const e = Game.enemies[0];
            if (e) { e.x = mid.x; e.y = mid.y; e.hp = e.maxHp; }
            Game.effects = Game.effects.filter(x => x.kind !== "execute");
            Ult.cast(t, Game);
            const ex = Game.effects.find(x => x.kind === "execute");
            return ex ? ("ok:" + ex.x.toFixed(0) + "," + ex.y.toFixed(0)) : "no execute effect";
        })()
    `);
}

async function main() {
    fs.mkdirSync(OUT, { recursive: true });
    const page = JSON.parse(await fetch(`${CDP}/json/list`).then(r => r.text())).find(t => t.type === "page");
    if (!page) throw new Error("No WebView target");

    for (const ms of [80, 100, 150, 200, 300, 400]) {
        const res = await setupExecute(page.webSocketDebuggerUrl);
        console.log("cast", ms + "ms wait", res);
        await sleep(ms);
        saveScreen(`execute_${ms}ms`);
        await sleep(900);
    }
    console.log("Saved to", OUT);
}

main().catch(e => { console.error(e); process.exit(1); });
