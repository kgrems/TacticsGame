using System;
using TacticsGame.Grid;

namespace TacticsGame.Battle;

public sealed class BattleResolver
{
    public AttackResult Attack(
        BattleGrid battleGrid,
        BattleUnit attacker,
        BattleUnit target)
    {
        ArgumentNullException.ThrowIfNull(battleGrid);
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(target);

        if (attacker.IsDefeated)
        {
            throw new InvalidOperationException(
                $"Defeated unit '{attacker.Name}' cannot attack.");
        }

        if (target.IsDefeated)
        {
            throw new InvalidOperationException(
                $"Unit '{target.Name}' is already defeated.");
        }

        if (attacker.Team == target.Team)
        {
            throw new InvalidOperationException(
                "A unit cannot attack another unit on the same team.");
        }

        var distance =
            Math.Abs(attacker.Position.X - target.Position.X) +
            Math.Abs(attacker.Position.Y - target.Position.Y);

        if (distance > attacker.AttackRange)
        {
            throw new InvalidOperationException(
                $"Target '{target.Name}' is outside attack range.");
        }

        var damage =
            Math.Max(
                1,
                attacker.EffectiveAttackDamage -
                target.EffectiveDefense);

        target.CurrentHealth = Math.Max(
            0,
            target.CurrentHealth - damage);

        if (target.IsDefeated)
        {
            battleGrid.RemoveUnit(target);
        }

        return new AttackResult(
            AttackerName: attacker.Name,
            TargetName: target.Name,
            Damage: damage,
            RemainingHealth: target.CurrentHealth,
            WasDefeated: target.IsDefeated);
    }
}
