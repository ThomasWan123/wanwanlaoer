/**
 * 发版前门禁：Gradle 资源一致性 + 数值审计 + manifest +（可选）真机验收
 *
 * 用法：
 *   node tools/pre-release.mjs              # 本地/CI：Gradle + 静态审计
 *   node tools/pre-release.mjs --device     # 额外跑真机视口与 device-acceptance
 *   node tools/pre-release.mjs --skip-gradle  # 跳过 Gradle（已构建时）
 */
import { execSync, spawnSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";

const ROOT = process.cwd();
const OUT = path.join(ROOT, "tools", "acceptance-captures");
const DEBUG_APK = fs.existsSync(path.join(ROOT, "app", "build", "outputs", "apk", "debug", "app-debug-repacked.apk"))
    ? path.join(ROOT, "app", "build", "outputs", "apk", "debug", "app-debug-repacked.apk")
    : path.join(ROOT, "app", "build", "outputs", "apk", "debug", "app-debug.apk");
const ADB = process.env.ADB || (process.platform === "win32"
    ? "C:\\Users\\HMM\\AppData\\Local\\Android\\Sdk\\platform-tools\\adb.exe"
    : "adb");
const GRADLE = process.platform === "win32" ? "gradlew.bat" : "./gradlew";
const args = process.argv.slice(2);
const withDevice = args.includes("--device");
const skipGradle = args.includes("--skip-gradle");

function run(cmd, label) {
    console.log(`\n>>> ${label}\n$ ${cmd}\n`);
    execSync(cmd, { stdio: "inherit", cwd: ROOT });
}

function runNode(script, extra = "", envExtra = {}) {
    const cmd = `node "${path.join("tools", script)}" ${extra}`.trim();
    console.log(`\n>>> ${script}\n$ ${cmd}\n`);
    execSync(cmd, {
        stdio: "inherit",
        cwd: ROOT,
        env: { ...process.env, ADB, ...envExtra }
    });
}

function adbDeviceConnected() {
    try {
        const out = execSync(`"${ADB}" devices`, { encoding: "utf8" });
        return out.split("\n").some(l => l.includes("\tdevice"));
    } catch {
        return false;
    }
}

async function main() {
    fs.mkdirSync(OUT, { recursive: true });
    const summary = { time: new Date().toISOString(), steps: [], pass: true };

    const step = (id, ok, detail) => {
        summary.steps.push({ id, ok, detail });
        if (!ok) summary.pass = false;
        console.log(`[${ok ? "OK" : "FAIL"}] ${id}${detail ? " — " + detail : ""}`);
    };

    if (!skipGradle) {
        try {
            run(`${GRADLE} verifyApkAssetsParity --no-daemon`, "verifyApkAssetsParity");
            step("verifyApkAssetsParity", true);
        } catch (e) {
            step("verifyApkAssetsParity", false, String(e.message || e));
        }
    } else {
        step("verifyApkAssetsParity", fs.existsSync(DEBUG_APK), skipGradle ? "skipped (--skip-gradle)" : "missing apk");
    }

    try {
        runNode("audit-campaign-balance.mjs");
        step("audit-campaign-balance", true);
    } catch {
        step("audit-campaign-balance", false);
    }

    if (fs.existsSync(DEBUG_APK)) {
        try {
            runNode("verify-art-manifest.mjs", `--apk "${DEBUG_APK}"`);
            step("verify-art-manifest", true);
        } catch {
            step("verify-art-manifest", false);
        }
    } else {
        try {
            runNode("verify-art-manifest.mjs");
            step("verify-art-manifest", true, "workspace assets only");
        } catch {
            step("verify-art-manifest", false, "no apk and workspace incomplete");
        }
    }

    if (withDevice) {
        if (!adbDeviceConnected()) {
            step("device-tests", false, "no adb device; connect phone or omit --device");
        } else {
            for (const script of [
                "test-campaign-viewport.mjs",
                "device-acceptance.mjs",
                "test-level5-battle.mjs"
            ]) {
                try {
                    runNode(script);
                    step(script, true);
                } catch {
                    step(script, false);
                }
            }
        }
    }

    fs.writeFileSync(
        path.join(OUT, "pre-release-summary.json"),
        JSON.stringify(summary, null, 2)
    );

    console.log("\n=== pre-release 汇总 ===");
    console.log("通过:", summary.pass ? "是" : "否");
    console.log("报告:", path.join(OUT, "pre-release-summary.json"));
    process.exit(summary.pass ? 0 : 1);
}

main().catch(e => {
    console.error(e);
    process.exit(1);
});
