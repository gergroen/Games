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
