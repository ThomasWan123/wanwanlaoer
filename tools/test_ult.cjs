// Promise-based: click replay if needed, then place tower + ult, await
const script = `(async function(){
  var out = {};
  // Click replay if available to get into running state
  var replay = document.getElementById('btn-replay');
  if (replay) { out.hadReplay=true; replay.click(); await new Promise(r=>setTimeout(r,600)); }
  else { out.hadReplay=false; }
  // resume from pause if pending
  if (Game.state==='paused') { var r=document.getElementById('btn-resume'); if(r) r.click(); await new Promise(r=>setTimeout(r,300)); }
  // place tower
  var g0 = typeof getGeneral==='function' ? getGeneral('guanyu') : null;
  if (Game.level && g0 && Game.towers.length===0 && Game.level.slots.length){
    var s=Game.level.slots[0];
    if (typeof Game.placeTower==='function') Game.placeTower(s,g0); // honor bounds
    if (Game.towers.length===0){
      var tw=new (window.Tower)(g0,s.x,s.y,0);
      Game.towers.push(tw); s.occupied=tw; out.forced=true;
    }
  }
  out.towers=Game.towers?Game.towers.length:-1;
  out.state=Game.state;
  // fill rage + ult
  if (Game.towers && Game.towers.length){
    var t=Game.towers[0]; t.rage=t.maxRage;
    if(window.Ult){Ult.cast(t,Game); out.ult='cast';}
    out.effects=Game.effects.map(function(e){return e.kind;});
  }
  return JSON.stringify(out);
})()`;
const ws = new WebSocket('ws://127.0.0.1:9222/devtools/page/CF994AC3BFEE3B87913FBD4387AF57E0');
ws.onopen = () => ws.send(JSON.stringify({id:1, method:'Runtime.evaluate', params:{expression:script, returnByValue:true, awaitPromise:true}}));
ws.onmessage = (ev) => { const m=JSON.parse(ev.data); if(m.id===1){ console.log(m.result&&m.result.result&&m.result.result.value); process.exit(0);} };
ws.onerror = e => { console.log('err',e.message); process.exit(1); };
setTimeout(()=>{console.log('TIMEOUT');process.exit(1);},10000);
