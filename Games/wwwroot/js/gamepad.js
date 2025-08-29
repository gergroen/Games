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
  let radarCtx, radarCanvas; // radar context and canvas
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
    radarCanvas = document.getElementById('radarCanvas');
    if(!_keyHandlerAdded){
      document.addEventListener('keydown', (e)=>{
        if(e.key==='F11') { e.preventDefault(); toggleFullscreen(); }
      });
      _keyHandlerAdded=true;
    }
    if(!canvas) return;
    ctx = canvas.getContext('2d');
    if(radarCanvas) {
      radarCtx = radarCanvas.getContext('2d');
    }
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
  function draw(player, enemy, projectiles, cameraX, cameraY, powerUps){
    if(!ctx) return;
    cameraX = cameraX || 0;
    cameraY = cameraY || 0;
    powerUps = powerUps || [];
    
    ctx.clearRect(0,0,canvas.width,canvas.height);
    ctx.strokeStyle='#333';
    
    // Draw grid relative to camera position
    const gridSize = 40;
    const startX = Math.floor(cameraX / gridSize) * gridSize;
    const startY = Math.floor(cameraY / gridSize) * gridSize;
    
    for(let x = startX; x < cameraX + canvas.width + gridSize; x += gridSize) {
      const screenX = x - cameraX;
      if (screenX >= 0 && screenX <= canvas.width) {
        ctx.beginPath(); 
        ctx.moveTo(screenX, 0); 
        ctx.lineTo(screenX, canvas.height); 
        ctx.stroke();
      }
    }
    for(let y = startY; y < cameraY + canvas.height + gridSize; y += gridSize) {
      const screenY = y - cameraY;
      if (screenY >= 0 && screenY <= canvas.height) {
        ctx.beginPath(); 
        ctx.moveTo(0, screenY); 
        ctx.lineTo(canvas.width, screenY); 
        ctx.stroke();
      }
    }
    
    // Draw power-ups before tanks so they appear behind
    drawPowerUps(powerUps, cameraX, cameraY);
    
    drawTank(player,'#228B22','#90ff90', cameraX, cameraY);
    if(Array.isArray(enemy)){
      enemy.forEach(e=>{ if(e.hp>0 || e.Hp>0) drawTank(e,'#ff4c4c','#ff9090', cameraX, cameraY); });
    } else {
      drawTank(enemy,'#ff4c4c','#ff9090', cameraX, cameraY);
    }
    ctx.fillStyle='#ffdc66';
    projectiles.forEach(p=>{ 
      const screenX = p.x - cameraX;
      const screenY = p.y - cameraY;
      if (screenX >= -10 && screenX <= canvas.width + 10 && screenY >= -10 && screenY <= canvas.height + 10) {
        ctx.beginPath(); 
        ctx.arc(screenX, screenY, 4, 0, Math.PI*2); 
        ctx.fill(); 
      }
    });
    drawExplosions(cameraX, cameraY);
    
    // Draw radar
    drawRadar(player, enemy, cameraX, cameraY, powerUps);
  }
  
  function drawRadar(player, enemy, cameraX, cameraY, powerUps) {
    if (!radarCtx || !radarCanvas) return;
    
    // World dimensions (from BattlefieldService)
    const worldWidth = 5000;
    const worldHeight = 5000;
    
    // Radar dimensions
    const radarWidth = radarCanvas.width;
    const radarHeight = radarCanvas.height;
    
    // Scale factors to map world coordinates to radar coordinates
    const scaleX = radarWidth / worldWidth;
    const scaleY = radarHeight / worldHeight;
    
    // Clear radar
    radarCtx.clearRect(0, 0, radarWidth, radarHeight);
    
    // Draw world bounds (subtle grid)
    radarCtx.strokeStyle = '#333';
    radarCtx.lineWidth = 1;
    radarCtx.strokeRect(0, 0, radarWidth, radarHeight);
    
    // Draw viewport rectangle (current camera view)
    const viewportX = cameraX * scaleX;
    const viewportY = cameraY * scaleY;
    const viewportWidth = canvas.width * scaleX;
    const viewportHeight = canvas.height * scaleY;
    
    radarCtx.strokeStyle = '#666';
    radarCtx.lineWidth = 1;
    radarCtx.strokeRect(viewportX, viewportY, viewportWidth, viewportHeight);
    
    // Draw power-ups (colored squares)
    if (powerUps && Array.isArray(powerUps)) {
      powerUps.forEach(powerUp => {
        const powerUpX = powerUp.x * scaleX;
        const powerUpY = powerUp.y * scaleY;
        
        // Set color based on power-up type
        switch (powerUp.type) {
          case 0: // Health
            radarCtx.fillStyle = '#22FF22';
            break;
          case 1: // Shield
            radarCtx.fillStyle = '#2222FF';
            break;
          case 2: // FirePower
            radarCtx.fillStyle = '#FF2222';
            break;
          case 3: // Speed
            radarCtx.fillStyle = '#FFFF22';
            break;
          default:
            radarCtx.fillStyle = '#FFFFFF';
        }
        
        // Draw power-up as a small diamond
        const size = 2;
        radarCtx.save();
        radarCtx.translate(powerUpX, powerUpY);
        radarCtx.rotate(Math.PI / 4); // 45 degree rotation for diamond
        radarCtx.fillRect(-size, -size, size * 2, size * 2);
        radarCtx.restore();
      });
    }
    
    // Draw player position (green dot)
    if (player && (player.hp > 0 || player.Hp > 0 || player.hp === undefined)) {
      const playerX = player.x * scaleX;
      const playerY = player.y * scaleY;
      
      radarCtx.fillStyle = '#22AA22';
      radarCtx.beginPath();
      radarCtx.arc(playerX, playerY, 3, 0, Math.PI * 2);
      radarCtx.fill();
      
      // Draw player direction indicator
      radarCtx.strokeStyle = '#22AA22';
      radarCtx.lineWidth = 1;
      const angle = player.angle || 0;
      const dirLength = 6;
      radarCtx.beginPath();
      radarCtx.moveTo(playerX, playerY);
      radarCtx.lineTo(playerX + Math.cos(angle) * dirLength, playerY + Math.sin(angle) * dirLength);
      radarCtx.stroke();
    }
    
    // Draw enemy positions (red dots)
    if (Array.isArray(enemy)) {
      enemy.forEach(e => {
        if (e.hp > 0 || e.Hp > 0) {
          const enemyX = e.x * scaleX;
          const enemyY = e.y * scaleY;
          
          radarCtx.fillStyle = '#AA2222';
          radarCtx.beginPath();
          radarCtx.arc(enemyX, enemyY, 2.5, 0, Math.PI * 2);
          radarCtx.fill();
        }
      });
    } else if (enemy && (enemy.hp > 0 || enemy.Hp > 0)) {
      const enemyX = enemy.x * scaleX;
      const enemyY = enemy.y * scaleY;
      
      radarCtx.fillStyle = '#AA2222';
      radarCtx.beginPath();
      radarCtx.arc(enemyX, enemyY, 2.5, 0, Math.PI * 2);
      radarCtx.fill();
    }
  }
  function drawTank(t,color,barrelColor,cameraX,cameraY){
    cameraX = cameraX || 0;
    cameraY = cameraY || 0;
    
    const screenX = t.x - cameraX;
    const screenY = t.y - cameraY;
    
    // Only draw if tank is visible on screen (with some buffer)
    if (screenX < -50 || screenX > canvas.width + 50 || screenY < -50 || screenY > canvas.height + 50) {
      return;
    }
    
    ctx.save();
    ctx.translate(screenX, screenY);
    
    // Draw HP bar above tank
    if(t.hp !== undefined || t.Hp !== undefined) {
      const hp = t.hp || t.Hp || 100;
      const maxHp = 100;
      const barWidth = 40;
      const barHeight = 6;
      const barY = -25; // Position above tank
      
      // Background bar (red/dark)
      ctx.fillStyle = '#441111';
      ctx.fillRect(-barWidth/2, barY, barWidth, barHeight);
      
      // HP bar (green to red based on health)
      const hpRatio = hp / maxHp;
      let hpColor;
      if(hpRatio > 0.6) {
        hpColor = '#22AA22'; // Green
      } else if(hpRatio > 0.3) {
        hpColor = '#AAAA22'; // Yellow
      } else {
        hpColor = '#AA2222'; // Red
      }
      ctx.fillStyle = hpColor;
      ctx.fillRect(-barWidth/2, barY, barWidth * hpRatio, barHeight);
      
      // Border around HP bar
      ctx.strokeStyle = '#000';
      ctx.lineWidth = 1;
      ctx.strokeRect(-barWidth/2, barY, barWidth, barHeight);
    }
    
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
  function drawExplosions(cameraX, cameraY){
    cameraX = cameraX || 0;
    cameraY = cameraY || 0;
    
    const now=performance.now();
    for(let i=explosions.length-1;i>=0;i--){
      const e=explosions[i];
      const t=(now-e.start)/e.duration;
      if(t>=1){ explosions.splice(i,1); continue; }
      
      const screenX = e.x - cameraX;
      const screenY = e.y - cameraY;
      
      // Only draw if explosion is visible on screen
      if (screenX < -100 || screenX > canvas.width + 100 || screenY < -100 || screenY > canvas.height + 100) {
        continue;
      }
      
      const r=10 + t*55;
      const alpha = (1-t);
      // outer ring
      ctx.beginPath();
      ctx.strokeStyle=`rgba(255,180,40,${alpha})`;
      ctx.lineWidth=4*(1-t)+1;
      ctx.arc(screenX, screenY, r, 0, Math.PI*2);
      ctx.stroke();
      // filled core
      ctx.beginPath();
      ctx.fillStyle=`rgba(255,90,0,${alpha*0.6})`;
      ctx.arc(screenX, screenY, r*0.45, 0, Math.PI*2);
      ctx.fill();
    }
  }
  function drawPowerUps(powerUps, cameraX, cameraY) {
    if (!powerUps || !Array.isArray(powerUps)) return;
    
    cameraX = cameraX || 0;
    cameraY = cameraY || 0;
    
    powerUps.forEach(powerUp => {
      const screenX = powerUp.x - cameraX;
      const screenY = powerUp.y - cameraY;
      
      // Only draw if power-up is visible on screen
      if (screenX < -30 || screenX > canvas.width + 30 || screenY < -30 || screenY > canvas.height + 30) {
        return;
      }
      
      // Pulsing effect based on time
      const time = performance.now() / 1000;
      const pulse = 0.8 + 0.2 * Math.sin(time * 3);
      const size = 12 * pulse;
      
      ctx.save();
      ctx.translate(screenX, screenY);
      
      // Draw power-up based on type
      switch (powerUp.type) {
        case 0: // Health
          ctx.fillStyle = '#22FF22';
          ctx.strokeStyle = '#00AA00';
          break;
        case 1: // Shield
          ctx.fillStyle = '#2222FF';
          ctx.strokeStyle = '#0000AA';
          break;
        case 2: // FirePower
          ctx.fillStyle = '#FF2222';
          ctx.strokeStyle = '#AA0000';
          break;
        case 3: // Speed
          ctx.fillStyle = '#FFFF22';
          ctx.strokeStyle = '#AAAA00';
          break;
        default:
          ctx.fillStyle = '#FFFFFF';
          ctx.strokeStyle = '#AAAAAA';
      }
      
      // Draw diamond shape
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.moveTo(0, -size);
      ctx.lineTo(size, 0);
      ctx.lineTo(0, size);
      ctx.lineTo(-size, 0);
      ctx.closePath();
      ctx.fill();
      ctx.stroke();
      
      // Draw inner symbol
      ctx.fillStyle = '#FFFFFF';
      ctx.font = '12px Arial';
      ctx.textAlign = 'center';
      ctx.textBaseline = 'middle';
      
      let symbol = '?';
      switch (powerUp.type) {
        case 0: symbol = '+'; break; // Health
        case 1: symbol = '⚡'; break; // Shield
        case 2: symbol = '⚔'; break; // FirePower
        case 3: symbol = '⟩'; break; // Speed
      }
      
      ctx.fillText(symbol, 0, 0);
      
      ctx.restore();
    });
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
