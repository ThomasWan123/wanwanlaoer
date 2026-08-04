// Start a fresh battle, place a tower, spawn enemies, trigger ult
const setup = `(function(){
  var out = {};
  // If defeated, go back to menu then back to battle
  if (Game.state === 'defeat') {
    // Try to find restart button
    var btns = document.querySelectorAll('button');
    out.buttons = [];
    for (var i=0;i<btns.length;i++){ out.buttons.push({id:btns[i].id, text:btns[i].textContent.trim()}); }
    return JSON.stringify(out);
  }
  return JSON.stringify({state:'not-defeat', stateGame:Game.state});
})()`;
const ws = new WebSocket('ws://127.0.0.1:9222/devtools/page/CF994AC3BFEE3B87913FBD4387AF57E0');
ws.onopen = () => ws.send(JSON.stringify({id:1, method:'Runtime.evaluate', params:{expression:setup, returnByValue:true}}));
ws.onmessage = (ev) => { const m=JSON.parse(ev.data); if(m.id===1){ console.log(m.result&&m.result.result&&m.result.result.value); process.exit(0);} };
setTimeout(()=>{console.log('TIMEOUT');process.exit(1);},8000);
