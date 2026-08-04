// Restart battle to running state via replay button, then place tower
const port = process.argv[2] || '9250';
const pageId = process.argv[3] || '711842C886FE61714798C428C033649E';
const script = `(async function(){
  var out={};
  var g=function(id){var e=document.getElementById(id);return e&&!e.classList.contains('hidden')?e:null;};
  // If in battle but not running, click replay/resume
  if (g('btn-replay')){ g('btn-replay').click(); out.clicked='replay'; }
  else if (g('btn-resume')){ g('btn-resume').click(); out.clicked='resume'; }
  else if (Game.state==='idle' && g('btn-start')){ g('btn-start').click(); out.clicked='start'; }
  await new Promise(r=>setTimeout(r,1200));
  // if still not running, maybe need to re-navigate from menu
  out.active=(function(){var s=['menu','level-select','lineup-screen','game-screen'];for(var i=0;i<s.length;i++){var e=document.getElementById(s[i]);if(e&&e.classList.contains('active'))return s[i];}return '?';})();
  out.state=Game.state;
  // Ensure a tower exists for rally/ultBurst source coords
  if (Game.level && Game.towers.length===0 && Game.level.slots.length){
    var g0=typeof getGeneral==='function'?getGeneral('guanyu'):null;
    if (g0){ var s=Game.level.slots[0]; if (Game.placeTower) Game.placeTower(s,g0); if(Game.towers.length===0){var tw=new (window.Tower)(g0,s.x,s.y,0);Game.towers.push(tw);s.occupied=tw;out.forced=true;} }
  }
  out.towers=Game.towers.length;
  // Make it running by releasing pause if needed
  if (Game.state==='paused'){ var r2=g('btn-resume'); if(r2){r2.click(); out.resumed=true;} await new Promise(r=>setTimeout(r,500)); }
  out.stateFinal=Game.state;
  return JSON.stringify(out);
})()`;
const ws=new WebSocket('ws://127.0.0.1:'+port+'/devtools/page/'+pageId);
ws.onopen=()=>ws.send(JSON.stringify({id:1,method:'Runtime.evaluate',params:{expression:script,returnByValue:true,awaitPromise:true}}));
ws.onmessage=(ev)=>{const m=JSON.parse(ev.data);if(m.id===1){console.log('RESULT:',m.result&&m.result.result&&m.result.result.value);process.exit(0);}};
ws.onerror=e=>{console.log('err',e.message);process.exit(1);};
setTimeout(()=>{console.log('TIMEOUT');process.exit(1);},12000);
