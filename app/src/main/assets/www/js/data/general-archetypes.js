// 可玩武将战斗模板（BOSS 将生成用）
window.archetypeForGeneral = function (g) {
    if (!g) return "melee";
    return g.archetype || g.attackType || "melee";
};

window.GENERAL_ARCHETYPES = {
    melee: {
        attackType: "melee", projectileType: "slash",
        levels: [
            { dmg: 26, rate: 1.1, range: 125, upgradeCost: 75 },
            { dmg: 48, rate: 1.0, range: 135, upgradeCost: 115 },
            { dmg: 88, rate: 0.9, range: 145, upgradeCost: 0 }
        ],
        ult: { type: "execute", name: "破阵斩将", short: "对最强敌巨额真实伤害", desc: "锁定战场最强敌人，造成 280 真实伤害" }
    },
    splash: {
        attackType: "splash", projectileType: "shock",
        levels: [
            { dmg: 17, rate: 1.35, range: 115, splash: 48, upgradeCost: 85 },
            { dmg: 30, rate: 1.25, range: 125, splash: 58, upgradeCost: 130 },
            { dmg: 55, rate: 1.15, range: 135, splash: 72, upgradeCost: 0 }
        ],
        ult: { type: "stun", name: "震军怒吼", short: "全场眩晕 3 秒", desc: "震慑全场敌人，眩晕 3 秒并造成 45 伤害" }
    },
    rapid: {
        attackType: "rapid", projectileType: "spear",
        levels: [
            { dmg: 9, rate: 0.48, range: 155, upgradeCost: 80 },
            { dmg: 16, rate: 0.42, range: 165, upgradeCost: 125 },
            { dmg: 26, rate: 0.34, range: 175, upgradeCost: 0 }
        ],
        ult: { type: "charge", name: "铁骑冲阵", short: "沿路径冲杀", desc: "沿路径冲杀两个来回，每次撞击 55 伤害" }
    },
    magic: {
        attackType: "magic", projectileType: "fan",
        levels: [
            { dmg: 13, rate: 1.05, range: 175, slow: 0.32, slowDur: 1.4, upgradeCost: 95 },
            { dmg: 22, rate: 0.95, range: 188, slow: 0.42, slowDur: 1.7, upgradeCost: 145 },
            { dmg: 40, rate: 0.88, range: 200, slow: 0.5, slowDur: 2.0, upgradeCost: 0 }
        ],
        ult: { type: "maze", name: "奇门困敌", short: "路径禁锢伤害", desc: "布下奇门，5 秒内每秒 22 伤害并减速" }
    },
    pierce: {
        attackType: "pierce", projectileType: "halberd",
        levels: [
            { dmg: 40, rate: 1.05, range: 165, pierce: 2, upgradeCost: 110 },
            { dmg: 72, rate: 0.95, range: 175, pierce: 3, upgradeCost: 165 },
            { dmg: 118, rate: 0.88, range: 188, pierce: 4, upgradeCost: 0 }
        ],
        ult: { type: "execute", name: "贯甲一戟", short: "单体斩杀", desc: "对最强敌人造成 320 真实伤害" }
    },
    blaze: {
        attackType: "splash", projectileType: "fire",
        levels: [
            { dmg: 20, rate: 1.28, range: 145, splash: 52, upgradeCost: 100 },
            { dmg: 36, rate: 1.18, range: 155, splash: 65, upgradeCost: 155 },
            { dmg: 65, rate: 1.08, range: 168, splash: 85, upgradeCost: 0 }
        ],
        ult: { type: "blaze", name: "燎原烈火", short: "全路径火焰 5 秒", desc: "点燃路径，每秒 55 伤害持续 5 秒" }
    },
    charge: {
        attackType: "rapid", projectileType: "lance",
        levels: [
            { dmg: 12, rate: 0.55, range: 140, upgradeCost: 90 },
            { dmg: 21, rate: 0.48, range: 150, upgradeCost: 140 },
            { dmg: 34, rate: 0.4, range: 162, upgradeCost: 0 }
        ],
        ult: { type: "charge", name: "奔雷突", short: "路径冲杀", desc: "沿路径来回冲杀，每次 58 伤害" }
    },
    flood: {
        attackType: "magic", projectileType: "wave",
        levels: [
            { dmg: 15, rate: 1.0, range: 170, slow: 0.38, slowDur: 1.6, upgradeCost: 95 },
            { dmg: 26, rate: 0.92, range: 182, slow: 0.48, slowDur: 1.9, upgradeCost: 140 },
            { dmg: 44, rate: 0.85, range: 195, slow: 0.55, slowDur: 2.1, upgradeCost: 0 }
        ],
        ult: { type: "flood", name: "水淹三军", short: "全路径洪水减速", desc: "洪水灌路，80~180 真实伤害并减速 60% 持续 4 秒" }
    }
};

window.ARCHETYPE_ROLE_LABEL = {
    melee: "武将",
    splash: "统帅",
    rapid: "先锋",
    magic: "谋略",
    pierce: "破甲",
    blaze: "火攻",
    charge: "突骑",
    flood: "水军"
};

window.ULTIMATE_TYPE_LABEL = {
    flood: "水军",
    blaze: "火攻",
    stun: "震慑",
    maze: "谋略",
    execute: "斩杀",
    charge: "突骑",
    rally: "仁德",
    hex: "奇门",
    tide: "水军"
};

window.FACTION_COLORS = {
    "群": { color: "#7a4f1c", accent: "#f7d774" },
    "魏": { color: "#1a3a6a", accent: "#8ab4ff" },
    "蜀": { color: "#2a8f3f", accent: "#a8e6a8" },
    "吴": { color: "#1f6db5", accent: "#ff9b4a" },
    "汉": { color: "#6a5030", accent: "#e8d89a" },
    "晋": { color: "#4a3a5a", accent: "#c9b8ff" }
};
