/**
 * 程序化短音效（Web Audio），无外部音频文件
 */
window.Sfx = {
    _ctx: null,
    _enabled: true,

    init() {
        if (window.Progress) this._enabled = Progress.getSfxEnabled();
    },

    setEnabled(on) {
        this._enabled = !!on;
    },

    isEnabled() {
        return this._enabled;
    },

    _ensureCtx() {
        if (!this._enabled) return null;
        try {
            if (!this._ctx) {
                const AC = window.AudioContext || window.webkitAudioContext;
                if (!AC) return null;
                this._ctx = new AC();
            }
            if (this._ctx.state === "suspended") this._ctx.resume();
            return this._ctx;
        } catch (e) {
            return null;
        }
    },

    _tone(freq, duration, type, gain, when) {
        const ctx = this._ensureCtx();
        if (!ctx) return;
        const t0 = when != null ? when : ctx.currentTime;
        const osc = ctx.createOscillator();
        const g = ctx.createGain();
        osc.type = type || "sine";
        osc.frequency.setValueAtTime(freq, t0);
        g.gain.setValueAtTime(0.0001, t0);
        g.gain.exponentialRampToValueAtTime(gain || 0.12, t0 + 0.02);
        g.gain.exponentialRampToValueAtTime(0.0001, t0 + duration);
        osc.connect(g);
        g.connect(ctx.destination);
        osc.start(t0);
        osc.stop(t0 + duration + 0.05);
    },

    _noise(duration, gain) {
        const ctx = this._ensureCtx();
        if (!ctx) return;
        const t0 = ctx.currentTime;
        const len = Math.floor(ctx.sampleRate * duration);
        const buf = ctx.createBuffer(1, len, ctx.sampleRate);
        const data = buf.getChannelData(0);
        for (let i = 0; i < len; i++) data[i] = (Math.random() * 2 - 1) * 0.5;
        const src = ctx.createBufferSource();
        src.buffer = buf;
        const g = ctx.createGain();
        g.gain.setValueAtTime(gain || 0.08, t0);
        g.gain.exponentialRampToValueAtTime(0.0001, t0 + duration);
        src.connect(g);
        g.connect(ctx.destination);
        src.start(t0);
    },

    playUlt(type) {
        if (!this._enabled) return;
        switch (type) {
            case "flood":
                this._tone(200, 0.2, "sine", 0.09);
                this._tone(340, 0.25, "triangle", 0.08);
                break;
            case "blaze":
                this._tone(160, 0.15, "sawtooth", 0.1);
                this._tone(280, 0.22, "square", 0.07);
                break;
            case "stun":
                this._tone(90, 0.28, "square", 0.1);
                this._noise(0.08, 0.06);
                break;
            case "maze":
                this._tone(440, 0.12, "sine", 0.07);
                this._tone(660, 0.18, "sine", 0.06);
                break;
            case "execute":
                this._tone(120, 0.08, "sawtooth", 0.11);
                this._tone(80, 0.2, "triangle", 0.09);
                break;
            case "charge":
                this._tone(300, 0.1, "triangle", 0.08);
                this._tone(500, 0.15, "sine", 0.07);
                break;
            case "rally":
                this._tone(392, 0.14, "sine", 0.09);
                this._tone(523, 0.2, "triangle", 0.08);
                break;
            case "hex":
                this._tone(330, 0.16, "sine", 0.08);
                this._tone(495, 0.22, "triangle", 0.07);
                break;
            case "tide":
                this._tone(220, 0.18, "sine", 0.09);
                this._tone(440, 0.24, "triangle", 0.08);
                break;
            default:
                this.play("ult");
        }
    },

    play(name) {
        if (!this._enabled) return;
        switch (name) {
            case "place":
                this._tone(420, 0.08, "triangle", 0.1);
                this._tone(620, 0.06, "sine", 0.06, this._ctx ? this._ctx.currentTime + 0.04 : 0);
                break;
            case "upgrade":
                this._tone(520, 0.1, "square", 0.07);
                this._tone(780, 0.12, "sine", 0.08);
                break;
            case "kill":
                this._tone(280, 0.05, "square", 0.06);
                this._noise(0.04, 0.05);
                break;
            case "ult":
                this._tone(180, 0.15, "sawtooth", 0.1);
                this._tone(360, 0.2, "triangle", 0.09);
                break;
            case "victory":
                this._tone(523, 0.12, "sine", 0.1);
                this._tone(659, 0.12, "sine", 0.09, this._ctx ? this._ctx.currentTime + 0.1 : 0);
                this._tone(784, 0.2, "sine", 0.1, this._ctx ? this._ctx.currentTime + 0.2 : 0);
                break;
            case "defeat":
                this._tone(220, 0.25, "sawtooth", 0.08);
                this._tone(160, 0.35, "triangle", 0.07, this._ctx ? this._ctx.currentTime + 0.15 : 0);
                break;
            case "boss":
                this._tone(110, 0.3, "sawtooth", 0.1);
                this._noise(0.12, 0.05);
                break;
            case "wave":
                this._tone(520, 0.06, "sine", 0.06);
                this._tone(780, 0.08, "triangle", 0.05, this._ctx ? this._ctx.currentTime + 0.05 : 0);
                break;
            case "ui":
                this._tone(640, 0.05, "sine", 0.05);
                break;
            default:
                break;
        }
    }
};
