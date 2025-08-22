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
  let ctx, canvas; let over=false; let _ref=null;
  function init(dotNetRef){
    _ref = dotNetRef;
    canvas = document.getElementById('tankCanvas');
    if(!canvas) return;
    ctx = canvas.getContext('2d');
    function frame(ts){
      if(over) return;
      if(_ref){ _ref.invokeMethodAsync('Frame', ts).catch(()=>{}); }
      // schedule next frame
      requestAnimationFrame(frame);
    }
    requestAnimationFrame(frame);
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
  function gameOver(msg){
    over=true;
    if(!ctx) return;
    ctx.fillStyle='rgba(0,0,0,0.6)';
    ctx.fillRect(0,0,canvas.width,canvas.height);
    ctx.fillStyle='#fff';
    ctx.font='32px sans-serif';
    ctx.textAlign='center';
    ctx.fillText(msg, canvas.width/2, canvas.height/2);
  }
  return { init, draw, gameOver };
})();
