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

    // Power-up effects
    public double ShieldTime { get; set; } = 0; // Remaining shield time
    public double FirePowerTime { get; set; } = 0; // Remaining fire power boost time
    public double SpeedBoostTime { get; set; } = 0; // Remaining speed boost time
    public double BasePowerCooldown { get; set; } = 0.35; // Base firing cooldown

    /// <summary>
    /// Gets the effective speed based on current HP and speed boosts. Tanks move slower as they take damage.
    /// </summary>
    public double EffectiveSpeed => Speed * (Hp / 100.0) * (SpeedBoostTime > 0 ? 1.5 : 1.0);

    /// <summary>
    /// Gets the effective firing cooldown with fire power boost.
    /// </summary>
    public double EffectiveFirCooldown => FirePowerTime > 0 ? BasePowerCooldown * 0.5 : BasePowerCooldown;
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

public enum PowerUpType { Health, Shield, FirePower, Speed }

public class PowerUp
{
    public int Id { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public PowerUpType Type { get; set; }
    public double SpawnTime { get; set; }
    public double Duration { get; set; } = 60.0; // How long it stays on battlefield
}
