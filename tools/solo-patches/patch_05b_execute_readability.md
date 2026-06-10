# Patch 05b — Execute 可读性（手动合并说明）

SOLO 建议：自动化截帧容易错过 execute 峰值，通过延长可见窗口 + 高亮核心斩击线提升可读性。

## 1. `app/src/main/assets/www/js/entities/effect.js`

```diff
- duration: 0.6
+ duration: 0.75
```

位置：`case "execute"` 内 `game.effects.push({ kind: "execute", ... })`

## 2. `app/src/main/assets/www/js/art.js` — `case "execute"`

将 `(1 - t)` 替换为 `hold` 缓出曲线，并加大斩击线：

```javascript
case "execute": {
    const life = Math.min(1, ef.elapsed / ef.duration);
    const hold = 1 - Math.pow(life, 0.65);
    ctx.translate(ef.x, ef.y);
    ctx.rotate(ef.elapsed * 6);
    ctx.strokeStyle = `rgba(255,213,90,${hold})`;
    ctx.lineWidth = 8;
    ctx.beginPath();
    ctx.moveTo(-62, 0); ctx.lineTo(62, 0);
    ctx.moveTo(0, -62); ctx.lineTo(0, 62);
    ctx.stroke();
    ctx.fillStyle = `rgba(255,255,255,${0.8 * hold})`;
    ctx.beginPath(); ctx.arc(0, 0, 12, 0, Math.PI * 2); ctx.fill();
    // 斩击残影
    ctx.strokeStyle = `rgba(255,235,175,${0.70 * hold})`;
    ctx.lineWidth = 3;
    ctx.beginPath();
    ctx.moveTo(-62, -62); ctx.lineTo(62, 62);
    ctx.moveTo(-62, 62); ctx.lineTo(62, -62);
    ctx.stroke();
    // 高亮核心斩击线
    ctx.strokeStyle = `rgba(255,255,255,${0.35 * hold})`;
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(-62, -62); ctx.lineTo(62, 62);
    ctx.moveTo(-62, 62); ctx.lineTo(62, -62);
    ctx.stroke();
    break;
}
```

## 验证

```powershell
node tools\ult-execute-capture.mjs
# 期望：execute_100ms.png 可见金色 X 斩击
```

上次真机结果（SM-F9660）：100ms 帧可读，150ms 仍 faint 可见。
