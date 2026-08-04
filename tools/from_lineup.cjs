// From lineup screen -> start battle -> place tower -> flood ult
const port = process.argv[2] || '9240';
const pageId = process.argv[3] || '711842C886FE61714798C428C033649E';
const script = `(async function(){
  var out={};
  var g=function(id){var e=document.getElementById(id);return e&&!e.classList.contains('hidden')?e:null;};
  // From lineup: ensure general selected then start
  var c=document.querySelector('#lineup-grid > *'); if(c){c.dispatchEvent(new MouseEvent('click',{bubbles:true})); out.selGeneral=true;}
  await new Promise(r=>setTimeout(r,400));
  var bs=g('btn-lineup-start'); if(bs){bs.click(); out.tappedStart=true;}
  await new Promise(r=>setTimeout(r,2000));
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
    if (Game.towers.length){ var t=Game.towers[0]; t.rage=t.maxRage; if(window.Ult){Ult.cast(t,Game); out.ult=true;} out.effects=Game.effects.map(function(e){return e.kind;}); }
  }
  return JSON.stringify(out);
})()`;
const ws=new WebSocket('ws://127.0.0.1:'+port+'/devtools/page/'+pageId);
ws.onopen=()=>ws.send(JSON.stringify({id:1,method:'Runtime.evaluate',params:{expression:script,returnByValue:true,awaitPromise:true}}));
ws.onmessage=(ev)=>{const m=JSON.parse(ev.data);if(m.id===1){console.log('RESULT:',m.result&&m.result.result&&m.result.result.value);process.exit(0);}};
ws.onerror=e=>{console.log('err',e.message);process.exit(1);};
setTimeout(()=>{console.log('TIMEOUT');process.exit(1);},15000);
