using TamagotchiGame.Models.Tanks;

namespace TamagotchiGame.Services;

/// <summary>
/// Encapsulates tank battle game state & core simulation logic (AI, collisions, projectiles).
/// UI/Gamepad/JS rendering remain in the component.
/// </summary>
public class BattlefieldService
{
    private readonly Random _rand = new();
    public int GameWidth { get; private set; } = 640;
    public int GameHeight { get; private set; } = 400;

    public PlayerTank Player { get; private set; } = new();
    public List<AllyTank> Allies { get; } = new();
    public List<EnemyTank> Enemies { get; } = new();
    public List<Projectile> Projectiles { get; } = new();

    private bool _canFire = true; private double _fireCooldown = 0;

    public int EnemiesRemaining => Enemies.Count(e => e.Hp > 0);
    public int AlliesAlive => Allies.Count(a => a.Hp > 0);

    public void Reset()
    {
        Projectiles.Clear();
        _canFire = true; _fireCooldown = 0;
        SpawnTeams();
    }

    public void SetCanvasSize(int w, int h)
    {
        if (w > 100 && h > 100)
        {
            GameWidth = w; GameHeight = h;
        }
    }

    public void SpawnTeams()
    {
        // Player vs single enemy mode (no allies)
        Allies.Clear();
        Enemies.Clear();
        Player = new PlayerTank { Id = 0, Team = Team.Player, X = 120, Y = GameHeight / 2.0 };

        Enemies.Add(new EnemyTank
        {
            Id = 100,
            Team = Team.Enemy,
            X = GameWidth - 140,
            Y = GameHeight / 2.0,
            Behavior = EnemyBehavior.Aggressive,
            StrafeDir = _rand.Next(0, 2) == 0 ? -1 : 1,
            NextFireTimer = 0.8 + _rand.NextDouble() * 0.6,
            DecisionTimer = 0.6 + _rand.NextDouble() * 0.8
        });
    }

    public void Update(double dt, Action<Projectile>? onExplosion)
    {
        HandleAllies(dt);
        HandleEnemies(dt);
        UpdateProjectiles(dt);
        CheckCollisions(onExplosion);
        ApplySeparation();
        _fireCooldown -= dt; if (_fireCooldown <= 0) _canFire = true;
    }

    public void MovePlayer(double moveX, double moveY, double dt)
    {
        Player.X += moveX * Player.Speed * dt; Player.Y += moveY * Player.Speed * dt; Clamp(Player);
    }

    public void AimPlayer(double aimX, double aimY)
    {
        if (Math.Abs(aimX) > 0 || Math.Abs(aimY) > 0) Player.Angle = Math.Atan2(aimY, aimX);
    }

    public void TryFirePlayer(Action? onFire)
    {
        if (_canFire) { Fire(Player); _canFire = false; _fireCooldown = 0.35; onFire?.Invoke(); }
    }

    private void HandleAllies(double dt)
    {
        foreach (var ally in Allies) { if (ally.Hp <= 0) continue; AllyAI(ally, dt); }
    }
    private void HandleEnemies(double dt)
    {
        foreach (var e in Enemies) { if (e.Hp <= 0) continue; EnemyAI(e, dt); }
    }

    private void AllyAI(AllyTank ally, double dt)
    {
        EnemyTank? target = null; double best = double.MaxValue;
        foreach (var e in Enemies)
        {
            if (e.Hp <= 0) continue; double dx = e.X - ally.X; double dy = e.Y - ally.Y; double d2 = dx * dx + dy * dy; if (d2 < best) { best = d2; target = e; }
        }
        if (target == null) return;
        double dxT = target.X - ally.X; double dyT = target.Y - ally.Y; double dist = Math.Sqrt(dxT * dxT + dyT * dyT) + 0.001; double ang = Math.Atan2(dyT, dxT); ally.Angle = ang;
        double moveVX = 0, moveVY = 0;
        if (dist > 150) { moveVX = Math.Cos(ang); moveVY = Math.Sin(ang); }
        else if (dist < 110) { moveVX = -Math.Cos(ang); moveVY = -Math.Sin(ang); }
        else { moveVX = Math.Cos(ang + ally.StrafeDir * Math.PI / 2) * 0.8; moveVY = Math.Sin(ang + ally.StrafeDir * Math.PI / 2) * 0.8; }
        Normalise(ref moveVX, ref moveVY);
        ally.X += moveVX * ally.Speed * dt; ally.Y += moveVY * ally.Speed * dt; Clamp(ally);
        ally.NextFireTimer -= dt; if (ally.NextFireTimer <= 0) { Fire(ally); ally.NextFireTimer = 0.7 + _rand.NextDouble() * 0.6; }
    }

    private void EnemyAI(EnemyTank enemy, double dt)
    {
        Tank? target = Player.Hp > 0 ? Player : null; double best = target != null ? ((Player.X - enemy.X) * (Player.X - enemy.X) + (Player.Y - enemy.Y) * (Player.Y - enemy.Y)) : double.MaxValue;
        if (target == null)
        {
            foreach (var a in Allies)
            {
                if (a.Hp <= 0) continue; double ax = a.X - enemy.X; double ay = a.Y - enemy.Y; double d2 = ax * ax + ay * ay; if (d2 < best) { best = d2; target = a; }
            }
        }
        if (target == null) return;
        enemy.DecisionTimer -= dt; if (enemy.DecisionTimer <= 0 && (enemy.Behavior == EnemyBehavior.Wanderer || enemy.Behavior == EnemyBehavior.Flanker)) { enemy.RandomDirAngle = _rand.NextDouble() * Math.PI * 2; enemy.DecisionTimer = 0.8 + _rand.NextDouble() * 1.2; }
        double dx = target.X - enemy.X; double dy = target.Y - enemy.Y; double dist = Math.Sqrt(dx * dx + dy * dy) + 0.001; double targetAngle = Math.Atan2(dy, dx); enemy.Angle = targetAngle;
        double moveVX = 0, moveVY = 0; double desired = 140;
        switch (enemy.Behavior)
        {
            case EnemyBehavior.Aggressive:
                if (dist > 120) { moveVX = Math.Cos(targetAngle); moveVY = Math.Sin(targetAngle); }
                else { moveVX = Math.Cos(targetAngle + enemy.StrafeDir * Math.PI / 2) * 0.8; moveVY = Math.Sin(targetAngle + enemy.StrafeDir * Math.PI / 2) * 0.8; }
                break;
            case EnemyBehavior.Shy:
                if (dist < 200) { moveVX = -Math.Cos(targetAngle); moveVY = -Math.Sin(targetAngle); }
                else if (dist < 260) { moveVX = Math.Cos(targetAngle + enemy.StrafeDir * Math.PI / 2) * 0.6; moveVY = Math.Sin(targetAngle + enemy.StrafeDir * Math.PI / 2) * 0.6; }
                break;
            case EnemyBehavior.Circler:
                if (dist > desired + 30) { moveVX += Math.Cos(targetAngle) * 0.9; moveVY += Math.Sin(targetAngle) * 0.9; }
                else if (dist < desired - 30) { moveVX -= Math.Cos(targetAngle) * 0.9; moveVY -= Math.Sin(targetAngle) * 0.9; }
                moveVX += Math.Cos(targetAngle + enemy.StrafeDir * Math.PI / 2); moveVY += Math.Sin(targetAngle + enemy.StrafeDir * Math.PI / 2);
                break;
            case EnemyBehavior.Sniper:
                if (dist < 260) { moveVX = -Math.Cos(targetAngle); moveVY = -Math.Sin(targetAngle); }
                break;
            case EnemyBehavior.Wanderer:
                moveVX = Math.Cos(enemy.RandomDirAngle) * 0.7; moveVY = Math.Sin(enemy.RandomDirAngle) * 0.7; if (dist < 110) { moveVX -= Math.Cos(targetAngle) * 0.5; moveVY -= Math.Sin(targetAngle) * 0.5; }
                break;
            case EnemyBehavior.Flanker:
                double flank = targetAngle + enemy.StrafeDir * Math.PI / 2; moveVX = Math.Cos(flank); moveVY = Math.Sin(flank); if (dist > 180) { moveVX += Math.Cos(targetAngle) * 0.6; moveVY += Math.Sin(targetAngle) * 0.6; }
                break;
        }
        Normalise(ref moveVX, ref moveVY);
        enemy.X += moveVX * enemy.Speed * dt; enemy.Y += moveVY * enemy.Speed * dt; Clamp(enemy);
        enemy.NextFireTimer -= dt; if (enemy.NextFireTimer <= 0) { Fire(enemy); enemy.NextFireTimer = enemy.Behavior == EnemyBehavior.Sniper ? 1.3 + _rand.NextDouble() * 1.0 : 0.8 + _rand.NextDouble() * 0.7; }
    }

    public void Fire(Tank t)
    {
        Projectiles.Add(new Projectile { X = t.X + Math.Cos(t.Angle) * 20, Y = t.Y + Math.Sin(t.Angle) * 20, Angle = t.Angle, Speed = 300, OwnerTeam = t.Team });
    }

    private void UpdateProjectiles(double dt)
    {
        for (int i = Projectiles.Count - 1; i >= 0; i--)
        {
            var pr = Projectiles[i]; pr.X += Math.Cos(pr.Angle) * pr.Speed * dt; pr.Y += Math.Sin(pr.Angle) * pr.Speed * dt;
            if (pr.X < 0 || pr.Y < 0 || pr.X > GameWidth || pr.Y > GameHeight) Projectiles.RemoveAt(i);
        }
    }

    private void CheckCollisions(Action<Projectile>? onExplosion)
    {
        for (int i = Projectiles.Count - 1; i >= 0; i--)
        {
            var pr = Projectiles[i];
            if (Player.Hp > 0 && pr.OwnerTeam != Player.Team && Hit(pr, Player)) { Player.Hp -= 10; onExplosion?.Invoke(pr); Projectiles.RemoveAt(i); continue; }
            bool removed = false;
            for (int a = 0; a < Allies.Count && !removed; a++)
            {
                var ally = Allies[a]; if (ally.Hp <= 0 || ally.Team == pr.OwnerTeam) continue; if (Hit(pr, ally)) { ally.Hp -= 10; onExplosion?.Invoke(pr); Projectiles.RemoveAt(i); removed = true; }
            }
            if (removed) continue;
            for (int e = 0; e < Enemies.Count && !removed; e++)
            {
                var en = Enemies[e]; if (en.Hp <= 0 || en.Team == pr.OwnerTeam) continue; if (Hit(pr, en)) { en.Hp -= 10; onExplosion?.Invoke(pr); Projectiles.RemoveAt(i); removed = true; }
            }
        }
    }

    private void ApplySeparation()
    {
        double minDist = 36; var all = new List<Tank>(); all.AddRange(Allies); all.AddRange(Enemies);
        for (int i = 0; i < all.Count; i++)
        {
            var a = all[i]; if (a.Hp <= 0) continue;
            for (int j = i + 1; j < all.Count; j++)
            {
                var b = all[j]; if (b.Hp <= 0) continue; double dx = b.X - a.X; double dy = b.Y - a.Y; double d2 = dx * dx + dy * dy; if (d2 < 0.01) { dx = 0.5; dy = 0; d2 = 0.25; }
                double d = Math.Sqrt(d2); if (d < minDist)
                {
                    double push = (minDist - d) / 2; double nx = dx / d; double ny = dy / d; a.X -= nx * push; a.Y -= ny * push; b.X += nx * push; b.Y += ny * push; Clamp(a); Clamp(b);
                }
            }
        }
    }

    private bool Hit(Projectile pr, Tank t) => (pr.X - t.X) * (pr.X - t.X) + (pr.Y - t.Y) * (pr.Y - t.Y) < 22 * 22;

    private void Clamp(Tank t)
    {
        t.X = Math.Clamp(t.X, 20, GameWidth - 20); t.Y = Math.Clamp(t.Y, 20, GameHeight - 20);
    }

    private static void Normalise(ref double x, ref double y)
    {
        double mag = Math.Sqrt(x * x + y * y); if (mag > 0.001) { x /= mag; y /= mag; }
    }
}
