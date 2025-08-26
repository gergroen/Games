// Simple dual virtual joystick implementation for Tanks page
// Emits movement and aim vectors to .NET via invokeMethodAsync calls on frame updates while touch active.
(function(){
  const clamp=(v,min,max)=>v<min?min:(v>max?max:v);
  function setup(id, callback){
    const el=document.getElementById(id); if(!el) return;
    const stick=el.querySelector('.stick');
    let active=false; let touchId=null; let vx=0, vy=0;
    const maxRadius = el.clientWidth/2 - (stick.clientWidth/2) - 4; // padding
    function setFromEvent(e){
      let clientX, clientY;
      if(e.changedTouches){
        for(const t of e.changedTouches){ if(t.identifier===touchId){ clientX=t.clientX; clientY=t.clientY; break; } }
      } else { clientX=e.clientX; clientY=e.clientY; }
      if(clientX==null) return;
      const rect=el.getBoundingClientRect();
      const cx=rect.left + rect.width/2; const cy=rect.top + rect.height/2;
      const dx=clientX - cx; const dy=clientY - cy;
      const dist=Math.sqrt(dx*dx+dy*dy);
      const angle=Math.atan2(dy,dx);
      const useDist = Math.min(dist, maxRadius);
      const sx = Math.cos(angle)*useDist; const sy=Math.sin(angle)*useDist;
      stick.style.transform=`translate(${sx}px, ${sy}px)`;
      vx = (sx / maxRadius); vy = (sy / maxRadius);
      if(dist<6){ vx=0; vy=0; }
      if(callback) callback(vx, vy, active);
    }
    function start(e){
      if(active) return; active=true; el.classList.add('active');
      if(e.changedTouches){ touchId=e.changedTouches[0].identifier; }
      setFromEvent(e); e.preventDefault();
    }
    function move(e){ if(!active) return; setFromEvent(e); }
    function end(e){ if(!active) return;
      if(e.changedTouches){
        let ended=false; for(const t of e.changedTouches){ if(t.identifier===touchId){ ended=true; break; } }
        if(!ended) return;
      }
      active=false; touchId=null; el.classList.remove('active'); vx=0; vy=0; stick.style.transform='translate(0,0)'; if(callback) callback(0,0,false);
    }
    el.addEventListener('mousedown', start); el.addEventListener('mousemove', move); window.addEventListener('mouseup', end);
    el.addEventListener('touchstart', start, {passive:false}); el.addEventListener('touchmove', move, {passive:false}); window.addEventListener('touchend', end); window.addEventListener('touchcancel', end);
  }
  window.virtualJoysticks={ init:function(dotNetRef){
    setup('moveJoystick', (x,y,act)=>{ if(dotNetRef) dotNetRef.invokeMethodAsync('OnVirtualMove', x, y, act).catch(()=>{}); });
    setup('aimJoystick', (x,y,act)=>{ if(dotNetRef) dotNetRef.invokeMethodAsync('OnVirtualAim', x, y, act).catch(()=>{}); });
  }};
})();
