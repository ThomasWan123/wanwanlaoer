// 关卡与敌军 — 30 关由 LevelFactory 生成
window.ENEMY_TYPES = {};

window.REGIONAL_ENEMY_TYPES = {
    bingzhou_rider: { name: "并州狼骑", hp: 200, speed: 125, gold: 18, size: 17, color: "#5a3a2a", weapon: "lance", scale: 1.15, armor: 0.08 },
    qiang_rider: { name: "羌族骑兵", hp: 185, speed: 112, gold: 17, size: 16, color: "#6a5040", weapon: "lance", scale: 1.1, armor: 0.1 },
    nanman: { name: "南蛮卒", hp: 165, speed: 68, gold: 14, size: 15, color: "#3d5a30", weapon: "club", scale: 1.05, armor: 0.18 },
    nanman_elite: { name: "象兵精英", hp: 520, speed: 36, gold: 32, size: 22, color: "#4a3a28", weapon: "baggage", scale: 1.35, armor: 0.42 },
    jiangdong_navy: { name: "江东舟师", hp: 220, speed: 42, gold: 16, size: 16, color: "#2a4a6a", weapon: "crossbow", scale: 1.08, armor: 0.2 },
    shanxi_pike: { name: "巴蜀矛手", hp: 195, speed: 58, gold: 15, size: 15, color: "#4a3a2a", weapon: "spear", scale: 1.06, armor: 0.15 },
    xiliang_archer: { name: "凉州弓骑", hp: 140, speed: 95, gold: 14, size: 15, color: "#5a4030", weapon: "bow", scale: 1.05, armor: 0.08 }
};

Object.assign(window.ENEMY_TYPES, window.REGIONAL_ENEMY_TYPES);

// 敌军兵种 — 基础类型
window.ENEMY_TYPES_BASE = {
    yellow: { name: "黄巾卒", hp: 68, speed: 78, gold: 9, size: 14, color: "#c9a86a", weapon: "club", scale: 1.0 },
    saber: { name: "环刀手", hp: 95, speed: 71, gold: 10, size: 14, color: "#6b4a3a", weapon: "saber", scale: 1.0 },
    spear: { name: "枪阵卒", hp: 138, speed: 64, gold: 13, size: 15, color: "#7a3a2a", weapon: "spear", scale: 1.05 },
    qingzhou: { name: "青州兵", hp: 175, speed: 60, gold: 16, size: 15, color: "#5a4a38", weapon: "saber", scale: 1.06, armor: 0.1 },
    halberdier: { name: "戟士", hp: 210, speed: 56, gold: 18, size: 16, color: "#4a2a1a", weapon: "halberd", scale: 1.08, armor: 0.14 },
    scout: { name: "斥候", hp: 52, speed: 128, gold: 8, size: 12, color: "#5c5a48", weapon: "bow", scale: 0.92 },
    crossbow: { name: "弩兵", hp: 125, speed: 50, gold: 14, size: 14, color: "#4a5048", weapon: "crossbow", scale: 1.0, armor: 0.12 },
    shield: { name: "重盾甲士", hp: 380, speed: 40, gold: 24, size: 17, color: "#365e8a", weapon: "shield", scale: 1.15, armor: 0.37 },
    rattan: { name: "藤甲兵", hp: 410, speed: 44, gold: 26, size: 17, color: "#2d4a2a", weapon: "rattan", scale: 1.1, armor: 0.5, fireVulnerable: true, fireVulnerableMul: 1.65 },
    rider: { name: "西凉铁骑", hp: 230, speed: 118, gold: 20, size: 18, color: "#8a2a2a", weapon: "lance", scale: 1.2, armor: 0.06 },
    tiger: { name: "虎豹骑", hp: 305, speed: 108, gold: 28, size: 19, color: "#3a1a1a", weapon: "tiger_lance", scale: 1.22, armor: 0.24 },
    baggage: { name: "辎重队", hp: 780, speed: 32, gold: 52, size: 24, color: "#6a5030", weapon: "baggage", scale: 1.25, armor: 0.38 },
    siege: { name: "冲城车", hp: 1040, speed: 29, gold: 66, size: 28, color: "#5a3a1a", weapon: "siege", scale: 1.45, armor: 0.52 },
    wudang: { name: "无当飞军", hp: 255, speed: 86, gold: 22, size: 16, color: "#3d5c40", weapon: "bow", scale: 1.08, armor: 0.16 },
    white_ear: { name: "白耳兵", hp: 295, speed: 72, gold: 25, size: 17, color: "#3a4a5a", weapon: "saber", scale: 1.08, armor: 0.26, plumeWhite: true },
    fangji: { name: "方天戟近卫", hp: 360, speed: 48, gold: 24, size: 18, color: "#221018", weapon: "fangji_halberd", scale: 1.18, armor: 0.34, pierceExtraCost: 1 },
    huojian: { name: "火箭手", hp: 86, speed: 70, gold: 12, size: 14, color: "#6a4538", weapon: "rocket_bow", scale: 1.0, armor: 0.06, splashVulnerableMul: 1.42, fireVulnerable: true, fireVulnerableMul: 1.48 },
    desperado: { name: "死士", hp: 72, speed: 98, gold: 11, size: 13, color: "#4a2028", weapon: "desperado", scale: 0.98, armor: 0.05, deathNovaRadius: 52, deathNovaDamage: 42 },
    junyi: { name: "军医吏", hp: 168, speed: 46, gold: 15, size: 14, color: "#3a5a4a", weapon: "medic_satchel", scale: 1.0, armor: 0.1, regenPerSec: 8 },
    boss: { name: "敌将", hp: 2850, speed: 38, gold: 245, size: 28, color: "#2a1a4a", weapon: "boss", scale: 1.6, armor: 0.43, isBoss: true }
};

Object.assign(window.ENEMY_TYPES, window.ENEMY_TYPES_BASE);

window.LEVELS = LevelFactory.buildAll();
