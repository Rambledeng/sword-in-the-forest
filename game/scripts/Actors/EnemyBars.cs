using Godot;

namespace SwordInTheForest.Actors;

/// <summary>
/// Health and posture above an enemy's head, plus the red finisher mark while it kneels.
/// Posture grows from the centre outward, as in the HUD.
/// </summary>
public partial class EnemyBars : Node2D
{
    private const float Width = 16;
    private readonly Enemy? _enemy;

    public EnemyBars(Enemy enemy)
    {
        _enemy = enemy;
        ZIndex = 40;
        Position = new Vector2(0, -21);
    }

    public EnemyBars() { }

    public override void _Draw()
    {
        if (_enemy == null || _enemy.Current == Enemy.State.Dead) return;
        var health = _enemy.Health / (float)Mathf.Max(1, Core.Tunables.SaberBandit.MaxHealth);
        var posture = (float)_enemy.Posture.Fraction;
        var x0 = -Width / 2;
        if (_enemy.IsKneeling)
        {
            // Finisher mark: a pulsing red dot.
            var pulse = 0.75f + 0.25f * Mathf.Sin(Time.GetTicksMsec() * 0.012f);
            DrawCircle(new Vector2(0, -5), 3.5f * pulse, new Color(0.15f, 0, 0, 0.8f));
            DrawCircle(new Vector2(0, -5), 2.5f * pulse, new Color(0.95f, 0.15f, 0.15f));
            DrawCircle(new Vector2(-0.5f, -5.5f), 0.8f, new Color(1f, 0.7f, 0.7f));
        }
        if (health < 1f || posture > 0f)
        {
            DrawRect(new Rect2(x0 - 1, -1, Width + 2, 3), new Color(0.05f, 0.05f, 0.07f, 0.85f));
            DrawRect(new Rect2(x0, 0, Mathf.Round(Width * health), 1), new Color(0.85f, 0.2f, 0.2f));
            var pw = Mathf.Round(Width * posture / 2f);
            if (pw > 0)
            {
                var c = posture >= 0.999f ? new Color(1f, 0.25f, 0.15f) : new Color(1f, 0.72f, 0.2f).Lerp(new Color(1f, 0.35f, 0.15f), posture);
                DrawRect(new Rect2(-pw, 1, pw * 2, 1), c);
            }
        }
    }
}
