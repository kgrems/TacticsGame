#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticsGame.Battle;

/// <summary>
/// Coordinates player and enemy phases.
/// </summary>
public sealed class BattleTurnController
{
    public int RoundNumber { get; private set; } = 1;

    public BattleTeam ActiveTeam { get; private set; } = BattleTeam.Player;

    public BattleUnit? ActiveUnit { get; private set; }

    public void BeginBattle(
        IEnumerable<BattleUnit> units)
    {
        ArgumentNullException.ThrowIfNull(
            units);

        RoundNumber = 1;

        StartTeamTurn(
            BattleTeam.Player,
            units);
    }

    public bool CanSelectUnit(
        BattleUnit unit)
    {
        ArgumentNullException.ThrowIfNull(
            unit);

        return unit.Team == ActiveTeam &&
               !unit.IsDefeated &&
               !unit.TurnState.HasEndedTurn &&
               unit == ActiveUnit;
    }

    public void EndUnitTurn(
        BattleUnit unit,
        IEnumerable<BattleUnit> units)
    {
        ArgumentNullException.ThrowIfNull(
            unit);

        ArgumentNullException.ThrowIfNull(
            units);

        if (!CanSelectUnit(unit))
        {
            return;
        }

        unit.TurnState.MarkEndedTurn();

        if (!AreAllUnitsFinished(
                ActiveTeam,
                units))
        {
            ActiveUnit =
                GetNextReadyUnit(
                    ActiveTeam,
                    units);

            return;
        }

        AdvanceTeam(
            units);
    }

    public void SkipActiveTeamTurn(
        IEnumerable<BattleUnit> units)
    {
        ArgumentNullException.ThrowIfNull(
            units);

        foreach (var unit in units.Where(unit =>
                     unit.Team == ActiveTeam &&
                     !unit.IsDefeated))
        {
            unit.TurnState.MarkEndedTurn();
        }

        AdvanceTeam(
            units);
    }

    private void AdvanceTeam(
        IEnumerable<BattleUnit> units)
    {
        if (ActiveTeam == BattleTeam.Player)
        {
            StartTeamTurn(
                BattleTeam.Enemy,
                units);

            return;
        }

        RoundNumber++;

        StartTeamTurn(
            BattleTeam.Player,
            units);
    }

    private void StartTeamTurn(
        BattleTeam team,
        IEnumerable<BattleUnit> units)
    {
        ActiveTeam = team;

        foreach (var unit in units.Where(unit =>
                     unit.Team == team &&
                     !unit.IsDefeated))
        {
            unit.TurnState.Reset();
        }

        ActiveUnit =
            GetNextReadyUnit(
                team,
                units);
    }

    private static BattleUnit? GetNextReadyUnit(
        BattleTeam team,
        IEnumerable<BattleUnit> units)
    {
        return units.FirstOrDefault(unit =>
            unit.Team == team &&
            !unit.IsDefeated &&
            !unit.TurnState.HasEndedTurn);
    }

    private static bool AreAllUnitsFinished(
        BattleTeam team,
        IEnumerable<BattleUnit> units)
    {
        var activeUnits = units
            .Where(unit =>
                unit.Team == team &&
                !unit.IsDefeated)
            .ToList();

        return activeUnits.Count == 0 ||
               activeUnits.All(unit =>
                   unit.TurnState.HasEndedTurn);
    }
}
