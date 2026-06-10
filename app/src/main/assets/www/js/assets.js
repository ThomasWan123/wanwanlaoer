// 美术资产预加载 — manifest 驱动，缺失时静默降级到程序绘制
// 使用 XHR（WebView file:// 下比 fetch 更可靠）
window.ArtAssets = {
    _images: {},
    _ready: false,
    _failed: false,
    _manifestPortraits: {},
    _manifestMaps: {},
    _manifestUi: {},
    base: "assets/",

    _fetchJson(url) {
        return new Promise((resolve, reject) => {
            const xhr = new XMLHttpRequest();
            xhr.open("GET", url, true);
            xhr.responseType = "text";
            xhr.onload = () => {
                if (xhr.status >= 200 && xhr.status < 300 || xhr.status === 0) {
                    try {
                        resolve(JSON.parse(xhr.responseText.replace(/^\uFEFF/, "")));
                    } catch (e) {
                        reject(e);
                    }
                } else {
                    reject(new Error("HTTP " + xhr.status));
                }
            };
            xhr.onerror = () => reject(new Error("XHR failed: " + url));
            xhr.send();
        });
    },

    init() {
        return this._fetchJson(this.base + "manifest.json")
            .then(manifest => {
                this._manifestPortraits = manifest.portraits || {};
                this._manifestMaps = manifest.maps || {};
                this._manifestUi = manifest.ui || {};
                return this._preload(manifest);
            })
            .catch(() => { this._failed = true; return false; });
    },

    _preload(manifest) {
        const paths = new Set();
        if (manifest.ui) Object.values(manifest.ui).forEach(p => paths.add(p));
        if (manifest.portraits) Object.values(manifest.portraits).forEach(p => paths.add(p));
        if (manifest.maps) Object.values(manifest.maps).forEach(p => paths.add(p));

        const loads = [...paths].map(rel => new Promise(resolve => {
            const img = new Image();
            img.onload = () => { this._images[rel] = img; resolve(true); };
            img.onerror = () => resolve(false);
            img.src = this.base + rel;
        }));

        return Promise.all(loads).then(results => {
            const ok = results.filter(Boolean).length;
            this._ready = ok > 0;
            if (this._ready) document.body.classList.add("has-art-assets");
            return { ready: this._ready, loaded: ok, total: loads.length };
        });
    },

    get(relPath) {
        return this._images[relPath] || null;
    },

    getPortrait(generalId) {
        const rel = this._manifestPortraits[generalId];
        return rel ? this.get(rel) : null;
    },

    getMapBg(mapTheme) {
        const rel = this._manifestMaps[mapTheme];
        return rel ? this.get(rel) : null;
    },

    getUi(key) {
        const rel = this._manifestUi[key];
        return rel ? this.get(rel) : null;
    },

    hasMapArt(mapTheme) {
        return !!this.getMapBg(mapTheme);
    }
};
