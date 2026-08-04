// Use Node 22 built-in WebSocket (no deps)
const js = `(function(){
  var out = {};
  var screens=['menu','level-select','lineup-screen','game-screen'];
  for(var i=0;i<screens.length;i++){var e=document.getElementById(screens[i]); if(e&&e.classList.contains('active')) out.activeScreen=screens[i];}
  if(window.Game){out.state=Game.state; out.towers=Game.towers?Game.towers.length:-1; out.enemies=Game.enemies?Game.enemies.length:-1; out.hasLevel=!!Game.level; out.projectionEnabled=window.Projection?Projection.enabled:false; out.panX=Math.round(Game.view.panX); out.panY=Math.round(Game.view.panY); out.zoom=Game.view.zoom;}
  out.layoutMode=window.Viewport?Viewport.layoutMode:null; out.ultraWide=window.Viewport?Viewport.ultraWide:null;
  out.hasParallax = !!window.Art._drawParallaxLayers;
  out.heightFor = window.Projection?Projection.heightFor('tower',{level:3}):null;
  return JSON.stringify(out);
})()`;

const ws = new WebSocket('ws://127.0.0.1:9222/devtools/page/CF994AC3BFEE3B87913FBD4387AF57E0');
ws.onopen = () => {
  ws.send(JSON.stringify({id:1, method:'Runtime.evaluate', params:{expression:js, returnByValue:true}}));
};
ws.onmessage = (ev) => {
  const msg = JSON.parse(ev.data);
  if (msg.id === 1) {
    console.log('GAME STATE RESULT:');
    console.log(msg.result && msg.result.result && msg.result.result.value);
    process.exit(0);
  }
};
ws.onerror = (e) => { console.log('WS error', e.message); process.exit(1); };
setTimeout(() => { console.log('TIMEOUT'); process.exit(1); }, 8000);
