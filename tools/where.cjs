const pageId = process.argv[2] || '711842C886FE61714798C428C033649E';
const port = process.argv[3] || '9230';
const script = `(function(){
  var out={};
  var screens=['menu','level-select','lineup-screen','game-screen'];
  for(var i=0;i<screens.length;i++){var e=document.getElementById(screens[i]); if(e&&e.classList.contains('active')) out.active=screens[i];}
  out.state = window.Game?Game.state:null;
  out.levelListCount = document.querySelectorAll('#level-list .level-item, #level-list > [data-level], #level-list .card, #level-list > div').length;
  out.lineupCards = document.querySelectorAll('#lineup-grid .lineup-card, #lineup-grid > *').length;
  return JSON.stringify(out);
})()`;
const ws=new WebSocket('ws://127.0.0.1:'+port+'/devtools/page/'+pageId);
ws.onopen=()=>ws.send(JSON.stringify({id:1,method:'Runtime.evaluate',params:{expression:script,returnByValue:true}}));
ws.onmessage=(ev)=>{const m=JSON.parse(ev.data);if(m.id===1){console.log(m.result&&m.result.result&&m.result.result.value);process.exit(0);}};
ws.onerror=e=>{console.log('err',e.message);process.exit(1);};
setTimeout(()=>{console.log('TIMEOUT');process.exit(1);},8000);
