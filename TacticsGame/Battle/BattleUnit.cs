using Microsoft.Xna.Framework;

namespace TacticsGame.Battle;

public sealed class BattleUnit
{
    public string Name { get; init; } = string.Empty;

    public BattleTeam Team { get; init; }

    public Point Position { get; set; }

    public Vector2 RenderGridPosition { get; set; }

    public int MaximumHealth { get; init; }

    public int CurrentHealth { get; set; }

    public int AttackDamage { get; init; }

    public int AttackRange { get; init; }

    public int MovementRange { get; init; }

    public int JumpHeight { get; init; }

    public UnitFacing Facing { get; set; } = UnitFacing.FrontRight;

    public UnitTurnState TurnState { get; } = new();

    public bool IsDefeated => CurrentHealth <= 0;

    public void ResetRenderPosition()
    {
        RenderGridPosition = new Vector2(Position.X, Position.Y);
    }
}