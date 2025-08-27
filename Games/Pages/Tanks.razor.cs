using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Games.Models;
using Games.Models.Tanks;
using Games.Services;

namespace Games.Pages;

public partial class Tanks : ComponentBase, IAsyncDisposable
{
    [Inject] public IJSRuntime JS { get; set; } = default!;
    [Inject] public BattlefieldService Battlefield { get; set; } = default!;

    private DotNetObjectReference<Tanks>? _selfRef;
    private GamepadSnapshot[] _pads = Array.Empty<GamepadSnapshot>();
    private bool[] _prevButtons = Array.Empty<bool>();
    private double _lastTs;
    private bool _running;
    private double _manualMoveX; private double _manualMoveY;
    private double _manualAimX; private double _manualAimY; private bool _manualAiming; private DateTime _lastManualFire = DateTime.MinValue; private bool _fireHeld;
    private bool _autoFire; // Auto firing enabled flag
    private double _autoFireTimer; // timer accumulator between shots when auto fire

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _selfRef = DotNetObjectReference.Create(this);
            Battlefield.Reset();
            await JS.InvokeVoidAsync("tankGame.init", _selfRef);
            // Initialize virtual joysticks for touch/mobile
            await JS.InvokeVoidAsync("virtualJoysticks.init", _selfRef);
            _running = true;
        }
    }

    private bool DeadZonePressed(double v) => Math.Abs(v) < 0.25;
    private double Dead(double v) => DeadZonePressed(v) ? 0 : v;
    private bool Pressed(GamepadSnapshot pad, int index) => index < pad.Buttons.Length && pad.Buttons[index].Pressed;

    [JSInvokable]
    public async Task Frame(double ts)
    {
        double dt = (_lastTs == 0 ? 0 : (ts - _lastTs)) / 1000.0; _lastTs = ts;
        _pads = await JS.InvokeAsync<GamepadSnapshot[]>("gamepadManager.pollGamepads");
        if (_pads.Length > 0) { var cur = _pads[0]; if (_prevButtons.Length != cur.Buttons.Length) _prevButtons = new bool[cur.Buttons.Length]; }
        if (!_running)
        {
            if ((Battlefield.Player.Hp <= 0 || Battlefield.EnemiesRemaining == 0) && _pads.Length > 0)
            {
                var gp = _pads[0]; bool startNow = gp.Buttons.Length > 9 && gp.Buttons[9].Pressed; bool startPrev = _prevButtons.Length > 9 && _prevButtons[9]; if (startNow && !startPrev) Restart();
                for (int i = 0; i < gp.Buttons.Length && i < _prevButtons.Length; i++) _prevButtons[i] = gp.Buttons[i].Pressed;
            }
            return;
        }
        HandleInput(dt);
        Battlefield.Update(dt, pr => _ = JS.InvokeVoidAsync("tankGame.addExplosion", pr.X, pr.Y), () => _ = JS.InvokeVoidAsync("tankGame.vibrate"));
        var livingOthers = Battlefield.Allies.Where(a => a.Hp > 0).Cast<Tank>().Concat(Battlefield.Enemies.Where(e => e.Hp > 0)).ToList();
        await JS.InvokeVoidAsync("tankGame.draw", Battlefield.Player, livingOthers, Battlefield.Projectiles);
        if (Battlefield.Player.Hp <= 0 || Battlefield.EnemiesRemaining == 0)
        {
            _running = false; bool playerWon = Battlefield.Player.Hp > 0 && Battlefield.EnemiesRemaining == 0;
            if (!playerWon) _ = JS.InvokeVoidAsync("tankGame.addExplosion", Battlefield.Player.X, Battlefield.Player.Y);
            await JS.InvokeVoidAsync("tankGame.gameOver", playerWon ? "Victory!" : "Defeat!");
            _ = JS.InvokeVoidAsync("tankGame.playOutcome", playerWon);
        }
        if (_pads.Length > 0) { var gp = _pads[0]; for (int i = 0; i < gp.Buttons.Length && i < _prevButtons.Length; i++) _prevButtons[i] = gp.Buttons[i].Pressed; }
        StateHasChanged();
    }

    private void HandleInput(double dt)
    {
        if (_pads.Length == 0)
        {
            // Manual on-screen controls path
            if (Math.Abs(_manualMoveX) > 0 || Math.Abs(_manualMoveY) > 0)
                Battlefield.MovePlayer(_manualMoveX, _manualMoveY, dt);
            if (_manualAiming && (Math.Abs(_manualAimX) > 0 || Math.Abs(_manualAimY) > 0))
                Battlefield.AimPlayer(_manualAimX, _manualAimY);
            HandleFiring(dt);
            return;
        }
        var p = _pads[0];
        bool startNow = p.Buttons.Length > 9 && p.Buttons[9].Pressed; bool selectNow = p.Buttons.Length > 8 && p.Buttons[8].Pressed;
        bool startPrev = _prevButtons.Length > 9 && _prevButtons[9]; bool selectPrev = _prevButtons.Length > 8 && _prevButtons[8];
        if (startNow && selectNow && (!startPrev || !selectPrev)) _ = JS.InvokeVoidAsync("tankGame.toggleFullscreen");
        double moveX = 0, moveY = 0; if (p.Axes.Length >= 2) { moveX = Dead(p.Axes[0]); moveY = Dead(p.Axes[1]); }
        if (moveX == 0 && Pressed(p, 14)) moveX = -1; if (moveX == 0 && Pressed(p, 15)) moveX = 1; if (moveY == 0 && Pressed(p, 12)) moveY = -1; if (moveY == 0 && Pressed(p, 13)) moveY = 1;
        Battlefield.MovePlayer(moveX, moveY, dt);
        double aimX = 0, aimY = 0; if (p.Axes.Length >= 4) { aimX = Dead(p.Axes[2]); aimY = Dead(p.Axes[3]); }
        Battlefield.AimPlayer(aimX, aimY);
        bool ltNow = p.Buttons.Length > 6 && p.Buttons[6].Pressed; bool rtNow = p.Buttons.Length > 7 && p.Buttons[7].Pressed;
        bool ltPrev = _prevButtons.Length > 6 && _prevButtons[6]; bool rtPrev = _prevButtons.Length > 7 && _prevButtons[7];
        if ((ltNow && !ltPrev) || (rtNow && !rtPrev)) { Battlefield.TryFirePlayer(() => _ = JS.InvokeVoidAsync("tankGame.playFire")); }
        if (_autoFire) HandleFiring(dt);
    }

    private void HandleFiring(double dt)
    {
        // Manual hold fire always tries
        if (_fireHeld)
        {
            Battlefield.TryFirePlayer(() => _ = JS.InvokeVoidAsync("tankGame.playFire"));
        }
        if (_autoFire && !_fireHeld)
        {
            _autoFireTimer -= dt;
            if (_autoFireTimer <= 0)
            {
                Battlefield.TryFirePlayer(() => _ = JS.InvokeVoidAsync("tankGame.playFire"));
                _autoFireTimer = 0.4; // slightly above core cooldown ensuring consistent cadence
            }
        }
    }

    private void StartMove(double x, double y)
    {
        _manualMoveX = x; _manualMoveY = y; StateHasChanged();
    }
    private void StopMove(double x, double y)
    {
        if (_manualMoveX == x && _manualMoveY == y) { _manualMoveX = 0; _manualMoveY = 0; StateHasChanged(); }
    }

    private void ManualFire()
    {
        Battlefield.TryFirePlayer(() => _ = JS.InvokeVoidAsync("tankGame.playFire"));
    }

    private void StartFire()
    {
        _fireHeld = true; Battlefield.TryFirePlayer(() => _ = JS.InvokeVoidAsync("tankGame.playFire"));
    }
    private void StopFire()
    {
        _fireHeld = false;
    }

    private void ToggleAutoFire()
    {
        _autoFire = !_autoFire; if (_autoFire) { _autoFireTimer = 0; }
    }

    private void StartAim(double x, double y)
    {
        _manualAimX = x; _manualAimY = y; _manualAiming = true; Battlefield.AimPlayer(x, y); StateHasChanged();
    }
    private void StopAim()
    {
        _manualAiming = false; _manualAimX = 0; _manualAimY = 0; StateHasChanged();
    }

    private void Restart()
    {
        Battlefield.Reset(); _lastTs = 0; _running = true; _ = JS.InvokeVoidAsync("tankGame.init", _selfRef); _ = JS.InvokeVoidAsync("virtualJoysticks.init", _selfRef);
    }

    private Task ToggleFullscreen() => JS.InvokeVoidAsync("tankGame.toggleFullscreen").AsTask();
    [JSInvokable] public Task SetCanvasSize(int w, int h) { Battlefield.SetCanvasSize(w, h); return Task.CompletedTask; }

    public async ValueTask DisposeAsync()
    {
        _running = false; _selfRef?.Dispose(); await Task.CompletedTask;
        try { await JS.InvokeVoidAsync("tankGame.cleanup"); } catch { }
    }

    // Virtual joystick callbacks
    [JSInvokable]
    public Task OnVirtualMove(double x, double y, bool active)
    {
        if (active)
        {
            _manualMoveX = x; _manualMoveY = y;
        }
        else
        {
            _manualMoveX = 0; _manualMoveY = 0;
        }
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnVirtualAim(double x, double y, bool active)
    {
        if (active)
        {
            _manualAimX = x; _manualAimY = y; _manualAiming = true; Battlefield.AimPlayer(x, y);
        }
        else
        {
            _manualAiming = false; _manualAimX = 0; _manualAimY = 0;
        }
        return Task.CompletedTask;
    }
}
