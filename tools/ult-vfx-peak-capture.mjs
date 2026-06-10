import { execSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";

const ADB = process.env.ADB || "C:\\Users\\HMM\\AppData\\Local\\Android\\Sdk\\platform-tools\\adb.exe";
const DEVICE = process.env.ADB_DEVICE || "RFCY71HFAYX";
const OUT = path.join(process.cwd(), "tools", "ult-vfx-captures", "patch05");
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

async function castUlt(wsUrl, kind, hero) {
    return cdpEval(wsUrl, `
        (async () => {
            Main.startGame(0);
            await new Promise(r => setTimeout(r, 500));
            if (window.MobileBridge) MobileBridge.applyLevelViewport();
            Game.state = "running";
            Game.gold = 9999;
            Game._demoMode = true;
            Game.spawning = false;
            Game.waveTimer = 99;
            document.getElementById("banner")?.classList.add("hidden");
            const g = GENERALS.find(x => x.id === "${hero}");
            Game.towers.length = 0;
            Game.slots.forEach(s => { s.occupied = null; });
            const slot = Game.slots.find(s => !s.occupied) || Game.slots[0];
            const t = new Tower(g, slot.x, slot.y, Game.slots.indexOf(slot));
            slot.occupied = t;
            Game.towers.push(t);
            Game.selectedTower = t;
            t.rage = t.maxRage;
            Game.enemies.length = 0;
            if ("${kind}" === "execute" || "${kind}" === "maze") {
                Game._spawnEnemy("junyi");
                await new Promise(r => setTimeout(r, 150));
                const e = Game.enemies[0];
                if (e) { e.hp = e.maxHp; e.alive = true; }
            }
            Game._scheduledTicks.length = 0;
            Game.effects.length = 0;
            Ult.cast(t, Game);
            return Game.effects.filter(e => e.kind === "${kind}").length;
        })()
    `);
}

async function main() {
    fs.mkdirSync(OUT, { recursive: true });
    const page = JSON.parse(await fetch(`${CDP}/json/list`).then(r => r.text())).find(t => t.type === "page");
    if (!page) throw new Error("No WebView target");

    const shots = [
        { kind: "stun", hero: "zhangfei", delays: [40, 80, 120, 200] },
        { kind: "execute", hero: "lvbu", delays: [80, 150, 250] },
        { kind: "maze", hero: "zhugeliang", delays: [400, 800] },
        { kind: "charge", hero: "zhaoyun", delays: [600, 1200, 1800] },
    ];

    for (const s of shots) {
        for (const ms of s.delays) {
            const n = await castUlt(page.webSocketDebuggerUrl, s.kind, s.hero);
            console.log(s.kind, ms + "ms", "effects=" + n);
            await sleep(ms);
            saveScreen(`${s.kind}_${ms}ms`);
            await sleep(800);
        }
    }
    console.log("Saved to", OUT);
}

main().catch(e => { console.error(e); process.exit(1); });
