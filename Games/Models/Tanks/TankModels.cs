namespace Games.Models.Tanks;

public enum Team { Player = 0, Ally = 1, Enemy = 2 }
public enum EnemyBehavior { Aggressive, Shy, Circler, Sniper, Wanderer, Flanker }

public class Tank
{
    public int Id { get; set; }
    public Team Team { get; set; }
    public double X { get; set; } = 100;
    public double Y { get; set; } = 200;
    public double Angle { get; set; } = 0;
    public double BarrelAngle { get; set; } = 0;
    public double Speed { get; set; } = 110;
    public int Hp { get; set; } = 100;
    public bool IsPlayer { get; set; }
}

public class PlayerTank : Tank
{
    public PlayerTank() { Team = Team.Player; IsPlayer = true; }
}

public class AllyTank : Tank
{
    public double NextFireTimer { get; set; } = 1;
    public double StrafeDir { get; set; } = 1;
    public AllyTank() { Team = Team.Ally; Speed = 105; }
}

public class EnemyTank : Tank
{
    public double NextFireTimer { get; set; } = 1;
    public EnemyBehavior Behavior { get; set; } = EnemyBehavior.Aggressive;
    public double StrafeDir { get; set; } = 1;
    public double DecisionTimer { get; set; } = 1;
    public double RandomDirAngle { get; set; } = 0;
    public EnemyTank() { Team = Team.Enemy; Speed = 100; }
}

public class Projectile
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Angle { get; set; }
    public double Speed { get; set; }
    public Team OwnerTeam { get; set; }
}
