namespace TacticsGame.Items;

public sealed class EquipmentItem
{
    public string Name { get; init; } = string.Empty;

    public EquipmentSlot Slot { get; init; }

    public int HealthBonus { get; init; }

    public int AttackBonus { get; init; }

    public int DefenseBonus { get; init; }

    public int MovementBonus { get; init; }
}