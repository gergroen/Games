namespace TamagotchiGame.Models;

/// <summary>
/// Represents core state for the virtual pet.
/// </summary>
public class PetState
{
    public PetState(string name)
    {
        Name = name;
    }

    public string Name { get; set; }
    public double Hunger { get; set; } = 20;
    public double Happiness { get; set; } = 80;
    public double Energy { get; set; } = 80;
    public string Animation { get; set; } = "idle";
}
