// Trigger flood ult and hold briefly so we can screenshot during active effect
const port = process.argv[2] || '9240';
const pageId = process.argv[3] || '711842C886FE61714798C428C033649E';
const script = `(async function(){
  var out={};
  if (window.Game && Game.level && Game.towers.length){
    var t=Game.towers[0]; t.rage=t.maxRage;
    if(window.Ult){Ult.cast(t,Game); out.ult=true;}
    out.effects=Game.effects.map(function(e){return e.kind;});
    out.elapsed0=Game.effects.length?Game.effects[0].elapsed:0;
  }
  // wait 0.4s so watermark rises, then report (screen stays showing effect)
  await new Promise(r=>setTimeout(r,450));
  out.effectsNow=Game.effects.map(function(e){return e.kind;});
  return JSON.stringify(out);
})()`;
const ws=new WebSocket('ws://127.0.0.1:'+port+'/devtools/page/'+pageId);
ws.onopen=()=>ws.send(JSON.stringify({id:1,method:'Runtime.evaluate',params:{expression:script,returnByValue:true,awaitPromise:true}}));
ws.onmessage=(ev)=>{const m=JSON.parse(ev.data);if(m.id===1){console.log('RESULT:',m.result&&m.result.result&&m.result.result.value);process.exit(0);}};
ws.onerror=e=>{console.log('err',e.message);process.exit(1);};
setTimeout(()=>{console.log('TIMEOUT');process.exit(1);},12000);
