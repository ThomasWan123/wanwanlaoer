// 武将数据 —— 致敬《三国·谋定天下》武将设定
// 每位武将三级成长 + 大招（怒气槽满后释放）
// stats 结构: damage(单发伤害) range(射程,像素) rate(攻击间隔,秒) cost(部署/升级费)
window.GENERALS = [
    // ============ 蜀 · 关羽（重击/单体高伤） ============
    {
        id: "guanyu",
        name: "关羽",
        title: "武圣 · 美髯公",
        faction: "蜀",
        color: "#2a8f3f",
        accent: "#f7d774",
        archetype: "flood",
        attackType: "melee",          // 近战重击
        projectileType: "slash",
        cost: 80,
        range: 130,
        levels: [
            { dmg: 28, rate: 1.1, range: 130, upgradeCost: 80 },
            { dmg: 52, rate: 1.0, range: 140, upgradeCost: 120 },
            { dmg: 95, rate: 0.9, range: 150, upgradeCost: 0 }
        ],
        ultimate: {
            name: "水淹七军",
            short: "全路径真实伤害 + 强减速 4 秒",
            cost: 100,
            desc: "召洪水冲灌全路径，对每个敌人造成 80 ~ 200 真实伤害并减速 60% 持续 4 秒",
            type: "flood"
        },
        story: "建安二十四年，水淹七军，擒于禁、斩庞德，威震华夏。"
    },
    // ============ 蜀 · 张飞（范围震慑） ============
    {
        id: "zhangfei",
        name: "张飞",
        title: "万人敌 · 燕人",
        faction: "蜀",
        color: "#2a8f3f",
        accent: "#aa3322",
        archetype: "splash",
        attackType: "splash",
        projectileType: "shock",
        cost: 100,
        range: 110,
        levels: [
            { dmg: 18, rate: 1.4, range: 110, splash: 50, upgradeCost: 100 },
            { dmg: 32, rate: 1.3, range: 120, splash: 60, upgradeCost: 160 },
            { dmg: 58, rate: 1.2, range: 130, splash: 75, upgradeCost: 0 }
        ],
        ultimate: {
            name: "当阳桥怒喝",
            short: "全场眩晕 3 秒 + 伤害",
            cost: 100,
            desc: "怒吼震慑全场敌人，眩晕 3 秒并造成 50 伤害",
            type: "stun"
        },
        story: "据水断桥，瞋目横矛，曰：可来共决死！敌皆不敢近。"
    },
    // ============ 蜀 · 赵云（高速多段） ============
    {
        id: "zhaoyun",
        name: "赵云",
        title: "常山 · 一身是胆",
        faction: "蜀",
        color: "#2a8f3f",
        accent: "#e0e0e0",
        archetype: "rapid",
        attackType: "rapid",
        projectileType: "spear",
        cost: 90,
        range: 160,
        levels: [
            { dmg: 10, rate: 0.45, range: 160, upgradeCost: 90 },
            { dmg: 17, rate: 0.40, range: 170, upgradeCost: 140 },
            { dmg: 28, rate: 0.32, range: 180, upgradeCost: 0 }
        ],
        ultimate: {
            name: "七进七出",
            short: "沿路径来回冲杀，撞击多段伤害",
            cost: 100,
            desc: "化身银影沿路径冲杀两个来回，每次撞击造成 60 伤害",
            type: "charge"
        },
        story: "长坂坡七进七出，怀抱阿斗，所向披靡。"
    },
    // ============ 蜀 · 诸葛亮（控场+减速） ============
    {
        id: "zhugeliang",
        name: "诸葛亮",
        title: "卧龙 · 智绝",
        faction: "蜀",
        color: "#2a8f3f",
        accent: "#7ad8ff",
        archetype: "magic",
        attackType: "magic",
        projectileType: "fan",
        cost: 110,
        range: 180,
        levels: [
            { dmg: 14, rate: 1.0, range: 180, slow: 0.35, slowDur: 1.5, upgradeCost: 110 },
            { dmg: 24, rate: 0.95, range: 195, slow: 0.45, slowDur: 1.8, upgradeCost: 170 },
            { dmg: 42, rate: 0.85, range: 210, slow: 0.55, slowDur: 2.2, upgradeCost: 0 }
        ],
        ultimate: {
            name: "八阵图",
            short: "阵内持续伤害 + 减速 5 秒",
            cost: 100,
            desc: "在路径上布下八阵，5 秒内禁锢途经敌人，每秒造成 25 伤害",
            type: "maze"
        },
        story: "八阵既成，自谓功盖三分国，名成八阵图。"
    },
    // ============ 蜀 · 刘备（仁德支援） ============
    {
        id: "liubei",
        name: "刘备",
        title: "昭烈帝 · 仁德",
        faction: "蜀",
        color: "#2a8f3f",
        accent: "#d4af6a",
        archetype: "magic",
        attackType: "magic",
        projectileType: "fan",
        cost: 95,
        range: 165,
        levels: [
            { dmg: 11, rate: 1.05, range: 165, slow: 0.22, slowDur: 1.2, upgradeCost: 88 },
            { dmg: 19, rate: 0.98, range: 175, slow: 0.32, slowDur: 1.5, upgradeCost: 135 },
            { dmg: 34, rate: 0.9, range: 185, slow: 0.4, slowDur: 1.8, upgradeCost: 0 }
        ],
        ultimate: {
            name: "仁德鼓舞",
            short: "全军怒满 + 路径减速 + 回复 1 城防",
            cost: 100,
            desc: "昭烈恩德鼓舞三军，所有武将怒气立刻蓄满，敌军沿路径减速，并稳固皇城 1 点城防",
            type: "rally"
        },
        story: "桃园结义，弘毅宽厚，得人心者得天下。"
    },
    // ============ 群 · 吕布（极致单体） ============
    {
        id: "lvbu",
        name: "吕布",
        title: "飞将 · 神威无敌",
        faction: "群",
        color: "#7a4f1c",
        accent: "#ffd24a",
        archetype: "pierce",
        attackType: "pierce",
        projectileType: "halberd",
        cost: 140,
        range: 170,
        levels: [
            { dmg: 45, rate: 1.0, range: 170, pierce: 2, upgradeCost: 140 },
            { dmg: 78, rate: 0.95, range: 180, pierce: 3, upgradeCost: 200 },
            { dmg: 130, rate: 0.85, range: 195, pierce: 4, upgradeCost: 0 }
        ],
        ultimate: {
            name: "方天破",
            short: "对当前最强敌单体 350 真实伤害",
            cost: 100,
            desc: "方天画戟横扫战场最强敌人，造成 350 真实伤害",
            type: "execute"
        },
        story: "人中吕布，马中赤兔，三英战之难分胜负。"
    },
    // ============ 吴 · 周瑜（火攻范围） ============
    {
        id: "zhouyu",
        name: "周瑜",
        title: "美周郎 · 火神",
        faction: "吴",
        color: "#1f6db5",
        accent: "#ff7a2e",
        archetype: "blaze",
        attackType: "splash",
        projectileType: "fire",
        cost: 120,
        range: 150,
        levels: [
            { dmg: 22, rate: 1.3, range: 150, splash: 55, upgradeCost: 120 },
            { dmg: 38, rate: 1.2, range: 160, splash: 70, upgradeCost: 180 },
            { dmg: 70, rate: 1.1, range: 175, splash: 90, upgradeCost: 0 }
        ],
        ultimate: {
            name: "火烧赤壁",
            short: "全路径持续火焰伤害 5 秒",
            cost: 100,
            desc: "于全路径点起赤焰，每秒 60 伤害持续 5 秒",
            type: "blaze"
        },
        story: "谈笑间，樯橹灰飞烟灭，三江口曹军八十万灰飞。"
    },
    // ============ 魏 · 曹操（控场削弱） ============
    {
        id: "caocao",
        name: "曹操",
        title: "魏武帝 · 奸雄",
        faction: "魏",
        color: "#3d5a80",
        accent: "#9ab0d0",
        archetype: "magic",
        attackType: "magic",
        projectileType: "fan",
        cost: 115,
        range: 170,
        levels: [
            { dmg: 12, rate: 1.0, range: 170, slow: 0.3, slowDur: 1.4, upgradeCost: 105 },
            { dmg: 21, rate: 0.92, range: 182, slow: 0.4, slowDur: 1.7, upgradeCost: 165 },
            { dmg: 38, rate: 0.85, range: 195, slow: 0.48, slowDur: 2.0, upgradeCost: 0 }
        ],
        ultimate: {
            name: "横槊赋诗",
            short: "全路径奇门减速 + 持续伤害",
            cost: 100,
            desc: "布下奇门困敌，5 秒内敌军沿路径大幅减速并持续受伤",
            type: "hex"
        },
        story: "对酒当歌，人生几何；横槊赋诗，气吞山河。"
    },
    // ============ 吴 · 孙权（水军支援） ============
    {
        id: "sunquan",
        name: "孙权",
        title: "吴大帝 · 紫髯",
        faction: "吴",
        color: "#1f6db5",
        accent: "#6ec8ff",
        archetype: "flood",
        attackType: "magic",
        projectileType: "wave",
        cost: 105,
        range: 168,
        levels: [
            { dmg: 13, rate: 1.02, range: 168, slow: 0.28, slowDur: 1.3, upgradeCost: 98 },
            { dmg: 23, rate: 0.94, range: 178, slow: 0.38, slowDur: 1.6, upgradeCost: 150 },
            { dmg: 40, rate: 0.86, range: 190, slow: 0.46, slowDur: 1.9, upgradeCost: 0 }
        ],
        ultimate: {
            name: "江东水师",
            short: "潮汐伤害 + 全军怒 + 赏金",
            cost: 100,
            desc: "江东潮汐冲刷敌军，造成真实伤害并减速，全军怒气提升并获得 45 金",
            type: "tide"
        },
        story: "据江东之地，任贤用能，与曹刘鼎足而三。"
    }
];
