// Trigger a specific ult effect by kind in the active battle, hold at a good moment
const port = process.argv[2] || '9240';
const pageId = process.argv[3] || '711842C886FE61714798C428C033649E';
const kind = process.argv[4] || 'blaze';
const script = `(async function(){
  var out={before:0};
  if (window.Game && Game.level && Game.canvas){
    var path = Game.level.path || [];
    var elapsed = 1.2; // set elapsed so effect is in "peak" rendering phase
    // create effect of requested kind
    var ef=null;
    switch('${kind}'){
      case 'blaze': ef={kind:'blaze',path:path,elapsed:elapsed,duration:5}; break;
      case 'maze': ef={kind:'maze',x:path.length?path[Math.floor(path.length*0.5)].x:480, y:path.length?path[Math.floor(path.length*0.5)].y:300, elapsed:elapsed,duration:5}; break;
      case 'stun': ef={kind:'stun',x:480,y:300,elapsed:elapsed,duration:2.2}; break;
      case 'execute': ef={kind:'execute',x:640,y:400,elapsed:elapsed,duration:0.6}; break;
      case 'charge': ef={kind:'charge',x:500,y:300,elapsed:elapsed,duration:4,trail:[{x:300,y:300},{x:340,y:310},{x:380,y:320},{x:420,y:330},{x:460,y:340},{x:500,y:350},{x:540,y:360},{x:580,y:340},{x:620,y:320}]}; break;
      case 'hex': ef={kind:'hex',path:path,elapsed:elapsed,duration:5}; break;
      case 'tide': ef={kind:'tide',path:path,elapsed:elapsed,duration:4.5}; break;
      case 'rally': ef={kind:'rally',path:path,bursts:Game.towers.map(function(t){return {x:t.x,y:t.y};}),elapsed:elapsed,duration:2.8}; break;
      case 'ultBurst': ef={kind:'ultBurst',x:480,y:300,ultType:'execute',color:'#f7d774',elapsed:elapsed,duration:1.5}; break;
      default: ef={kind:'${kind}',x:480,y:300,elapsed:elapsed,duration:2}; break;
    }
    if (ef){ Game.effects.push(ef); out.pushed=true; }
    // force one render
    if (typeof Game.render==='function') Game.render();
    out.effectsKinds=Game.effects.map(function(e){return e.kind;});
  } else { out.msg='no battle'; }
  return JSON.stringify(out);
})()`;
const ws=new WebSocket('ws://127.0.0.1:'+port+'/devtools/page/'+pageId);
ws.onopen=()=>ws.send(JSON.stringify({id:1,method:'Runtime.evaluate',params:{expression:script,returnByValue:true,awaitPromise:true}}));
ws.onmessage=(ev)=>{const m=JSON.parse(ev.data);if(m.id===1){console.log('RESULT:',m.result&&m.result.result&&m.result.result.value);process.exit(0);}};
ws.onerror=e=>{console.log('err',e.message);process.exit(1);};
setTimeout(()=>{console.log('TIMEOUT');process.exit(1);},10000);
