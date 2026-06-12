using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace TacticsGame.Grid;

/// <summary>
/// Stores the result of a movement-range search.
/// In addition to the reachable destinations, it retains enough
/// information to reconstruct the route to any valid destination.
/// </summary>
public sealed class MovementSearchResult
{
    private readonly HashSet<Point> _reachableTiles;
    private readonly Dictionary<Point, Point> _previousTiles;

    public IReadOnlyCollection<Point> ReachableTiles =>
        _reachableTiles;

    public MovementSearchResult(
        IEnumerable<Point> reachableTiles,
        Dictionary<Point, Point> previousTiles)
    {
        ArgumentNullException.ThrowIfNull(
            reachableTiles);

        ArgumentNullException.ThrowIfNull(
            previousTiles);

        _reachableTiles =
            new HashSet<Point>(
                reachableTiles);

        _previousTiles =
            new Dictionary<Point, Point>(
                previousTiles);
    }

    /// <summary>
    /// Returns true when the destination is reachable from the unit's
    /// original position.
    /// </summary>
    public bool CanReach(
        Point destination)
    {
        return
            _reachableTiles.Contains(
                destination);
    }

    /// <summary>
    /// Reconstructs the shortest path from the starting tile to the
    /// requested destination. The starting tile is not included in
    /// the returned path, but the destination tile is included.
    /// </summary>
    public IReadOnlyList<Point> BuildPath(
        Point startingPosition,
        Point destination)
    {
        if (!CanReach(
                destination))
        {
            return
                Array.Empty<Point>();
        }

        var path =
            new List<Point>();

        var currentPosition =
            destination;

        while (!currentPosition.Equals(
                   startingPosition))
        {
            path.Add(
                currentPosition);

            if (!_previousTiles.TryGetValue(
                    currentPosition,
                    out currentPosition))
            {
                return
                    Array.Empty<Point>();
            }
        }

        path.Reverse();

        return
            path;
    }
}