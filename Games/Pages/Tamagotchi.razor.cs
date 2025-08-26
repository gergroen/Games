using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Games.Interop;
using Games.Models;
using Games.Services;

namespace Games.Pages;

public partial class Tamagotchi : ComponentBase, IAsyncDisposable
{
    [Inject] public IJSRuntime JS { get; set; } = default!;
    [Inject] public PetGameService PetService { get; set; } = default!;

    private System.Timers.Timer? _tickTimer;
    private DotNetObjectReference<FrameCallback>? _frameCallbackRef;
    private FrameCallback? _frameCallback;
    private bool[] _prevButtons = Array.Empty<bool>();

    protected bool GamepadConnected { get; private set; }
    protected string? GamepadId { get; private set; }

    protected override void OnInitialized()
    {
        _tickTimer = new System.Timers.Timer(1000);
        _tickTimer.Elapsed += (s, e) =>
        {
            PetService.ApplyDecay();
            InvokeAsync(StateHasChanged);
        };
        _tickTimer.Start();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _frameCallback = new FrameCallback(CheckGamepadAsync);
            _frameCallbackRef = DotNetObjectReference.Create(_frameCallback);
            await JS.InvokeVoidAsync("gamepadManager.startLoop", _frameCallbackRef);
        }
    }

    private void OnFeed() { PetService.Feed(); StateHasChanged(); }
    private void OnPlay() { PetService.Play(); StateHasChanged(); }
    private void OnRest() { PetService.Rest(); StateHasChanged(); }

    private async Task CheckGamepadAsync(double _)
    {
        try
        {
            var pads = await JS.InvokeAsync<GamepadSnapshot[]>("gamepadManager.pollGamepads");
            if (pads.Length > 0)
            {
                GamepadConnected = true;
                GamepadId = pads[0].Id;
                var p = pads[0];
                EnsurePrevButtonsSize(p.Buttons.Length);
                for (int i = 0; i < p.Buttons.Length; i++)
                {
                    bool pressed = p.Buttons[i].Pressed;
                    if (pressed && !_prevButtons[i])
                    {
                        if (i == 0) OnFeed();
                        else if (i == 1) OnPlay();
                        else if (i == 2) OnRest();
                    }
                    _prevButtons[i] = pressed;
                }
            }
            else
            {
                GamepadConnected = false;
            }
        }
        catch
        {
            // ignore polling errors
        }
        StateHasChanged();
    }

    private void EnsurePrevButtonsSize(int len)
    {
        if (_prevButtons.Length != len)
            _prevButtons = new bool[len];
    }

    public ValueTask DisposeAsync()
    {
        _tickTimer?.Stop();
        _tickTimer?.Dispose();
        _frameCallbackRef?.Dispose();
        return ValueTask.CompletedTask;
    }
}
