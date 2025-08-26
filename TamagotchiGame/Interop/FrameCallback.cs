using Microsoft.JSInterop;

namespace TamagotchiGame.Interop;

/// <summary>
/// Helper that lets JavaScript provide a requestAnimationFrame timestamp back into .NET.
/// </summary>
public sealed class FrameCallback
{
    private readonly Func<double, Task> _callback;
    public FrameCallback(Func<double, Task> callback) => _callback = callback;

    [JSInvokable]
    public Task Invoke(double timestamp) => _callback(timestamp);
}
