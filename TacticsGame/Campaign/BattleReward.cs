#nullable enable

using TacticsGame.Items;

namespace TacticsGame.Campaign;

public sealed class BattleReward
{
    public string EncounterName { get; init; } = string.Empty;

    public int Experience { get; init; }

    public EquipmentItem? Gear { get; init; }

    public bool FullyRecovered { get; init; } = true;
}
