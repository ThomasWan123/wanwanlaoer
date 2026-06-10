/**
 * 触觉反馈（Android NativeBridge.vibrate）
 */
window.Haptics = {
    _pulse(ms) {
        if (window.Native && Native.vibrate) Native.vibrate(ms);
    },

    light() { this._pulse(28); },
    medium() { this._pulse(55); },
    heavy() { this._pulse(95); },

    boss() { this._pulse(140); },

    leak() { this._pulse(75); },

    victory() {
        this._pulse(45);
        setTimeout(() => this._pulse(35), 90);
        setTimeout(() => this._pulse(50), 180);
    },

    defeat() {
        this._pulse(110);
        setTimeout(() => this._pulse(60), 120);
    },

    ult() { this._pulse(40); }
};
