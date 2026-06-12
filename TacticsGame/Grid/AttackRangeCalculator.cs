using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using TacticsGame.Battle;

namespace TacticsGame.Grid;

public sealed class AttackRangeCalculator
{
    public HashSet<Point> CalculateAttackableTiles(
        BattleGrid battleGrid,
        BattleUnit unit)
    {
        ArgumentNullException.ThrowIfNull(battleGrid);
        ArgumentNullException.ThrowIfNull(unit);

        var attackableTiles = new HashSet<Point>();

        for (var y = 0; y < battleGrid.Height; y++)
        {
            for (var x = 0; x < battleGrid.Width; x++)
            {
                var position = new Point(x, y);
                var distance =
                    Math.Abs(position.X - unit.Position.X) +
                    Math.Abs(position.Y - unit.Position.Y);

                if (distance == 0 || distance > unit.AttackRange)
                {
                    continue;
                }

                if (!battleGrid.TryGetTile(position, out var tile) ||
                    tile is null ||
                    !tile.IsWalkable)
                {
                    continue;
                }

                attackableTiles.Add(position);
            }
        }

        return attackableTiles;
    }
}