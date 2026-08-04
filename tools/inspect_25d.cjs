// Inspect the 2.5D rendering pipeline in detail
const js = `(function(){
  var out = {};
  out.P = window.Projection ? {enabled:Projection.enabled, squash:Projection.Y_SQUASH, hscale:Projection.HEIGHT_SCALE, shadowAlpha:Projection.SHADOW_ALPHA, ox:Projection.SHADOW_OFFSET_X, oy:Projection.SHADOW_OFFSET_Y} : null;
  out.hasDrawBaseExtrusion = window.Projection ? typeof Projection.drawBaseExtrusion : 'none';
  out.towerHeightL1 = window.Projection ? Projection.heightFor('tower',{level:1}) : null;
  out.towerHeightL3 = window.Projection ? Projection.heightFor('tower',{level:3,mergeTier:2}) : null;
  out.enemyHeight = window.Projection ? Projection.heightFor('enemy',{weapon:'boss'}) : null;
  out.obstacleHeight = window.Projection ? Projection.heightFor('obstacle',{kind:'rock'}) : null;
  // Check drawEffect was upgraded - look for 3D ult markers in source
  var src = Art.drawEffect.toString();
  out.effectHasFloodLevel = src.indexOf('waterLevel') >= 0;
  out.effectHasBlazeColumn = src.indexOf('flameH') >= 0;
  out.effectHasMazePillar = src.indexOf('pillarH') >= 0;
  out.effectHasUltColumn = src.indexOf('burstH') >= 0;
  out.effectHasStunDome = src.indexOf('domeH') >= 0;
  // parallax
  out.parallaxSrc = Art._drawParallaxLayers ? 'present' : 'missing';
  out.parallaxHasSky = typeof Art._drawSkyLayer === 'function';
  out.parallaxHasFar = typeof Art._drawFarLayer === 'function';
  // current effects on field
  out.effects = Game.effects ? Game.effects.map(e=>e.kind) : [];
  // enemies info
  out.enemyKinds = Game.enemies ? Game.enemies.map(e=>e.typeKey).slice(0,5) : [];
  return JSON.stringify(out, null, 2);
})()`;
const ws = new WebSocket('ws://127.0.0.1:9222/devtools/page/CF994AC3BFEE3B87913FBD4387AF57E0');
ws.onopen = () => ws.send(JSON.stringify({id:1, method:'Runtime.evaluate', params:{expression:js, returnByValue:true}}));
ws.onmessage = (ev) => { const m=JSON.parse(ev.data); if(m.id===1){ console.log(m.result&&m.result.result&&m.result.result.value); process.exit(0);} };
ws.onerror = e => { console.log('err',e.message); process.exit(1); };
setTimeout(()=>{console.log('TIMEOUT');process.exit(1);},8000);
