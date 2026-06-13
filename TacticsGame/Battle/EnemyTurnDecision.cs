using Microsoft.Xna.Framework;

namespace TacticsGame.Battle;

public sealed class EnemyTurnDecision
{
    public EnemyTurnDecisionType Type { get; }

    public BattleUnit? Target { get; }

    public Point? Destination { get; }

    private EnemyTurnDecision(
        EnemyTurnDecisionType type,
        BattleUnit? target,
        Point? destination)
    {
        Type = type;
        Target = target;
        Destination = destination;
    }

    public static EnemyTurnDecision Attack(
        BattleUnit target)
    {
        return new(
            EnemyTurnDecisionType.Attack,
            target,
            null);
    }

    public static EnemyTurnDecision Move(
        BattleUnit target,
        Point destination)
    {
        return new(
            EnemyTurnDecisionType.Move,
            target,
            destination);
    }

    public static EnemyTurnDecision Wait()
    {
        return new(
            EnemyTurnDecisionType.Wait,
            null,
            null);
    }
}