using Microsoft.Xna.Framework;
using TacticsGame.Battle;

namespace TacticsGame.Grid;

/// <summary>
/// Represents one logical tile in the tactical battle grid.
/// </summary>
public sealed class BattleTile
{
    public Point Position { get; }

    public bool IsWalkable { get; set; }

    public int Elevation { get; set; }

    public BattleUnit? OccupyingUnit { get; set; }

    public BattleTile(
        Point position,
        bool isWalkable,
        int elevation = 0)
    {
        Position =
            position;

        IsWalkable =
            isWalkable;

        Elevation =
            elevation;
    }
}