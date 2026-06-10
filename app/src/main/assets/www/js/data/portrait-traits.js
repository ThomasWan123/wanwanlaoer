// 关底将 / 里程碑将专属立绘配置（数据驱动，由 Art._drawPortraitTraits 合成）
window.PORTRAIT_TRAITS = {
    zhangjiao: { face: "pale", hat: "yellowTurban", beard: "long", weapon: "fan", extra: "none" },
    huaxiong: { face: "dark", hat: "helm", beard: "tiger", weapon: "saber", extra: "scar" },
    caobao: { face: "tan", hat: "warrior", beard: "short", weapon: "saber", extra: "none" },
    jiling: { face: "tan", hat: "helm", beard: "short", weapon: "halberd", extra: "none" },
    yuanshao: { face: "pale", hat: "crown", beard: "long", weapon: "none", extra: "none" },
    yanliang: { face: "red", hat: "helm", beard: "tiger", weapon: "halberd", extra: "none" },
    wenchou: { face: "dark", hat: "warrior", beard: "tiger", weapon: "halberd", extra: "scar" },
    chunyuqiong: { face: "tan", hat: "warrior", beard: "short", weapon: "none", extra: "flame" },
    shenpei: { face: "pale", hat: "official", beard: "long", weapon: "fan", extra: "none" },
    caoren: { face: "tan", hat: "helm", beard: "short", weapon: "none", extra: "none" },
    xiahoudun: { face: "tan", hat: "helm", beard: "short", weapon: "halberd", extra: "blind" },
    yuejin: { face: "tan", hat: "warrior", beard: "none", weapon: "spear", extra: "none" },
    huanggai: { face: "weathered", hat: "warrior", beard: "short", weapon: "none", extra: "flame" },
    caocao: { face: "pale", hat: "official", beard: "goatee", weapon: "saber", extra: "none" },
    jiangqin: { face: "tan", hat: "navy", beard: "none", weapon: "saber", extra: "none" },
    zhangliao: { face: "tan", hat: "helm", beard: "short", weapon: "halberd", extra: "none" },
    caimao: { face: "pale", hat: "navy", beard: "goatee", weapon: "fan", extra: "water" },
    zhangren: { face: "dark", hat: "warrior", beard: "short", weapon: "bow", extra: "none" },
    liuzhang: { face: "pale", hat: "crown", beard: "long", weapon: "none", extra: "none" },
    xiahouyuan: { face: "tan", hat: "helm", beard: "short", weapon: "spear", extra: "none" },
    yujin: { face: "weathered", hat: "warrior", beard: "short", weapon: "none", extra: "none" },
    lvmeng: { face: "tan", hat: "navy", beard: "none", weapon: "saber", extra: "mask" },
    luxun: { face: "pale", hat: "official", beard: "goatee", weapon: "fan", extra: "flame" },
    menghuo: { face: "dark", hat: "warrior", beard: "tiger", weapon: "halberd", extra: "none" },
    masu: { face: "pale", hat: "official", beard: "goatee", weapon: "fan", extra: "none" },
    guohuai: { face: "weathered", hat: "helm", beard: "short", weapon: "bow", extra: "none" },
    simayi: { face: "pale", hat: "official", beard: "long", weapon: "fan", extra: "none" },
    dengai: { face: "tan", hat: "warrior", beard: "short", weapon: "spear", extra: "none" },
    simayan: { face: "pale", hat: "crown", beard: "long", weapon: "saber", extra: "none" },
    lusu: { face: "pale", hat: "official", beard: "goatee", weapon: "fan", extra: "water" },
    sunquan: { face: "tan", hat: "crown", beard: "goatee", weapon: "saber", extra: "water" }
};

// 核心将已有 _hero_*，不写入 traits
window.CORE_PORTRAIT_IDS = new Set(["guanyu", "zhangfei", "zhaoyun", "zhugeliang", "lvbu", "zhouyu"]);

// AI 立绘圆形裁剪锚点（归一化 0~1，x/y 为图内脸部中心）
window.PORTRAIT_FACE_ANCHOR = {
    guanyu: { x: 0.5, y: 0.24 },
    zhangfei: { x: 0.5, y: 0.30 },
    zhaoyun: { x: 0.5, y: 0.27 },
    zhugeliang: { x: 0.5, y: 0.26 },
    liubei: { x: 0.5, y: 0.28 },
    lvbu: { x: 0.5, y: 0.26 },
    zhouyu: { x: 0.5, y: 0.27 },
    caocao: { x: 0.5, y: 0.28 },
    sunquan: { x: 0.5, y: 0.27 },
    zhangjiao: { x: 0.5, y: 0.25 },
    huaxiong: { x: 0.5, y: 0.29 },
    yuanshao: { x: 0.5, y: 0.26 },
    yanliang: { x: 0.5, y: 0.28 },
    wenchou: { x: 0.5, y: 0.29 },
    chunyuqiong: { x: 0.5, y: 0.30 },
    menghuo: { x: 0.5, y: 0.30 },
    simayi: { x: 0.5, y: 0.27 },
    lusu: { x: 0.5, y: 0.26 }
};
