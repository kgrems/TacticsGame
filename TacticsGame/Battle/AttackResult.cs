namespace TacticsGame.Battle;

public sealed record AttackResult(
    string AttackerName,
    string TargetName,
    int Damage,
    int RemainingHealth,
    bool WasDefeated);