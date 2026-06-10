// 合并核心将、BOSS 将、里程碑将
(function () {
    const core = window.GENERALS.slice();
    const coreIds = new Set(core.map(g => g.id));
    const merged = core.slice();
    for (const g of (window.BOSS_GENERALS || [])) {
        if (!coreIds.has(g.id)) merged.push(g);
    }
    for (const g of (window.MILESTONE_GENERAL_DEFS || [])) {
        if (!coreIds.has(g.id) && !merged.some(x => x.id === g.id)) merged.push(g);
    }
    window.GENERALS = merged;

    window.getGeneral = function (id) {
        return GENERALS.find(g => g.id === id) || null;
    };

    window.isGeneralUnlocked = function (id) {
        if (!window.Progress) return true;
        return Progress.getUnlockedGeneralIds().includes(id);
    };
})();
