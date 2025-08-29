using Games.Models.Tanks;

namespace Games.Services;

/// <summary>
/// Encapsulates tank battle game state & core simulation logic (AI, collisions, projectiles).
/// UI/Gamepad/JS rendering remain in the component.
/// </summary>
public class BattlefieldService
{
    private readonly Random _rand = new();
    public int GameWidth { get; private set; } = 640;
    public int GameHeight { get; private set; } = 400;

    // World size (larger than viewport)
    public int WorldWidth { get; private set; } = 5000;
    public int WorldHeight { get; private set; } = 5000;

    // Camera position (top-left corner of viewport in world coordinates)
    public double CameraX { get; private set; } = 0;
    public double CameraY { get; private set; } = 0;

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
        // Player vs 5 enemies mode (no allies)
        Allies.Clear();
        Enemies.Clear();
        Player = new PlayerTank { Id = 0, Team = Team.Player, X = 200, Y = WorldHeight / 2.0, BarrelAngle = 0 };

        // Available enemy behaviors for variety
        var behaviors = new[] { EnemyBehavior.Aggressive, EnemyBehavior.Shy, EnemyBehavior.Circler, EnemyBehavior.Sniper, EnemyBehavior.Wanderer, EnemyBehavior.Flanker };

        // Spawn 5 enemies at random positions
        for (int i = 0; i < 5; i++)
        {
            double x, y;
            int attempts = 0;

            // Find a position that's not too close to player or other enemies
            do
            {
                x = _rand.NextDouble() * (WorldWidth - 400) + 200; // 200 margin from edges
                y = _rand.NextDouble() * (WorldHeight - 400) + 200;
                attempts++;
            }
            while (attempts < 20 && (
                // Too close to player
                Math.Sqrt((x - Player.X) * (x - Player.X) + (y - Player.Y) * (y - Player.Y)) < 300 ||
                // Too close to existing enemies
                Enemies.Any(e => Math.Sqrt((x - e.X) * (x - e.X) + (y - e.Y) * (y - e.Y)) < 150)
            ));

            var enemy = new EnemyTank
            {
                Id = 100 + i,
                Team = Team.Enemy,
                X = x,
                Y = y,
                Angle = _rand.NextDouble() * Math.PI * 2, // Random initial direction
                BarrelAngle = _rand.NextDouble() * Math.PI * 2, // Random initial barrel direction
                Behavior = behaviors[i % behaviors.Length], // Cycle through behaviors for variety
                StrafeDir = _rand.Next(0, 2) == 0 ? -1 : 1,
                NextFireTimer = 0.8 + _rand.NextDouble() * 0.6,
                DecisionTimer = 0.6 + _rand.NextDouble() * 0.8
            };

            Enemies.Add(enemy);
        }

        // Center camera on player initially
        CameraX = Math.Clamp(Player.X - GameWidth / 2.0, 0, WorldWidth - GameWidth);
        CameraY = Math.Clamp(Player.Y - GameHeight / 2.0, 0, WorldHeight - GameHeight);
    }

    public void Update(double dt, Action<Projectile>? onExplosion, Action? onPlayerHit = null)
    {
        HandleAllies(dt);
        HandleEnemies(dt);
        UpdateProjectiles(dt);
        CheckCollisions(onExplosion, onPlayerHit);
        ApplySeparation();
        _fireCooldown -= dt; if (_fireCooldown <= 0) _canFire = true;
    }

    public void MovePlayer(double moveX, double moveY, double dt)
    {
        Player.X += moveX * Player.EffectiveSpeed * dt; Player.Y += moveY * Player.EffectiveSpeed * dt; ClampToWorld(Player);

        // Set body angle based on movement direction
        if (Math.Abs(moveX) > 0 || Math.Abs(moveY) > 0)
        {
            Player.Angle = Math.Atan2(moveY, moveX);
        }

        // Update camera to follow player
        UpdateCamera();
    }

    public void AimPlayer(double aimX, double aimY)
    {
        if (Math.Abs(aimX) > 0 || Math.Abs(aimY) > 0) Player.BarrelAngle = Math.Atan2(aimY, aimX);
    }

    private void UpdateCamera()
    {
        // Camera follows player with buffer from viewport edges
        double bufferX = GameWidth * 0.45; // Start moving camera when player is 45% from edge
        double bufferY = GameHeight * 0.45;

        // Get player position relative to current camera
        double playerScreenX = Player.X - CameraX;
        double playerScreenY = Player.Y - CameraY;

        // Calculate camera adjustments needed
        double cameraAdjustX = 0;
        double cameraAdjustY = 0;

        // Check if player is too close to left edge
        if (playerScreenX < bufferX)
        {
            cameraAdjustX = playerScreenX - bufferX; // Negative value, moves camera left
        }
        // Check if player is too close to right edge
        else if (playerScreenX > GameWidth - bufferX)
        {
            cameraAdjustX = playerScreenX - (GameWidth - bufferX); // Positive value, moves camera right
        }

        // Check if player is too close to top edge
        if (playerScreenY < bufferY)
        {
            cameraAdjustY = playerScreenY - bufferY; // Negative value, moves camera up
        }
        // Check if player is too close to bottom edge
        else if (playerScreenY > GameHeight - bufferY)
        {
            cameraAdjustY = playerScreenY - (GameHeight - bufferY); // Positive value, moves camera down
        }

        // Apply camera adjustments
        double newCameraX = CameraX + cameraAdjustX;
        double newCameraY = CameraY + cameraAdjustY;

        // Clamp camera to world bounds
        CameraX = Math.Clamp(newCameraX, 0, WorldWidth - GameWidth);
        CameraY = Math.Clamp(newCameraY, 0, WorldHeight - GameHeight);
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
        double dxT = target.X - ally.X; double dyT = target.Y - ally.Y; double dist = Math.Sqrt(dxT * dxT + dyT * dyT) + 0.001; double ang = Math.Atan2(dyT, dxT);

        // Set barrel to aim at target
        ally.BarrelAngle = ang;

        double moveVX = 0, moveVY = 0;
        if (dist > 150) { moveVX = Math.Cos(ang); moveVY = Math.Sin(ang); }
        else if (dist < 110) { moveVX = -Math.Cos(ang); moveVY = -Math.Sin(ang); }
        else { moveVX = Math.Cos(ang + ally.StrafeDir * Math.PI / 2) * 0.8; moveVY = Math.Sin(ang + ally.StrafeDir * Math.PI / 2) * 0.8; }
        Normalise(ref moveVX, ref moveVY);

        // Set body angle to movement direction
        if (Math.Abs(moveVX) > 0 || Math.Abs(moveVY) > 0)
        {
            ally.Angle = Math.Atan2(moveVY, moveVX);
        }

        ally.X += moveVX * ally.EffectiveSpeed * dt; ally.Y += moveVY * ally.EffectiveSpeed * dt; ClampToWorld(ally);
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
        double dx = target.X - enemy.X; double dy = target.Y - enemy.Y; double dist = Math.Sqrt(dx * dx + dy * dy) + 0.001; double targetAngle = Math.Atan2(dy, dx);

        // Set barrel to aim at target
        enemy.BarrelAngle = targetAngle;

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

        // Set body angle to movement direction
        if (Math.Abs(moveVX) > 0 || Math.Abs(moveVY) > 0)
        {
            enemy.Angle = Math.Atan2(moveVY, moveVX);
        }

        enemy.X += moveVX * enemy.EffectiveSpeed * dt; enemy.Y += moveVY * enemy.EffectiveSpeed * dt; ClampToWorld(enemy);

        // Only fire when enemy is within engagement range based on their behavior
        bool inEngagementRange = enemy.Behavior switch
        {
            EnemyBehavior.Aggressive => dist <= 200,
            EnemyBehavior.Shy => dist <= 260,
            EnemyBehavior.Circler => dist <= 170,
            EnemyBehavior.Sniper => dist <= 300,
            EnemyBehavior.Wanderer => dist <= 150,
            EnemyBehavior.Flanker => dist <= 200,
            _ => false
        };

        enemy.NextFireTimer -= dt;
        if (enemy.NextFireTimer <= 0 && inEngagementRange)
        {
            Fire(enemy);
            enemy.NextFireTimer = enemy.Behavior == EnemyBehavior.Sniper ? 1.3 + _rand.NextDouble() * 1.0 : 0.8 + _rand.NextDouble() * 0.7;
        }
    }

    public void Fire(Tank t)
    {
        // Always use barrel angle for firing direction
        double fireAngle = t.BarrelAngle;
        Projectiles.Add(new Projectile { X = t.X + Math.Cos(fireAngle) * 20, Y = t.Y + Math.Sin(fireAngle) * 20, Angle = fireAngle, Speed = 300, OwnerTeam = t.Team });
    }

    private void UpdateProjectiles(double dt)
    {
        for (int i = Projectiles.Count - 1; i >= 0; i--)
        {
            var pr = Projectiles[i]; pr.X += Math.Cos(pr.Angle) * pr.Speed * dt; pr.Y += Math.Sin(pr.Angle) * pr.Speed * dt;
            if (pr.X < 0 || pr.Y < 0 || pr.X > WorldWidth || pr.Y > WorldHeight) Projectiles.RemoveAt(i);
        }
    }

    private void CheckCollisions(Action<Projectile>? onExplosion, Action? onPlayerHit)
    {
        for (int i = Projectiles.Count - 1; i >= 0; i--)
        {
            var pr = Projectiles[i];
            if (Player.Hp > 0 && pr.OwnerTeam != Player.Team && Hit(pr, Player)) { Player.Hp -= 10; onExplosion?.Invoke(pr); onPlayerHit?.Invoke(); Projectiles.RemoveAt(i); continue; }
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

        // Check for bullet-to-bullet collisions
        for (int i = Projectiles.Count - 1; i >= 0; i--)
        {
            for (int j = i - 1; j >= 0; j--)
            {
                var pr1 = Projectiles[i];
                var pr2 = Projectiles[j];
                if (pr1.OwnerTeam != pr2.OwnerTeam && Hit(pr1, pr2))
                {
                    onExplosion?.Invoke(pr1);
                    onExplosion?.Invoke(pr2);
                    Projectiles.RemoveAt(i);
                    Projectiles.RemoveAt(j);
                    break; // Exit inner loop since pr1 is already removed
                }
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
                    double push = (minDist - d) / 2; double nx = dx / d; double ny = dy / d; a.X -= nx * push; a.Y -= ny * push; b.X += nx * push; b.Y += ny * push; ClampToWorld(a); ClampToWorld(b);
                }
            }
        }
    }

    private bool Hit(Projectile pr, Tank t) => (pr.X - t.X) * (pr.X - t.X) + (pr.Y - t.Y) * (pr.Y - t.Y) < 22 * 22;

    private bool Hit(Projectile pr1, Projectile pr2) => (pr1.X - pr2.X) * (pr1.X - pr2.X) + (pr1.Y - pr2.Y) * (pr1.Y - pr2.Y) < 8 * 8;

    private void ClampToWorld(Tank t)
    {
        // Clamp to world bounds
        t.X = Math.Clamp(t.X, 20, WorldWidth - 20);
        t.Y = Math.Clamp(t.Y, 20, WorldHeight - 20);
    }

    private static void Normalise(ref double x, ref double y)
    {
        double mag = Math.Sqrt(x * x + y * y); if (mag > 0.001) { x /= mag; y /= mag; }
    }
}
