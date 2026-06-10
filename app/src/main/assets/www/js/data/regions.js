// 区域 / 地形 / 天气 → modifiers 与 UI 标签
window.Regions = {
    REGION_LABEL: { north: "北地", south: "南疆", east: "江东", west: "西蜀", central: "中原" },
    TERRAIN_LABEL: { plain: "平原", mountain: "山地", river: "河流", swamp: "沼泽", forest: "丛林", coast: "海岸", city: "城池", camp: "营寨", snow: "雪原" },
    WEATHER_LABEL: { clear: "晴", rain: "雨", fog: "雾", wind: "大风", heat: "暑热", snow: "雪", dust: "沙尘", cold: "严寒" },

    buildUiTags(meta) {
        const r = this.REGION_LABEL[meta.region] || meta.region;
        const t = this.TERRAIN_LABEL[meta.terrain] || meta.terrain;
        const w = this.WEATHER_LABEL[meta.weather] || meta.weather;
        return [r, t, w];
    },

    applyEnvironment(meta) {
        const m = { desc: "" };
        const parts = [];

        if (meta.region === "north") {
            m.riderSpeedMul = 1.08;
            parts.push("北地骑兵迅捷");
        }
        if (meta.region === "south") {
            parts.push("南境密林");
            if (meta.weather === "rain" || meta.weather === "heat") m.fireDamageMul = 0.88;
        }
        if (meta.region === "east") {
            parts.push("江东水网");
            if (meta.weather === "rain") m.crossbowRateMul = 1.15;
        }
        if (meta.region === "west") {
            m.baggageSpeedMul = 0.88;
            parts.push("蜀道艰阻");
        }

        if (meta.weather === "fog") {
            m.scoutSpeedMul = 1.1;
            m.enemySpeedMul = (m.enemySpeedMul || 1) * 1.03;
            parts.push("大雾：斥候更疾");
        }
        if (meta.weather === "wind") {
            m.fireDamageMul = (m.fireDamageMul || 1) * 1.18;
            parts.push("大风助火");
        }
        if (meta.weather === "rain") {
            m.crossbowRateMul = 1.12;
            parts.push("暴雨：弩矢稍缓");
        }
        if (meta.weather === "snow" || meta.weather === "cold") {
            m.enemySpeedMul = (m.enemySpeedMul || 1) * 0.94;
            parts.push("严寒：敌军迟缓");
        }
        if (meta.weather === "dust") {
            m.enemySpeedMul = (m.enemySpeedMul || 1) * 0.96;
            parts.push("沙尘蔽日");
        }
        if (meta.weather === "heat") {
            m.enemySpeedMul = (m.enemySpeedMul || 1) * 0.97;
            parts.push("暑热：双方易疲");
        }

        if (meta.terrain === "river" || meta.terrain === "coast") parts.push("近水作战");
        if (meta.terrain === "mountain" || meta.terrain === "forest") parts.push("地利复杂");

        if (meta.modifierExtra) Object.assign(m, meta.modifierExtra);
        m.desc = parts.join("；") || "寻常战阵";
        return m;
    }
};
