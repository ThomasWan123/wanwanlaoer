// 由战役表生成 30 名 BOSS 可玩武将（完整三级+大招）
(function () {
    const BOSS_ULT_NAMES = {
        zhangjiao: { name: "黄天当立", short: "路径奇门禁锢", desc: "布下黄天奇门，5 秒内路径禁锢并持续伤害" },
        huaxiong: { name: "温酒斩将", short: "单体斩杀", desc: "对最强敌造成 300 真实伤害" },
        caobao: { name: "据城反扑", short: "范围震慑", desc: "震慑周围敌军，眩晕 3 秒" },
        jiling: { name: "袁术先锋", short: "重击溅射", desc: "重击震动，眩晕 3 秒" },
        yuanshao: { name: "四世三公", short: "洪水减速", desc: "洪水灌路，减速并造成真实伤害" },
        yanliang: { name: "河北第一", short: "冲阵斩杀", desc: "对最强敌造成 290 真实伤害" },
        wenchou: { name: "延津追击", short: "路径冲杀", desc: "沿路径冲杀，每次 52 伤害" },
        chunyuqiong: { name: "乌巢火起", short: "全路径火焰", desc: "点燃粮道，火焰 5 秒" },
        shenpei: { name: "死守官渡", short: "奇门困敌", desc: "布阵困敌 5 秒" },
        caoren: { name: "坚壁清野", short: "范围震慑", desc: "坚守不出，震慑敌军 3 秒" },
        xiahoudun: { name: "盲夏侯", short: "单体斩杀", desc: "对最强敌造成 310 真实伤害" },
        yuejin: { name: "先登陷阵", short: "路径冲杀", desc: "陷阵冲锋，来回撞击" },
        huanggai: { name: "苦肉计火", short: "全路径火焰", desc: "火攻连环，火焰 5 秒" },
        caocao: { name: "横槊赋诗", short: "奇门困敌", desc: "运筹帷幄，困敌 5 秒" },
        jiangqin: { name: "江东猛将", short: "路径冲杀", desc: "水师登陆，冲阵破敌" },
        zhangliao: { name: "逍遥津", short: "路径冲杀", desc: "八百破吴，沿路径冲杀" },
        caimao: { name: "水军都督", short: "洪水减速", desc: "水师封锁，洪水减速" },
        zhangren: { name: "落凤坡箭", short: "单体斩杀", desc: "冷箭穿心，280 真实伤害" },
        liuzhang: { name: "益州牧", short: "奇门困敌", desc: "据城困敌 5 秒" },
        xiahouyuan: { name: "疾风迅雷", short: "路径冲杀", desc: "西征冲杀，来回撞击" },
        yujin: { name: "降将受辱", short: "洪水减速", desc: "水淹三军，减速 4 秒" },
        lvmeng: { name: "白衣渡江", short: "路径冲杀", desc: "奇袭麦城，冲阵破敌" },
        luxun: { name: "火烧连营", short: "全路径火焰", desc: "夷陵火海，火焰 5 秒" },
        menghuo: { name: "蛮王怒吼", short: "破阵斩杀", desc: "蛮王怒吼，对最强敌造成 285 真实伤害" },
        masu: { name: "纸上谈兵", short: "奇门困敌", desc: "布阵失误，仍困敌 4 秒" },
        guohuai: { name: "陇西铁骑", short: "路径冲杀", desc: "铁骑冲阵" },
        simayi: { name: "鹰视狼顾", short: "奇门困敌", desc: "深沟高垒，困敌 5 秒" },
        dengai: { name: "阴平奇袭", short: "路径冲杀", desc: "偷渡阴平，奇袭冲杀" },
        simayan: { name: "受禅称帝", short: "单体斩杀", desc: "一统天下，350 真实伤害" }
    };

    const BOSS_TITLES = {
        zhangjiao: "天公将军", huaxiong: "都督", lvbu: "飞将",
        caobao: "徐州叛将", jiling: "袁术大将", yuanshao: "河北霸主",
        yanliang: "河北名将", wenchou: "河北猛将", chunyuqiong: "乌巢守将",
        shenpei: "官渡谋士", caoren: "曹魏名将", xiahoudun: "盲夏侯",
        yuejin: "先登营", huanggai: "东吴老将", caocao: "魏武帝",
        jiangqin: "东吴猛将", zhangliao: "五子良将", caimao: "水军督",
        zhangren: "西川名将", liuzhang: "益州牧", xiahouyuan: "征西将军",
        yujin: "降将", lvmeng: "东吴元帅", luxun: "书生拜将",
        menghuo: "南蛮王", masu: "街亭守将", guohuai: "雍凉名将",
        simayi: "晋宣帝", dengai: "征西名将", simayan: "晋武帝"
    };

    const CORE_IDS = new Set(["guanyu", "zhangfei", "zhaoyun", "zhugeliang", "lvbu", "zhouyu"]);

    function cloneLevels(arch) {
        return arch.levels.map(l => Object.assign({}, l));
    }

    function buildBossGeneral(meta) {
        if (CORE_IDS.has(meta.bossGeneralId) && meta.bossGeneralId === "lvbu") return null;
        const arch = GENERAL_ARCHETYPES[meta.archetype] || GENERAL_ARCHETYPES.melee;
        const fc = FACTION_COLORS[meta.faction] || FACTION_COLORS["群"];
        const ultCustom = BOSS_ULT_NAMES[meta.bossGeneralId];
        const ultBase = arch.ult;
        const tier = meta.tier || 1;
        const cost = 70 + tier * 12;

        return {
            id: meta.bossGeneralId,
            name: meta.bossName,
            title: BOSS_TITLES[meta.bossGeneralId] || meta.bossName,
            faction: meta.faction,
            color: fc.color,
            accent: fc.accent,
            archetype: meta.archetype,
            attackType: arch.attackType,
            projectileType: arch.projectileType,
            cost: cost,
            range: arch.levels[0].range,
            levels: cloneLevels(arch),
            ultimate: {
                name: ultCustom ? ultCustom.name : ultBase.name,
                short: ultCustom ? ultCustom.short : ultBase.short,
                cost: 100,
                desc: ultCustom ? ultCustom.desc : ultBase.desc,
                type: ultBase.type
            },
            story: `《三国演义》中${meta.bossName}于「${meta.name}」一战成名，收服后可为我军大将。`,
            unlockLevel: CAMPAIGN_META.indexOf(meta) + 1,
            isBossGeneral: true
        };
    }

    window.BOSS_GENERALS = [];
    for (const meta of CAMPAIGN_META) {
        if (meta.bossGeneralId === "lvbu") continue;
        const g = buildBossGeneral(meta);
        if (g) BOSS_GENERALS.push(g);
    }

    window.MILESTONE_GENERAL_DEFS = [{
        id: "lusu",
        name: "鲁肃",
        title: "东吴谋臣",
        faction: "吴",
        color: "#1f6db5",
        accent: "#a8d4ff",
        archetype: "magic",
        attackType: "magic",
        projectileType: "fan",
        cost: 105,
        range: 175,
        levels: [
            { dmg: 14, rate: 1.0, range: 175, slow: 0.3, slowDur: 1.4, upgradeCost: 100 },
            { dmg: 24, rate: 0.92, range: 188, slow: 0.4, slowDur: 1.7, upgradeCost: 150 },
            { dmg: 41, rate: 0.86, range: 200, slow: 0.48, slowDur: 2.0, upgradeCost: 0 }
        ],
        ultimate: {
            name: "联刘抗曹",
            short: "全路径减速",
            cost: 100,
            desc: "外交斡旋，全路径敌人减速 50% 持续 4 秒",
            type: "flood"
        },
        story: "赤壁之前，鲁肃力主联刘，奠定东吴国策。",
        unlockMilestone: 14
    }];
})();
