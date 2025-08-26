using Games.Models;

namespace Games.Services;

/// <summary>
/// Encapsulates pet logic (actions, decay, derived properties).
/// UI & JS interop stay in the component.
/// </summary>
public class PetGameService
{
    public PetState State { get; } = new("PixelPet");

    private int _animId;

    public string CurrentMood => State.Happiness > 70 && State.Hunger < 60 && State.Energy > 40 ? "Happy" :
        State.Hunger > 80 ? "Hungry" :
        State.Energy < 25 ? "Tired" :
        State.Happiness < 40 ? "Sad" : "Neutral";

    public string Face => State.Animation switch
    {
        "eating" => "(ˆڡˆ)",
        "playing" => "(≧◡≦)",
        "resting" => "(-‿-) zZ",
        _ => CurrentMood switch
        {
            "Happy" => "^_^",
            "Hungry" => "(º﹃º)",
            "Tired" => "(-_-)",
            "Sad" => "T_T",
            _ => "o_o"
        }
    };

    public string SpriteClasses => $"mood-{CurrentMood.ToLower()} anim-{State.Animation}";

    public void Feed()
    {
        double amount = 12 + (State.Hunger > 60 ? 8 : 0);
        State.Hunger = Math.Max(0, State.Hunger - amount);
        State.Happiness = Math.Min(100, State.Happiness + 3);
        TriggerAnimation("eating", 1200);
    }

    public void Play()
    {
        if (State.Energy < 8) return;
        State.Happiness = Math.Min(100, State.Happiness + 14);
        State.Energy = Math.Max(0, State.Energy - 8);
        State.Hunger = Math.Min(100, State.Hunger + 4);
        TriggerAnimation("playing", 1000);
    }

    public void Rest()
    {
        State.Energy = Math.Min(100, State.Energy + 18);
        State.Hunger = Math.Min(100, State.Hunger + 2);
        State.Happiness = Math.Min(100, State.Happiness + 1);
        TriggerAnimation("resting", 1500);
    }

    public void ApplyDecay()
    {
        double hungerInc = 0.9 + (State.Energy / 200.0);
        double energyDec = 0.5 + (State.Happiness / 250.0);
        double happinessDec = 0.3 + (State.Hunger > 70 ? 0.5 : 0) + (State.Energy < 30 ? 0.4 : 0);
        State.Hunger = Math.Min(100, State.Hunger + hungerInc);
        State.Energy = Math.Max(0, State.Energy - energyDec);
        State.Happiness = Math.Max(0, State.Happiness - happinessDec);
    }

    private void TriggerAnimation(string name, int durationMs)
    {
        int id = ++_animId;
        State.Animation = name;
        _ = Task.Run(async () =>
        {
            await Task.Delay(durationMs);
            if (_animId == id)
            {
                State.Animation = "idle";
            }
        });
    }
}
