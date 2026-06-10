/** 底部武将栏 vs 右侧操作面板头像应一致（同将、同裁剪） */
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
        const g = getGeneral("zhaoyun");
        if (slot && g) {
            const tower = new Tower(g, slot.x, slot.y, Game.slots.indexOf(slot));
            slot.occupied = tower;
            Game.towers.push(tower);
            Game.selectedTower = tower;
            if (Game.onUpdate) Game.onUpdate();
        }
        await new Promise(r => setTimeout(r, 400));

        function sampleCanvas(cvs) {
            if (!cvs) return null;
            const ctx = cvs.getContext("2d");
            const w = cvs.width, h = cvs.height;
            const cx = (w / 2) | 0, cy = (h / 2) | 0;
            const d = ctx.getImageData(cx, cy, 1, 1).data;
            const tl = ctx.getImageData((w * 0.2) | 0, (h * 0.2) | 0, 1, 1).data;
            return {
                backing: w,
                cssW: Math.round(cvs.getBoundingClientRect().width),
                cssH: Math.round(cvs.getBoundingClientRect().height),
                styleW: cvs.style.width,
                styleH: cvs.style.height,
                centerRgb: [d[0], d[1], d[2]],
                cornerRgb: [tl[0], tl[1], tl[2]]
            };
        }

        const barCvs = document.querySelector('.gen-chip[data-id="zhaoyun"] canvas');
        const tpCvs = document.querySelector("#tp-portrait canvas");
        const bar = sampleCanvas(barCvs);
        const tp = sampleCanvas(tpCvs);
        const centerDist = bar && tp
            ? Math.abs(bar.centerRgb[0] - tp.centerRgb[0])
              + Math.abs(bar.centerRgb[1] - tp.centerRgb[1])
              + Math.abs(bar.centerRgb[2] - tp.centerRgb[2])
            : 999;
        return {
            general: g?.name,
            bar,
            tp,
            centerColorDist: centerDist,
            pass: !!bar && !!tp
                && tp.cssW === 48 && tp.cssH === 48
                && tp.styleW === "48px" && tp.styleH === "48px"
                && centerDist <= 40
        };
    })()`);

    saveScreen("09_portrait_parity_zhaoyun");

    fs.writeFileSync(path.join(OUT, "portrait-parity-report.json"), JSON.stringify({ time: new Date().toISOString(), metrics }, null, 2));
    console.log("\n=== 武将头像一致性验收 ===");
    console.log("通过:", metrics.pass ? "是" : "否");
    console.log(JSON.stringify(metrics, null, 2));
    process.exit(metrics.pass ? 0 : 1);
}

main().catch(e => { console.error(e); process.exit(1); });
