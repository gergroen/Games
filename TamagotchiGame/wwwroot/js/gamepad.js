window.gamepadManager = {
  _lastState: {},
  pollGamepads: function () {
    const pads = navigator.getGamepads ? navigator.getGamepads() : [];
    const result = [];
    for (let i = 0; i < pads.length; i++) {
      const gp = pads[i];
      if (!gp) continue;
      result.push({
        index: gp.index,
        id: gp.id,
        buttons: gp.buttons.map(b => ({ pressed: b.pressed, value: b.value })),
        axes: gp.axes.slice(0)
      });
    }
    return result;
  },
  startLoop: function(dotNetRef){
    if(!dotNetRef) return;
    function frame(ts){
      dotNetRef.invokeMethodAsync('Invoke', ts).catch(()=>{});
      window.requestAnimationFrame(frame);
    }
    window.requestAnimationFrame(frame);
  }
};

window.addEventListener('gamepadconnected', e => console.log('Gamepad connected', e.gamepad));
window.addEventListener('gamepaddisconnected', e => console.log('Gamepad disconnected', e.gamepad));

window.tankGame = (function(){
  let ctx, canvas; let over=false; let _ref=null; let _frameFn=null;
  const explosions=[]; // {x,y,start,duration}
  let audioCtx=null; let _keyHandlerAdded=false; // added flag
  function ensureAudio(){
    if(!audioCtx){
      try{ audioCtx=new (window.AudioContext||window.webkitAudioContext)(); }catch(e){ audioCtx=null; }
    }
  }
  function playBoom(){
    if(!audioCtx) return;
    const ctxA=audioCtx;
    const now=ctxA.currentTime;
    const osc=ctxA.createOscillator();
    const gain=ctxA.createGain();
    osc.type='sawtooth';
    osc.frequency.setValueAtTime(180, now);
    osc.frequency.exponentialRampToValueAtTime(40, now+0.5);
    gain.gain.setValueAtTime(0.6, now);
    gain.gain.exponentialRampToValueAtTime(0.0001, now+0.5);
    osc.connect(gain).connect(ctxA.destination);
    osc.start(now); osc.stop(now+0.5);
    // noise burst
    const buffer=ctxA.createBuffer(1, ctxA.sampleRate*0.25, ctxA.sampleRate);
    const data=buffer.getChannelData(0);
    for(let i=0;i<data.length;i++){ data[i]=(Math.random()*2-1)*Math.pow(1-i/data.length,2); }
    const noise=ctxA.createBufferSource();
    noise.buffer=buffer;
    const nGain=ctxA.createGain();
    nGain.gain.setValueAtTime(0.5, now);
    nGain.gain.exponentialRampToValueAtTime(0.0001, now+0.25);
    noise.connect(nGain).connect(ctxA.destination);
    noise.start(now); noise.stop(now+0.25);
  }
  function addExplosion(x,y){
    explosions.push({x,y,start:performance.now(),duration:650});
    ensureAudio(); playBoom();
  }
  function init(dotNetRef){
    over=false;
    _ref = dotNetRef;
    canvas = document.getElementById('tankCanvas');
    if(!_keyHandlerAdded){
      document.addEventListener('keydown', (e)=>{
        if(e.key==='F11') { e.preventDefault(); toggleFullscreen(); }
      });
      _keyHandlerAdded=true;
    }
    if(!canvas) return;
    ctx = canvas.getContext('2d');
    if(!_frameFn){
      _frameFn = function frame(ts){
        if(_ref){ _ref.invokeMethodAsync('Frame', ts).catch(()=>{}); }
        requestAnimationFrame(_frameFn);
      };
      requestAnimationFrame(_frameFn);
    }
  }
  function draw(player, enemy, projectiles){
    if(!ctx) return;
    ctx.clearRect(0,0,canvas.width,canvas.height);
    // draw arena grid
    ctx.strokeStyle='#333';
    for(let x=0;x<canvas.width;x+=40){ ctx.beginPath(); ctx.moveTo(x,0); ctx.lineTo(x,canvas.height); ctx.stroke(); }
    for(let y=0;y<canvas.height;y+=40){ ctx.beginPath(); ctx.moveTo(0,y); ctx.lineTo(canvas.width,y); ctx.stroke(); }

    drawTank(player,'#4cff4c');
    drawTank(enemy,'#ff4c4c');

    ctx.fillStyle='#ffdc66';
    projectiles.forEach(p=>{ ctx.beginPath(); ctx.arc(p.x,p.y,4,0,Math.PI*2); ctx.fill(); });
    drawExplosions();
  }
  function drawTank(t,color){
    ctx.save();
    ctx.translate(t.x,t.y);
    ctx.rotate(t.angle);
    ctx.fillStyle=color;
    ctx.fillRect(-16,-12,32,24); // body
    ctx.fillStyle='#222';
    ctx.fillRect(0,-4,22,8); // barrel
    ctx.restore();
  }
  function drawExplosions(){
    const now=performance.now();
    for(let i=explosions.length-1;i>=0;i--){
      const e=explosions[i];
      const t=(now-e.start)/e.duration;
      if(t>=1){ explosions.splice(i,1); continue; }
      const r=10 + t*55;
      const alpha = (1-t);
      // outer ring
      ctx.beginPath();
      ctx.strokeStyle=`rgba(255,180,40,${alpha})`;
      ctx.lineWidth=4*(1-t)+1;
      ctx.arc(e.x,e.y,r,0,Math.PI*2);
      ctx.stroke();
      // filled core
      ctx.beginPath();
      ctx.fillStyle=`rgba(255,90,0,${alpha*0.6})`;
      ctx.arc(e.x,e.y,r*0.45,0,Math.PI*2);
      ctx.fill();
    }
  }
  let musicNodes=[];
  function stopMusic(){ musicNodes.forEach(n=>{try{n.stop();}catch{} }); musicNodes=[]; }
  function playOutcome(win){
    ensureAudio(); if(!audioCtx) return; stopMusic();
    const ctxA=audioCtx; const now=ctxA.currentTime; const tempo= win? 0.28:0.34; // seconds per note approx
    // Simple scale / melody sequences
    const victory=[262,330,392,523,392,523,660];
    const defeat=[392,370,349,330,262,196];
    const seq = win? victory: defeat;
    seq.forEach((f,i)=>{
      const osc=ctxA.createOscillator(); const gain=ctxA.createGain();
      osc.type= win? 'triangle':'square';
      osc.frequency.setValueAtTime(f, now + i*tempo);
      gain.gain.setValueAtTime(0.001, now + i*tempo);
      gain.gain.linearRampToValueAtTime(0.4, now + i*tempo + 0.02);
      gain.gain.exponentialRampToValueAtTime(0.0001, now + i*tempo + tempo*0.9);
      osc.connect(gain).connect(ctxA.destination);
      osc.start(now + i*tempo); osc.stop(now + i*tempo + tempo);
      musicNodes.push(osc);
    });
  }
  function playFire(){
    ensureAudio(); if(!audioCtx) return;
    const now=audioCtx.currentTime;
    const osc=audioCtx.createOscillator();
    const gain=audioCtx.createGain();
    osc.type='square';
    osc.frequency.setValueAtTime(520, now);
    osc.frequency.exponentialRampToValueAtTime(180, now+0.18);
    gain.gain.setValueAtTime(0.35, now);
    gain.gain.exponentialRampToValueAtTime(0.0001, now+0.2);
    osc.connect(gain).connect(audioCtx.destination);
    osc.start(now); osc.stop(now+0.22);
  }
  function toggleFullscreen(){
    const container=document.getElementById('tankGameContainer')||document.getElementById('tankCanvas');
    if(!container) return;
    if(!document.fullscreenElement){
      if(container.requestFullscreen) container.requestFullscreen();
    } else {
      document.exitFullscreen?.();
    }
  }
  function gameOver(msg){
    over=true;
    if(!ctx) return;
    ctx.fillStyle='rgba(0,0,0,0.6)';
    ctx.fillRect(0,0,canvas.width,canvas.height);
    ctx.fillStyle='#fff';
    ctx.font='28px sans-serif';
    ctx.textAlign='center';
    ctx.fillText(msg, canvas.width/2, canvas.height/2 - 10);
    ctx.font='18px sans-serif';
    ctx.fillText('Click or Press Start to Play Again', canvas.width/2, canvas.height/2 + 24);
  }
  // expose new funcs
  return { init, draw, gameOver, addExplosion, playOutcome, playFire, toggleFullscreen };
})();
