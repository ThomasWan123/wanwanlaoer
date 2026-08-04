// Capture: given a kind, connect, trigger, and let caller screenshot
// Usage: node capture_ult.cjs 9240 PAGEID KIND
const port = process.argv[2] || '9240';
const pageId = process.argv[3];
const kind = process.argv[4];
const fs = require('fs');
const http = require('http');

// Reuse trigger logic inline (no separate process to avoid WS framing issues)
const script = `(async function(){
  var out={};
  if (window.Game && Game.level){
    var path = Game.level.path || [];
    var ef=null;
    var center = path.length ? path[Math.min(path.length-1, Math.floor(path.length*0.55))] : {x:480,y:300};
    switch('${kind}'){
      case 'blaze': ef={kind:'blaze',path:path,elapsed:1.3,duration:6}; break;
      case 'afterblaze': ef={kind:'blaze',path:path,elapsed:1.3,duration:6}; break;
      case 'maze': ef={kind:'maze',x:center.x,y:center.y,elapsed:0.8,duration:6}; break;
      case 'stun': ef={kind:'stun',x:center.x,y:center.y,elapsed:0.3,duration:2.2}; break;
      case 'execute': ef={kind:'execute',x:center.x,y:Math.min(center.y,560),elapsed:0.15,duration:0.6}; break;
      case 'charge': ef={kind:'charge',x:center.x,y:center.y,elapsed:1.0,duration:4,trail:[{x:center.x-260,y:center.y-20},{x:center.x-200,y:center.y},{x:center.x-140,y:center.y+10},{x:center.x-80,y:center.y+20},{x:center.x-20,y:center.y+5},{x:center.x+40,y:center.y-5}]}; break;
      case 'hex': ef={kind:'hex',path:path,elapsed:1.0,duration:6}; break;
      case 'tide': ef={kind:'tide',path:path,elapsed:1.0,duration:5}; break;
      case 'rally': ef={kind:'rally',path:path,bursts:Game.towers.length?Game.towers.map(function(t){return {x:t.x,y:t.y};}):[{x:300,y:300},{x:480,y:300},{x:660,y:300},{x:480,y:400}],elapsed:1.0,duration:2.8}; break;
      case 'ultBurst': ef={kind:'ultBurst',x:center.x,y:center.y,ultType:'execute',color:'#f7d774',elapsed:0.5,duration:1.5}; break;
    }
    if (ef){ window.__EFF=ef; Game.effects.push(ef); out.pushed=true; }
    if (Game.render) Game.render();
    out.kind='${kind}'; out.center=''+Math.round(center.x)+','+Math.round(center.y);
  } else out.msg='no battle';
  return JSON.stringify(out);
})()`;

const ws=new WebSocket('ws://127.0.0.1:'+port+'/devtools/page/'+pageId);
ws.onopen=()=>ws.send(JSON.stringify({id:1,method:'Runtime.evaluate',params:{expression:script,returnByValue:true,awaitPromise:true}}));
ws.onmessage=(ev)=>{const m=JSON.parse(ev.data);if(m.id===1){console.log('RESULT:',m.result&&m.result.result&&m.result.result.value);process.exit(0);}};
ws.onerror=e=>{console.log('err',e.message);process.exit(1);};
setTimeout(()=>{console.log('TIMEOUT');process.exit(1);},10000);
