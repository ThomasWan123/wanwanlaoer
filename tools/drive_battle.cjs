// Self-contained: get page id via http, then drive battle. Single node process.
const port = process.argv[2] || '9230';
const http = require('http');
function getJSON() {
  return new Promise((resolve, reject) => {
    http.get('http://127.0.0.1:' + port + '/json', res => {
      let d = ''; res.on('data', c => d += c); res.on('end', () => { try{ resolve(JSON.parse(d)); }catch(e){ reject(e);} });
    }).on('error', reject);
  });
}
(async () => {
  let targets;
  for (let i=0;i<5;i++){
    try { targets = await getJSON(); if(targets && targets.length) break; } catch(e){}
    await new Promise(r=>setTimeout(r,1000));
  }
  if(!targets || !targets.length){ console.log('NO TARGETS'); process.exit(2); }
  const pageId = targets[0].id;
  console.log('Page:', pageId);

  const script = `(async function(){
    var out={};
    var g=function(id){var e=document.getElementById(id);return e&&!e.classList.contains('hidden')?e:null;};
    function click(id){var e=g(id); if(e){e.click(); return true;} return false;}
    if (g('btn-start')){ click('btn-start'); await new Promise(r=>setTimeout(r,900)); }
    if (document.getElementById('level-select').classList.contains('active')){
      // trigger the UI's level click by calling UI hook or clicking first item
      if (window.UI && typeof UI.selectLevel==='function'){ UI.selectLevel(0); out.usedUI=true; }
      else { var c=document.querySelector('#level-list [data-level], #level-list > div'); if(c){c.dispatchEvent(new MouseEvent('click',{bubbles:true})); out.usedDom=true;} }
      await new Promise(r=>setTimeout(r,1100));
    }
    if (document.getElementById('lineup-screen').classList.contains('active')){
      var c=document.querySelector('#lineup-grid > *'); if(c) c.dispatchEvent(new MouseEvent('click',{bubbles:true}));
      await new Promise(r=>setTimeout(r,300));
      click('btn-lineup-start');
      await new Promise(r=>setTimeout(r,1600));
    }
    out.active=(function(){var s=['menu','level-select','lineup-screen','game-screen'];for(var i=0;i<s.length;i++){var e=document.getElementById(s[i]);if(e&&e.classList.contains('active'))return s[i];}return '?';})();
    out.state=Game.state;
    if (window.Game && Game.level){
      var g0=typeof getGeneral==='function'?getGeneral('guanyu'):null;
      if (g0 && Game.towers.length===0 && Game.level.slots.length){
        var s=Game.level.slots[0];
        if (typeof Game.placeTower==='function') Game.placeTower(s,g0);
        if (Game.towers.length===0){ var tw=new (window.Tower)(g0,s.x,s.y,0); Game.towers.push(tw); s.occupied=tw; out.forced=true; }
      }
      out.towers=Game.towers.length;
      if (Game.towers.length){ var t=Game.towers[0]; t.rage=t.maxRage; if(window.Ult){Ult.cast(t,Game);} out.effects=Game.effects.map(function(e){return e.kind;}); }
    }
    return JSON.stringify(out);
  })()`;

  const ws = new WebSocket('ws://127.0.0.1:' + port + '/devtools/page/' + pageId);
  ws.onopen = () => ws.send(JSON.stringify({id:1, method:'Runtime.evaluate', params:{expression:script, returnByValue:true, awaitPromise:true}}));
  ws.onmessage = ev => { const m=JSON.parse(ev.data); if(m.id===1){ console.log('RESULT:', m.result&&m.result.result&&m.result.result.value); process.exit(0);} };
  ws.onerror = e => { console.log('ws err', e.message); process.exit(1); };
  setTimeout(()=>{console.log('TIMEOUT');process.exit(1);},20000);
})();
