// Query all general ultimate types
const port = process.argv[2] || '9240';
const pageId = process.argv[3] || '711842C886FE61714798C428C033649E';
const script = `(function(){
  var out={};
  out.generals = [];
  // getGeneralRegistry or iterate Game
  if (typeof getGeneral === 'function' && typeof Game !== 'undefined'){
    // try window.GENERALS or the generals data
    var keys = Object.keys(window).filter(function(k){ return typeof window[k]==='object' && window[k] && window[k].ultimate && window[k].id; });
    // Use known approach: Game has generator, but simplest is to check a list
  }
  // Try common globals
  var gList = window.GENERALS || window.Generals || window.GENERAL_LIST;
  out.hasGList = !!gList;
  // If there's a data file, attempts via registry
  if (window.getGeneral && window.GeneralRegistry){
    out.reg = 'has-registry';
  }
  return JSON.stringify(out);
})()`;
const ws=new WebSocket('ws://127.0.0.1:'+port+'/devtools/page/'+pageId);
ws.onopen=()=>ws.send(JSON.stringify({id:1,method:'Runtime.evaluate',params:{expression:script,returnByValue:true}}));
ws.onmessage=(ev)=>{const m=JSON.parse(ev.data);if(m.id===1){console.log('RESULT:',m.result&&m.result.result&&m.result.result.value);process.exit(0);}};
ws.onerror=e=>{console.log('err',e.message);process.exit(1);};
setTimeout(()=>{console.log('TIMEOUT');process.exit(1);},8000);
