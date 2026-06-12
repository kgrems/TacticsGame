using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using TacticsGame.Grid;

namespace TacticsGame.Battle;

/// <summary>
/// Moves a unit smoothly along a sequence of logical grid tiles.
/// The unit's grid occupancy is updated each time it completes
/// one tile of movement.
/// </summary>
public sealed class UnitMovementController
{
    private const float TilesPerSecond =
        4.0f;

    private readonly Queue<Point> _remainingPath =
        new();

    private BattleGrid? _battleGrid;

    private Point _segmentStart;
    private Point _segmentDestination;

    private float _segmentProgress;

    public BattleUnit? MovingUnit { get; private set; }

    public bool IsMoving =>
        MovingUnit is not null;

    /// <summary>
    /// Begins moving a unit along the supplied path.
    /// The path should not include the starting tile.
    /// </summary>
    public void BeginMove(
        BattleGrid battleGrid,
        BattleUnit unit,
        IReadOnlyList<Point> path)
    {
        ArgumentNullException.ThrowIfNull(
            battleGrid);

        ArgumentNullException.ThrowIfNull(
            unit);

        ArgumentNullException.ThrowIfNull(
            path);

        if (IsMoving)
        {
            throw new InvalidOperationException(
                "A unit is already moving.");
        }

        if (path.Count == 0)
        {
            throw new ArgumentException(
                "The movement path must contain at least one destination tile.",
                nameof(path));
        }

        _battleGrid =
            battleGrid;

        MovingUnit =
            unit;

        _remainingPath.Clear();

        foreach (var tile
                 in path)
        {
            _remainingPath.Enqueue(
                tile);
        }

        _segmentProgress =
            0.0f;

        unit.ResetRenderPosition();

        BeginNextSegment();
    }

    /// <summary>
    /// Updates the current movement animation.
    /// Returns true during the frame in which the unit finishes moving.
    /// </summary>
    public bool Update(
        GameTime gameTime)
    {
        if (!IsMoving)
        {
            return
                false;
        }

        var elapsedSeconds =
            (float)gameTime
                .ElapsedGameTime
                .TotalSeconds;

        _segmentProgress +=
            elapsedSeconds *
            TilesPerSecond;

        while (IsMoving &&
               _segmentProgress >=
               1.0f)
        {
            CompleteCurrentSegment();

            _segmentProgress -=
                1.0f;

            if (_remainingPath.Count ==
                0)
            {
                FinishMovement();

                return
                    true;
            }

            BeginNextSegment();
        }

        UpdateInterpolatedRenderPosition();

        return
            false;
    }

    private void BeginNextSegment()
    {
        if (MovingUnit is null)
        {
            return;
        }

        if (_remainingPath.Count ==
            0)
        {
            FinishMovement();

            return;
        }

        _segmentStart =
            MovingUnit.Position;

        _segmentDestination =
            _remainingPath.Dequeue();

        UpdateFacing(
            MovingUnit,
            _segmentStart,
            _segmentDestination);

        UpdateInterpolatedRenderPosition();
    }

    private void CompleteCurrentSegment()
    {
        if (MovingUnit is null ||
            _battleGrid is null)
        {
            return;
        }

        _battleGrid.MoveUnit(
            MovingUnit,
            _segmentDestination);

        MovingUnit.ResetRenderPosition();
    }

    private void UpdateInterpolatedRenderPosition()
    {
        if (MovingUnit is null)
        {
            return;
        }

        var start =
            new Vector2(
                _segmentStart.X,
                _segmentStart.Y);

        var destination =
            new Vector2(
                _segmentDestination.X,
                _segmentDestination.Y);

        MovingUnit.RenderGridPosition =
            Vector2.Lerp(
                start,
                destination,
                Math.Clamp(
                    _segmentProgress,
                    0.0f,
                    1.0f));
    }

    private void FinishMovement()
    {
        if (MovingUnit is not null)
        {
            MovingUnit.ResetRenderPosition();
        }

        _remainingPath.Clear();

        _battleGrid =
            null;

        MovingUnit =
            null;

        _segmentProgress =
            0.0f;
    }

    private static void UpdateFacing(
        BattleUnit unit,
        Point start,
        Point destination)
    {
        var deltaX =
            destination.X -
            start.X;

        var deltaY =
            destination.Y -
            start.Y;

        if (deltaX >
            0)
        {
            unit.Facing =
                UnitFacing.FrontRight;

            return;
        }

        if (deltaX <
            0)
        {
            unit.Facing =
                UnitFacing.BackLeft;

            return;
        }

        if (deltaY >
            0)
        {
            unit.Facing =
                UnitFacing.FrontLeft;

            return;
        }

        if (deltaY <
            0)
        {
            unit.Facing =
                UnitFacing.BackRight;
        }
    }
}