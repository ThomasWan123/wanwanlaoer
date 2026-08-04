// Freeze game, place a specific effect at peak render state, hold it
const port = process.argv[2] || '9264';
const pageId = process.argv[3];
const kind = process.argv[4] || 'maze';
const script = `(async function(){
  var out={};
  if (window.Game && Game.level){
    // Pause the game to freeze effect timers
    if (Game.state==='running'){ Game.state='paused'; } // stop main update advancing effects
    var path=Game.level.path||[];
    var center = path.length? path[Math.min(path.length-1,Math.floor(path.length*0.55))] : {x:480,y:300};
    var ef=null;
    switch('${kind}'){
      case 'maze': ef={kind:'maze',x:center.x,y:center.y,elapsed:0.6,duration:8}; break;
      case 'stun': ef={kind:'stun',x:center.x,y:center.y,elapsed:0.5,duration:8}; break;
      case 'hex': ef={kind:'hex',path:path,elapsed:1.0,duration:8}; break;
      case 'execute': ef={kind:'execute',x:center.x,y:center.y,elapsed:0.15,duration:8}; break;
    }
    if (ef){ Game.effects.push(ef); out.pushed=true; }
    // Render a few frames so the effect fully paints
    if (Game.render){ Game.render(); }
    await new Promise(r=>setTimeout(r,200));
    if (Game.render) Game.render();
    out.effects=Game.effects.map(function(e){return e.kind;});
    out.center=''+Math.round(center.x)+','+Math.round(center.y);
  } else out.msg='no battle';
  return JSON.stringify(out);
})()`;
const ws=new WebSocket('ws://127.0.0.1:'+port+'/devtools/page/'+pageId);
ws.onopen=()=>ws.send(JSON.stringify({id:1,method:'Runtime.evaluate',params:{expression:script,returnByValue:true,awaitPromise:true}}));
ws.onmessage=(ev)=>{const m=JSON.parse(ev.data);if(m.id===1){console.log('RESULT:',m.result&&m.result.result&&m.result.result.value);process.exit(0);}};
ws.onerror=e=>{console.log('err',e.message);process.exit(1);};
setTimeout(()=>{console.log('TIMEOUT');process.exit(1);},10000);
