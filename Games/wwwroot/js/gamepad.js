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
  const tracks=[]; // {x,y,angle,start,duration}
  const lastTankPositions=new Map(); // track previous positions to detect movement
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
  function addTrackPoint(tankId, x, y, angle){
    const now = performance.now();
    tracks.push({x, y, angle, start: now, duration: 3500}); // 3.5 second fade
    
    // Limit track history to prevent memory issues
    if(tracks.length > 500) {
      tracks.splice(0, 50); // Remove oldest 50 tracks
    }
  }
  function updateTracks(allTanks){
    // Check each tank for movement and add track points
    allTanks.forEach(tank => {
      if(!tank || (tank.hp !== undefined && tank.hp <= 0) || (tank.Hp !== undefined && tank.Hp <= 0)) return;
      
      const tankId = tank.id || tank.Id || 0;
      const currentX = tank.x || tank.X;
      const currentY = tank.y || tank.Y;
      const currentAngle = tank.angle || tank.Angle;
      
      const lastPos = lastTankPositions.get(tankId);
      
      if(lastPos) {
        const dx = currentX - lastPos.x;
        const dy = currentY - lastPos.y;
        const distance = Math.sqrt(dx * dx + dy * dy);
        
        // Add track point if tank moved more than 5 pixels
        if(distance > 5) {
          // Calculate movement direction angle
          const movementAngle = Math.atan2(dy, dx);
          
          // Place track points behind the movement direction
          // Add multiple track points along the movement path for better trail effect
          const numTrackPoints = Math.max(1, Math.floor(distance / 8)); // One track every 8 pixels
          
          for(let i = 0; i < numTrackPoints; i++) {
            const t = i / numTrackPoints; // Progress along the movement path (0 to 1)
            const trackX = lastPos.x + dx * t;
            const trackY = lastPos.y + dy * t;
            
            // Offset the track point backwards from the movement direction
            const offsetDistance = 16; // Distance behind the tank center
            const offsetX = trackX - Math.cos(movementAngle) * offsetDistance;
            const offsetY = trackY - Math.sin(movementAngle) * offsetDistance;
            
            addTrackPoint(tankId, offsetX, offsetY, movementAngle);
          }
        }
      }
      
      // Update last position
      lastTankPositions.set(tankId, {x: currentX, y: currentY, angle: currentAngle});
    });
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
  // Prevent vertical scrollbar while game active
  document.body.classList.add('no-vert-scroll');
  // Initial sizing
  responsiveResize();
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
    ctx.strokeStyle='#333';
    for(let x=0;x<canvas.width;x+=40){ ctx.beginPath(); ctx.moveTo(x,0); ctx.lineTo(x,canvas.height); ctx.stroke(); }
    for(let y=0;y<canvas.height;y+=40){ ctx.beginPath(); ctx.moveTo(0,y); ctx.lineTo(canvas.width,y); ctx.stroke(); }
    
    // Collect all tanks for track updates
    const allTanks = [player];
    if(Array.isArray(enemy)){
      allTanks.push(...enemy.filter(e => (e.hp > 0 || e.Hp > 0)));
    } else if(enemy && (enemy.hp > 0 || enemy.Hp > 0)) {
      allTanks.push(enemy);
    }
    
    // Update tracks based on tank movement
    updateTracks(allTanks);
    
    // Draw tracks first (under everything else)
    drawTracks();
    
    drawTank(player,'#4cff4c','#90ff90');
    if(Array.isArray(enemy)){
      enemy.forEach(e=>{ if(e.hp>0 || e.Hp>0) drawTank(e,'#ff4c4c','#ff9090'); });
    } else {
      drawTank(enemy,'#ff4c4c','#ff9090');
    }
    ctx.fillStyle='#ffdc66';
    projectiles.forEach(p=>{ ctx.beginPath(); ctx.arc(p.x,p.y,4,0,Math.PI*2); ctx.fill(); });
    drawExplosions();
  }
  function drawTank(t,color,barrelColor){
    ctx.save();
    ctx.translate(t.x,t.y);
    
    // Draw body with its angle
    ctx.save();
    ctx.rotate(t.angle);
    ctx.fillStyle=color;
    ctx.fillRect(-16,-12,32,24); // body
    ctx.restore();
    
    // Draw barrel with its angle (use barrelAngle if available, otherwise fall back to tank angle)
    ctx.save();
    const barrelAngle = t.barrelAngle !== undefined ? t.barrelAngle : t.angle;
    ctx.rotate(barrelAngle);
    ctx.fillStyle=barrelColor||'#222';
    ctx.fillRect(0,-4,22,8); // barrel
    ctx.restore();
    
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
  function drawTracks(){
    const now=performance.now();
    for(let i=tracks.length-1;i>=0;i--){
      const track=tracks[i];
      const t=(now-track.start)/track.duration;
      if(t>=1){ tracks.splice(i,1); continue; }
      
      const alpha = (1-t) * 0.7; // Increased opacity for better visibility
      
      ctx.save();
      ctx.translate(track.x, track.y);
      ctx.rotate(track.angle);
      
      // Draw tank track marks - two parallel lines representing treads
      // Oriented perpendicular to movement direction for realistic tire tracks
      ctx.strokeStyle=`rgba(120,100,80,${alpha})`; // Darker brown/dirt color for better visibility
      ctx.lineWidth=4; // Slightly thicker lines
      ctx.lineCap='round';
      
      // Left tread mark (perpendicular to movement direction)
      ctx.beginPath();
      ctx.moveTo(-8, -12);
      ctx.lineTo(8, -12);
      ctx.stroke();
      
      // Right tread mark (perpendicular to movement direction)
      ctx.beginPath();
      ctx.moveTo(-8, 12);
      ctx.lineTo(8, 12);
      ctx.stroke();
      
      // Add small cross-hatches for texture (also perpendicular)
      ctx.lineWidth=2; // Thicker texture lines
      ctx.strokeStyle=`rgba(80,60,40,${alpha * 0.8})`; // Slightly different color for texture
      for(let offset = -6; offset <= 6; offset += 4) {
        ctx.beginPath();
        ctx.moveTo(offset, -12);
        ctx.lineTo(offset, 12);
        ctx.stroke();
      }
      
      ctx.restore();
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
  function vibrate(){
    // Try gamepad vibration first
    let gamepadVibrated = false;
    const pads = navigator.getGamepads ? navigator.getGamepads() : [];
    for(let i = 0; i < pads.length; i++){
      const gp = pads[i];
      if(!gp) continue;
      if(gp.vibrationActuator && gp.vibrationActuator.type === 'dual-rumble'){
        // Strong hit vibration: 400ms with strong magnitude
        gp.vibrationActuator.playEffect('dual-rumble', {
          duration: 400,
          strongMagnitude: 0.8,
          weakMagnitude: 0.6
        }).catch(()=>{}); // Ignore vibration errors
        gamepadVibrated = true;
      }
    }
    
    // Fallback to mobile device vibration if no gamepad vibration occurred
    if(!gamepadVibrated && navigator.vibrate){
      try {
        // Mobile vibration pattern: strong pulse matching gamepad duration
        navigator.vibrate(400);
      } catch(e) {
        // Ignore mobile vibration errors (some browsers/devices don't support it)
      }
    }
  }
  function toggleFullscreen(){
    const container=document.getElementById('tankGameContainer')||document.getElementById('tankCanvas');
    if(!container) return;
    const doResize=()=>{
      const canvas=document.getElementById('tankCanvas');
      if(canvas){
        const w = container === document.fullscreenElement ? screen.width : 640;
        const h = container === document.fullscreenElement ? screen.height : 400;
        canvas.width = w; canvas.height = h;
        if(_ref){ _ref.invokeMethodAsync('SetCanvasSize', w, h).catch(()=>{}); }
      }
    };
    if(!document.fullscreenElement){
      if(container.requestFullscreen){ container.requestFullscreen().then(()=>{ setTimeout(doResize,50); }); }
    } else {
      document.exitFullscreen?.();
      setTimeout(doResize,50);
    }
  }
  // shared resize logic (fullscreen or responsive fallback) for orientation / window size changes
  function responsiveResize(){
    const canvas=document.getElementById('tankCanvas');
    if(!canvas) return;
    const container=document.getElementById('tankGameContainer')||canvas;
    if(document.fullscreenElement===container){
      // Fullscreen: use screen size
      canvas.width=screen.width; canvas.height=screen.height;
      if(_ref){ _ref.invokeMethodAsync('SetCanvasSize', screen.width, screen.height).catch(()=>{}); }
      return;
    }
    // Fill visible area below top-row (exclude its height + article padding)
    const topRow = document.querySelector('.top-row');
    let topOffset = topRow ? topRow.getBoundingClientRect().bottom : 0;
    // Account for potential padding on article/content (px-4 ~ 1.5rem default). Use computed style.
    const article = canvas.closest('article');
    if(article){
      const cs = getComputedStyle(article);
      const pt = parseFloat(cs.paddingTop)||0; // top padding already included in getBoundingClientRect bottom? usually not
      topOffset += pt; // add padding space to subtract
    }
    const availH = window.innerHeight - topOffset - 2; // slight buffer to avoid scroll
    const w = Math.max(200, (container.clientWidth || window.innerWidth));
    const h = Math.max(200, Math.floor(availH));
  // Optionally set container height so overlays align
  container.style.height = h + 'px';
  canvas.width = w; canvas.height = h;
  if(_ref){ _ref.invokeMethodAsync('SetCanvasSize', w, h).catch(()=>{}); }
  }
  document.addEventListener('fullscreenchange', ()=>{
    const container=document.getElementById('tankGameContainer');
    const canvas=document.getElementById('tankCanvas');
    if(!canvas) return;
    if(document.fullscreenElement===container){
      canvas.width=screen.width; canvas.height=screen.height;
      if(_ref){ _ref.invokeMethodAsync('SetCanvasSize', screen.width, screen.height).catch(()=>{}); }
    } else {
      canvas.width=640; canvas.height=400;
      if(_ref){ _ref.invokeMethodAsync('SetCanvasSize', 640, 400).catch(()=>{}); }
    }
  });
  // Handle mobile orientation changes (and some browsers fire only resize). Use slight delays to allow layout stabilization.
  window.addEventListener('orientationchange', ()=>{
    setTimeout(responsiveResize, 60);
    setTimeout(responsiveResize, 300);
  });
  // Also respond to window resize (helpful when rotating or resizing viewport UI chrome)
  window.addEventListener('resize', ()=>{
    // debounce via requestAnimationFrame
    if(window.__tankResizeRaf){ cancelAnimationFrame(window.__tankResizeRaf); }
    window.__tankResizeRaf = requestAnimationFrame(()=> responsiveResize());
  });
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
  function cleanup(){
    document.body.classList.remove('no-vert-scroll');
  }
  return { init, draw, gameOver, addExplosion, playOutcome, playFire, vibrate, toggleFullscreen, cleanup };
})();
