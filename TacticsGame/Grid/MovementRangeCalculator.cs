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
            new Queue<MovementStep>();

        bestCosts[
            unit.Position] =
            0;

        pendingTiles.Enqueue(
            new MovementStep(
                Position:
                    unit.Position,

                Cost:
                    0));

        while (pendingTiles.Count >
               0)
        {
            var currentStep =
                pendingTiles.Dequeue();

            var currentTile =
                battleGrid.GetTile(
                    currentStep.Position);

            foreach (var neighborTile
                     in battleGrid
                         .GetCardinalNeighbors(
                             currentStep.Position))
            {
                var newCost =
                    currentStep.Cost +
                    1;

                if (newCost >
                    unit.EffectiveMovementRange)
                {
                    continue;
                }

                if (!battleGrid.CanEnterTile(
                        neighborTile,
                        unit))
                {
                    continue;
                }

                var elevationDifference =
                    Math.Abs(
                        neighborTile.Elevation -
                        currentTile.Elevation);

                if (elevationDifference >
                    unit.JumpHeight)
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
                    currentStep.Position;

                reachableTiles.Add(
                    neighborTile.Position);

                pendingTiles.Enqueue(
                    new MovementStep(
                        Position:
                            neighborTile.Position,

                        Cost:
                            newCost));
            }
        }

        return
            new MovementSearchResult(
                reachableTiles,
                previousTiles);
    }

    private sealed record MovementStep(
        Point Position,
        int Cost);
}
