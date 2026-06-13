using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TacticsGame.Grid;

namespace TacticsGame.Battle;

public sealed class EnemyAiController
{
    public EnemyTurnDecision DecideAction(
        BattleGrid battleGrid,
        BattleUnit enemy,
        IReadOnlyCollection<BattleUnit> allUnits,
        MovementRangeCalculator movementRangeCalculator)
    {
        var livingPlayers =
            allUnits
                .Where(x =>
                    x.Team == BattleTeam.Player &&
                    !x.IsDefeated)
                .ToList();

        if (livingPlayers.Count == 0)
        {
            return EnemyTurnDecision.Wait();
        }

        var nearestPlayer =
            livingPlayers
                .OrderBy(x =>
                    ManhattanDistance(
                        enemy.Position,
                        x.Position))
                .First();

        var distance =
            ManhattanDistance(
                enemy.Position,
                nearestPlayer.Position);

        if (distance <= enemy.AttackRange)
        {
            return EnemyTurnDecision.Attack(
                nearestPlayer);
        }

        var searchResult =
            movementRangeCalculator.CalculateReachableTiles(
                battleGrid,
                enemy);

        Point? bestDestination = null;
        var bestDistance = int.MaxValue;

        foreach (var tile in searchResult.ReachableTiles)
        {
            var tileDistance =
                ManhattanDistance(
                    tile,
                    nearestPlayer.Position);

            if (tileDistance < bestDistance)
            {
                bestDistance = tileDistance;
                bestDestination = tile;
            }
        }

        if (bestDestination.HasValue)
        {
            return EnemyTurnDecision.Move(
                nearestPlayer,
                bestDestination.Value);
        }

        return EnemyTurnDecision.Wait();
    }

    private static int ManhattanDistance(
        Point a,
        Point b)
    {
        return
            Math.Abs(a.X - b.X) +
            Math.Abs(a.Y - b.Y);
    }
}