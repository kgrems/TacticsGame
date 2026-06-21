using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using TacticsGame.Battle;

namespace TacticsGame.Grid;

/// <summary>
/// Calculates which tiles a unit can reach using breadth-first search.
/// The search result also retains the shortest path to each destination.
/// </summary>
public sealed class MovementRangeCalculator
{
    public MovementSearchResult CalculateReachableTiles(
        BattleGrid battleGrid,
        BattleUnit unit)
    {
        ArgumentNullException.ThrowIfNull(
            battleGrid);

        ArgumentNullException.ThrowIfNull(
            unit);

        var reachableTiles =
            new HashSet<Point>();

        var bestCosts =
            new Dictionary<Point, int>();

        var previousTiles =
            new Dictionary<Point, Point>();

        var pendingTiles =
            new PriorityQueue<Point, int>();

        bestCosts[
            unit.Position] =
            0;

        pendingTiles.Enqueue(
            unit.Position,
            0);

        while (pendingTiles.Count >
               0)
        {
            var currentPosition =
                pendingTiles.Dequeue();

            var currentCost =
                bestCosts[
                    currentPosition];

            var currentTile =
                battleGrid.GetTile(
                    currentPosition);

            foreach (var neighborTile
                     in battleGrid
                         .GetCardinalNeighbors(
                             currentPosition))
            {
                if (!battleGrid.CanEnterTile(
                        neighborTile,
                        unit))
                {
                    continue;
                }

                var stepCost =
                    battleGrid.GetMovementCost(
                        currentTile,
                        neighborTile,
                        unit);

                if (stepCost == int.MaxValue)
                {
                    continue;
                }

                var newCost =
                    currentCost +
                    stepCost;

                if (newCost >
                    unit.EffectiveMovementRange)
                {
                    continue;
                }

                if (bestCosts.TryGetValue(
                        neighborTile.Position,
                        out var previousCost) &&
                    previousCost <=
                    newCost)
                {
                    continue;
                }

                bestCosts[
                    neighborTile.Position] =
                    newCost;

                previousTiles[
                    neighborTile.Position] =
                    currentPosition;

                reachableTiles.Add(
                    neighborTile.Position);

                pendingTiles.Enqueue(
                    neighborTile.Position,
                    newCost);
            }
        }

        return
            new MovementSearchResult(
                reachableTiles,
                previousTiles);
    }
}
