// 30 关战役元数据（BOSS id 全局唯一）
window.CAMPAIGN_META = [
    { id: "yingchuan", name: "颍川黄巾", desc: "黄巾初起，天下震动", region: "central", terrain: "plain", weather: "heat", mapTheme: "central_plain", bossName: "张角", bossGeneralId: "zhangjiao", faction: "群", archetype: "magic", enemyPool: ["yellow", "saber", "scout"], tier: 1, pathSeed: 0 },
    { id: "sishui", name: "汜水关", desc: "诸侯会盟，斩将夺关", region: "west", terrain: "mountain", weather: "clear", mapTheme: "west_pass", bossName: "华雄", bossGeneralId: "huaxiong", faction: "群", archetype: "melee", enemyPool: ["rider", "halberdier", "fangji"], tier: 1, pathSeed: 1 },
    { id: "huluguan", name: "虎牢关", desc: "三英战吕布，威震天下", region: "central", terrain: "plain", weather: "wind", mapTheme: "central_fort", bossName: "吕布", bossGeneralId: "lvbu", faction: "群", archetype: "pierce", enemyPool: ["fangji", "rider", "yellow", "halberdier"], tier: 1, pathSeed: 2, useLegacyPath: 0 },
    { id: "xuzhou", name: "徐州争锋", desc: "刘备据徐，曹军来犯", region: "east", terrain: "city", weather: "clear", mapTheme: "east_city", bossName: "曹豹", bossGeneralId: "caobao", faction: "群", archetype: "melee", enemyPool: ["spear", "saber", "baggage"], tier: 1, pathSeed: 3 },
    { id: "xiaopei", name: "小沛据守", desc: "袁术攻备，死守孤城", region: "central", terrain: "plain", weather: "wind", mapTheme: "central_plain", bossName: "纪灵", bossGeneralId: "jiling", faction: "群", archetype: "splash", enemyPool: ["shield", "spear", "crossbow"], tier: 1, pathSeed: 4 },
    { id: "guandu_front", name: "官渡前哨", desc: "袁绍大军压境", region: "north", terrain: "plain", weather: "cold", mapTheme: "north_plain", bossName: "袁绍", bossGeneralId: "yuanshao", faction: "群", archetype: "splash", enemyPool: ["qingzhou", "bingzhou_rider", "junyi"], tier: 2, pathSeed: 5 },
    { id: "baima", name: "白马坡", desc: "斩颜良，诛文丑前奏", region: "north", terrain: "plain", weather: "clear", mapTheme: "north_plain", bossName: "颜良", bossGeneralId: "yanliang", faction: "魏", archetype: "melee", enemyPool: ["rider", "spear", "tiger"], tier: 2, pathSeed: 6 },
    { id: "yanjin", name: "延津渡", desc: "渡口雾起，追击不断", region: "central", terrain: "river", weather: "fog", mapTheme: "central_river", bossName: "文丑", bossGeneralId: "wenchou", faction: "魏", archetype: "melee", enemyPool: ["rider", "scout", "desperado"], tier: 2, pathSeed: 7, modifierExtra: { riderSpeedMul: 1.1, scoutSpeedMul: 1.08 } },
    { id: "wuchao", name: "乌巢劫粮", desc: "劫烧粮草，断敌后路", region: "north", terrain: "camp", weather: "cold", mapTheme: "north_camp", bossName: "淳于琼", bossGeneralId: "chunyuqiong", faction: "魏", archetype: "blaze", enemyPool: ["baggage", "huojian", "saber"], tier: 2, pathSeed: 8 },
    { id: "guandu", name: "官渡决战", desc: "以少胜多，奠定北方", region: "north", terrain: "plain", weather: "dust", mapTheme: "north_dust", bossName: "审配", bossGeneralId: "shenpei", faction: "群", archetype: "magic", enemyPool: ["siege", "shield", "crossbow", "desperado"], tier: 2, pathSeed: 9 },
    { id: "xinye", name: "新野火攻", desc: "诸葛亮火烧曹军", region: "central", terrain: "city", weather: "clear", mapTheme: "central_city", bossName: "曹仁", bossGeneralId: "caoren", faction: "魏", archetype: "splash", enemyPool: ["huojian", "spear", "shield"], tier: 2, pathSeed: 10 },
    { id: "changbanpo", name: "长坂坡", desc: "百万军中，单骑救主", region: "north", terrain: "plain", weather: "clear", mapTheme: "north_chase", bossName: "夏侯惇", bossGeneralId: "xiahoudun", faction: "魏", archetype: "pierce", enemyPool: ["tiger", "scout", "rider"], tier: 2, pathSeed: 11, useLegacyPath: 1, modifierExtra: { riderSpeedMul: 1.12, scoutSpeedMul: 1.08 } },
    { id: "dangyang", name: "当阳桥", desc: "长坂桥头，一声喝退", region: "north", terrain: "river", weather: "fog", mapTheme: "north_bridge", bossName: "乐进", bossGeneralId: "yuejin", faction: "魏", archetype: "rapid", enemyPool: ["tiger", "spear", "crossbow"], tier: 2, pathSeed: 12 },
    { id: "jiangdong", name: "舌战江东", desc: "赤壁之前，东吴备战", region: "east", terrain: "coast", weather: "rain", mapTheme: "east_coast", bossName: "黄盖", bossGeneralId: "huanggai", faction: "吴", archetype: "blaze", enemyPool: ["jiangdong_navy", "crossbow", "shield"], tier: 3, pathSeed: 13 },
    { id: "chibi", name: "赤壁", desc: "火烧连营，八十万灰飞", region: "east", terrain: "river", weather: "wind", mapTheme: "east_fire", bossName: "曹操", bossGeneralId: "caocao", faction: "魏", archetype: "magic", enemyPool: ["rattan", "huojian", "shield", "rider"], tier: 3, pathSeed: 14, useLegacyPath: 2, modifierExtra: { fireDamageMul: 1.22 } },
    { id: "nanjun", name: "南郡争夺", desc: "赤壁余波，四郡未定", region: "south", terrain: "river", weather: "rain", mapTheme: "south_river", bossName: "蒋钦", bossGeneralId: "jiangqin", faction: "吴", archetype: "rapid", enemyPool: ["jiangdong_navy", "crossbow", "wudang"], tier: 3, pathSeed: 15 },
    { id: "hefei", name: "合肥之战", desc: "逍遥津前，张辽扬威", region: "east", terrain: "city", weather: "clear", mapTheme: "east_night", bossName: "张辽", bossGeneralId: "zhangliao", faction: "魏", archetype: "charge", enemyPool: ["tiger", "desperado", "scout"], tier: 3, pathSeed: 16 },
    { id: "jingzhou", name: "荆州水战", desc: "借荆立足，蔡瑁来犯", region: "south", terrain: "river", weather: "rain", mapTheme: "south_flood", bossName: "蔡瑁", bossGeneralId: "caimao", faction: "群", archetype: "magic", enemyPool: ["crossbow", "jiangdong_navy", "shield"], tier: 3, pathSeed: 17 },
    { id: "yizhou_gate", name: "益州门户", desc: "入蜀要道，张任死守", region: "west", terrain: "mountain", weather: "heat", mapTheme: "west_mountain", bossName: "张任", bossGeneralId: "zhangren", faction: "群", archetype: "pierce", enemyPool: ["shanxi_pike", "halberdier", "crossbow"], tier: 3, pathSeed: 18 },
    { id: "luocheng", name: "雒城血战", desc: "平定益州，刘璋请降", region: "west", terrain: "city", weather: "clear", mapTheme: "west_city", bossName: "刘璋", bossGeneralId: "liuzhang", faction: "群", archetype: "magic", enemyPool: ["shield", "spear", "junyi"], tier: 3, pathSeed: 19 },
    { id: "hanzhong", name: "汉中之战", desc: "定军山下，夏侯陨命", region: "west", terrain: "mountain", weather: "clear", mapTheme: "west_hill", bossName: "夏侯渊", bossGeneralId: "xiahouyuan", faction: "魏", archetype: "rapid", enemyPool: ["white_ear", "tiger", "qiang_rider"], tier: 4, pathSeed: 20 },
    { id: "xiangfan", name: "襄樊围城", desc: "关羽北伐，于禁来援", region: "south", terrain: "plain", weather: "rain", mapTheme: "south_rain", bossName: "于禁", bossGeneralId: "yujin", faction: "魏", archetype: "melee", enemyPool: ["shield", "siege", "crossbow"], tier: 4, pathSeed: 21 },
    { id: "maicheng", name: "麦城绝响", desc: "荆州失守，关羽危局", region: "south", terrain: "forest", weather: "fog", mapTheme: "south_forest", bossName: "吕蒙", bossGeneralId: "lvmeng", faction: "吴", archetype: "rapid", enemyPool: ["wudang", "scout", "desperado"], tier: 4, pathSeed: 22 },
    { id: "yiling", name: "夷陵火海", desc: "火烧连营，蜀军大败", region: "south", terrain: "forest", weather: "wind", mapTheme: "south_fire", bossName: "陆逊", bossGeneralId: "luxun", faction: "吴", archetype: "blaze", enemyPool: ["rattan", "huojian", "nanman"], tier: 4, pathSeed: 23, modifierExtra: { fireDamageMul: 1.25 } },
    { id: "nanman", name: "七擒孟获", desc: "南征蛮夷，七纵七擒", region: "south", terrain: "swamp", weather: "heat", mapTheme: "south_swamp", bossName: "孟获", bossGeneralId: "menghuo", faction: "群", archetype: "melee", enemyPool: ["nanman", "rattan", "nanman_elite"], tier: 4, pathSeed: 24 },
    { id: "jieting", name: "街亭失守", desc: "马谡失街，蜀军受挫", region: "west", terrain: "mountain", weather: "clear", mapTheme: "west_pass", bossName: "马谡", bossGeneralId: "masu", faction: "蜀", archetype: "magic", enemyPool: ["qiang_rider", "spear", "scout"], tier: 4, pathSeed: 25 },
    { id: "qishan", name: "祁山北伐", desc: "六出祁山，郭淮来战", region: "west", terrain: "mountain", weather: "snow", mapTheme: "west_snow", bossName: "郭淮", bossGeneralId: "guohuai", faction: "魏", archetype: "rapid", enemyPool: ["xiliang_archer", "shield", "junyi"], tier: 4, pathSeed: 26 },
    { id: "wuzhangyuan", name: "五丈原", desc: "秋风五丈，星落军心", region: "central", terrain: "plain", weather: "cold", mapTheme: "central_winter", bossName: "司马懿", bossGeneralId: "simayi", faction: "魏", archetype: "magic", enemyPool: ["crossbow", "shield", "siege"], tier: 4, pathSeed: 27 },
    { id: "jiangwei", name: "姜维继志", desc: "九伐中原，邓艾出奇", region: "west", terrain: "mountain", weather: "clear", mapTheme: "west_trail", bossName: "邓艾", bossGeneralId: "dengai", faction: "魏", archetype: "charge", enemyPool: ["qiang_rider", "bingzhou_rider", "desperado"], tier: 4, pathSeed: 28 },
    { id: "guijin", name: "三分归晋", desc: "三国终归，司马受禅", region: "central", terrain: "city", weather: "clear", mapTheme: "central_capital", bossName: "司马炎", bossGeneralId: "simayan", faction: "晋", archetype: "melee", enemyPool: ["tiger", "white_ear", "shield", "rider", "crossbow", "wudang", "halberdier", "desperado"], tier: 4, pathSeed: 29 }
];

// 里程碑赠送（不与 BOSS id 重复；吕布由第3关 BOSS 解锁）
window.MILESTONE_GENERALS = [
    { unlockAfterLevel: 2, id: "zhangfei" },
    { unlockAfterLevel: 3, id: "liubei" },
    { unlockAfterLevel: 5, id: "zhaoyun" },
    { unlockAfterLevel: 8, id: "zhugeliang" },
    { unlockAfterLevel: 11, id: "zhouyu" },
    { unlockAfterLevel: 14, id: "lusu" },
    { unlockAfterLevel: 15, id: "sunquan" },
    { unlockAfterLevel: 16, id: "caocao" }
];

window.STARTER_GENERAL_IDS = ["guanyu"];

window.Campaign = {
    LEVEL_COUNT: 30,
    MAX_LINEUP: 6,

    assertUniqueBossIds() {
        const seen = new Set();
        for (const m of CAMPAIGN_META) {
            if (seen.has(m.bossGeneralId)) throw new Error("Duplicate bossGeneralId: " + m.bossGeneralId);
            seen.add(m.bossGeneralId);
        }
    },

    getMeta(index) {
        return CAMPAIGN_META[index];
    },

    starsForTier(tier) {
        return "★".repeat(Math.min(5, tier + 1));
    },

    bossUnlockMessage(bossGeneralId) {
        const g = window.getGeneral ? window.getGeneral(bossGeneralId) : GENERALS.find(x => x.id === bossGeneralId);
        return g ? `${g.name}已加入我军！` : "新将已加入！";
    }
};

Campaign.assertUniqueBossIds();
