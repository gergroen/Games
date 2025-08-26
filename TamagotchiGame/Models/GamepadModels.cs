namespace TamagotchiGame.Models;

public class GamepadSnapshot
{
    public int Index { get; set; }
    public string Id { get; set; } = string.Empty;
    public GamepadButton[] Buttons { get; set; } = System.Array.Empty<GamepadButton>();
    public double[] Axes { get; set; } = System.Array.Empty<double>();
}

public class GamepadButton
{
    public bool Pressed { get; set; }
    public double Value { get; set; }
}
