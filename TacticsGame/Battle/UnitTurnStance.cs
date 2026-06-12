namespace TacticsGame.Battle;

/// <summary>
/// Tracks which actions a unit has already used during its current turn.
/// </summary>
public sealed class UnitTurnState
{
    public bool HasMoved { get; private set; }

    public bool HasActed { get; private set; }

    public bool HasEndedTurn { get; private set; }

    public void MarkMoved()
    {
        HasMoved = true;
    }

    public void MarkActed()
    {
        HasActed = true;
    }

    public void MarkEndedTurn()
    {
        HasEndedTurn = true;
    }

    public void Reset()
    {
        HasMoved = false;
        HasActed = false;
        HasEndedTurn = false;
    }
}