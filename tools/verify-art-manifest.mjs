/**
 * 校验 manifest.json 与磁盘/APK 内美术资源一致，并报告超体积文件
 *
 * 用法：
 *   node tools/verify-art-manifest.mjs
 *   node tools/verify-art-manifest.mjs --apk app/build/outputs/apk/debug/app-debug.apk
 *   node tools/verify-art-manifest.mjs --strict   # 缺文件即失败
 */
import fs from "node:fs";
import path from "node:path";
import { execSync } from "node:child_process";

const ROOT = process.cwd();
const ASSETS = path.join(ROOT, "app", "src", "main", "assets", "www", "assets");
const MANIFEST = path.join(ASSETS, "manifest.json");
const OUT = path.join(ROOT, "tools", "acceptance-captures");
const PORTRAIT_MAX = 200 * 1024;
const MAP_MAX = 400 * 1024;

const args = process.argv.slice(2);
const strict = args.includes("--strict");
const apkIdx = args.indexOf("--apk");
const apkPath = apkIdx >= 0 ? args[apkIdx + 1] : null;

function readManifest() {
    if (!fs.existsSync(MANIFEST)) throw new Error("manifest.json missing");
    return JSON.parse(fs.readFileSync(MANIFEST, "utf8"));
}

function collectManifestPaths(manifest) {
    const paths = new Set();
    if (manifest.ui) {
        for (const rel of Object.values(manifest.ui)) paths.add(rel.replace(/\\/g, "/"));
    }
    if (manifest.portraits) {
        for (const rel of Object.values(manifest.portraits)) paths.add(rel.replace(/\\/g, "/"));
    }
    if (manifest.maps) {
        for (const rel of Object.values(manifest.maps)) paths.add(rel.replace(/\\/g, "/"));
    }
    return paths;
}

function listWorkspaceImages() {
    const found = new Map();
    if (!fs.existsSync(ASSETS)) return found;
    for (const f of fs.readdirSync(ASSETS, { recursive: true })) {
        const name = String(f);
        if (!/\.(png|jpg|jpeg|webp)$/i.test(name)) continue;
        const full = path.join(ASSETS, name);
        if (fs.statSync(full).isFile()) {
            found.set(name.replace(/\\/g, "/"), full);
        }
    }
    return found;
}

function listApkImages(apk) {
    if (!fs.existsSync(apk)) throw new Error(`APK not found: ${apk}`);
    const listing = execSync(`jar tf "${apk}"`, { encoding: "utf8" });
    const prefix = "assets/www/assets/";
    const found = new Map();
    for (const line of listing.split(/\r?\n/)) {
        const trimmed = line.trim();
        if (!trimmed.startsWith(prefix)) continue;
        const rel = trimmed.slice(prefix.length);
        if (/\.(png|jpg|jpeg|webp)$/i.test(rel)) {
            found.set(rel, line);
        }
    }
    return found;
}

function sizeLimitFor(rel) {
    if (rel.startsWith("portraits/")) return PORTRAIT_MAX;
    if (rel.startsWith("maps/") || rel.startsWith("ui/")) return MAP_MAX;
    return MAP_MAX;
}

function main() {
    const manifest = readManifest();
    const expected = collectManifestPaths(manifest);
    const source = apkPath ? listApkImages(path.resolve(apkPath)) : listWorkspaceImages();

    const missing = [];
    const orphans = [];
    const oversize = [];

    for (const rel of expected) {
        if (!source.has(rel)) missing.push(rel);
        else if (!apkPath) {
            const st = fs.statSync(source.get(rel));
            const limit = sizeLimitFor(rel);
            if (st.size > limit) {
                oversize.push({ rel, bytes: st.size, limit, kb: Math.round(st.size / 1024) });
            }
        }
    }

    for (const rel of source.keys()) {
        if (rel === "manifest.json") continue;
        if (!expected.has(rel)) orphans.push(rel);
    }

    const report = {
        time: new Date().toISOString(),
        mode: apkPath ? "apk" : "workspace",
        apk: apkPath || null,
        manifest: {
            portraits: Object.keys(manifest.portraits || {}).length,
            maps: Object.keys(manifest.maps || {}).length,
            ui: Object.keys(manifest.ui || {}).length,
            entries: expected.size
        },
        onDisk: source.size,
        missing: missing.sort(),
        orphans: orphans.sort(),
        oversize,
        pass: missing.length === 0 && (apkPath ? true : oversize.length === 0)
    };

    if (strict && missing.length) report.pass = false;
    if (!strict && missing.length && !apkPath) {
        report.pass = oversize.length === 0;
        report.note = "workspace missing binary assets; run sync-art-assets.ps1 or pass --apk";
    }
    if (!apkPath && oversize.length) {
        report.note = (report.note ? report.note + "; " : "") + "run tools/compress-art.ps1 to reduce APK size";
    }

    fs.mkdirSync(OUT, { recursive: true });
    fs.writeFileSync(path.join(OUT, "art-manifest-report.json"), JSON.stringify(report, null, 2));

    console.log("\n=== manifest / 资源校验 ===");
    console.log(`模式: ${report.mode} | manifest 条目: ${expected.size} | 实际图片: ${source.size}`);
    console.log(`缺失: ${missing.length} | 孤儿: ${orphans.length} | 超体积: ${oversize.length}`);
    if (missing.length) console.log("  缺失示例:", missing.slice(0, 5).join(", "));
    if (oversize.length) {
        for (const o of oversize.slice(0, 8)) {
            console.log(`  超大 ${o.rel}: ${o.kb}KB (limit ${Math.round(o.limit / 1024)}KB)`);
        }
    }
    if (report.note) console.log("  备注:", report.note);
    console.log("通过:", report.pass ? "是" : "否");

    process.exit(report.pass ? 0 : 1);
}

main();
