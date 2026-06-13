#nullable enable

namespace TacticsGame.Battle;

public sealed class UnitJobDefinition
{
    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int MaximumHealth { get; init; }

    public int AttackDamage { get; init; }

    public int AttackRange { get; init; }

    public int MovementRange { get; init; }

    public int JumpHeight { get; init; }
}
