using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TacticsGame.Battle;
using TacticsGame.Maps;

namespace TacticsGame.Grid;

/// <summary>
/// Holds the logical battle tiles independently from the visual renderer.
/// </summary>
public sealed class BattleGrid
{
    private const string GroundLayerName =
        "Ground_Elevation_0";

    private const uint FlippedHorizontallyFlag =
        0x80000000;

    private const uint FlippedVerticallyFlag =
        0x40000000;

    private const uint FlippedDiagonallyFlag =
        0x20000000;

    private const uint RotatedHexagonal120Flag =
        0x10000000;

    private static readonly Point[] CardinalDirections =
    {
        new(
            x: -1,
            y: 0),

        new(
            x: 1,
            y: 0),

        new(
            x: 0,
            y: -1),

        new(
            x: 0,
            y: 1)
    };

    private readonly Dictionary<Point, BattleTile> _tiles =
        new();

    public int Width { get; }

    public int Height { get; }

    private BattleGrid(
        int width,
        int height)
    {
        Width =
            width;

        Height =
            height;
    }

    /// <summary>
    /// Builds a logical battle grid from the base ground layer.
    /// Any painted tile is walkable for now.
    /// Empty cells are treated as non-walkable.
    /// </summary>
    public static BattleGrid FromLoadedMap(
        LoadedTiledMap loadedMap)
    {
        ArgumentNullException.ThrowIfNull(
            loadedMap);

        var map =
            loadedMap.Map;

        var groundLayer =
            map.Layers
                .FirstOrDefault(layer =>
                    string.Equals(
                        layer.Name,
                        GroundLayerName,
                        StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        layer.Type,
                        "tilelayer",
                        StringComparison.OrdinalIgnoreCase));

        if (groundLayer is null)
        {
            throw new InvalidOperationException(
                $"The map does not contain a tile layer named '{GroundLayerName}'.");
        }

        var layerWidth =
            groundLayer.Width > 0
                ? groundLayer.Width
                : map.Width;

        var layerHeight =
            groundLayer.Height > 0
                ? groundLayer.Height
                : map.Height;

        var expectedTileCount =
            layerWidth *
            layerHeight;

        if (groundLayer.Data.Count !=
            expectedTileCount)
        {
            throw new InvalidOperationException(
                $"Layer '{GroundLayerName}' has {groundLayer.Data.Count} tile entries, " +
                $"but {expectedTileCount} were expected.");
        }

        var battleGrid =
            new BattleGrid(
                width:
                    layerWidth,
                height:
                    layerHeight);

        for (var y = 0;
             y < layerHeight;
             y++)
        {
            for (var x = 0;
                 x < layerWidth;
                 x++)
            {
                var tileIndex =
                    (y * layerWidth) +
                    x;

                var rawGlobalTileId =
                    groundLayer.Data[
                        tileIndex];

                var globalTileId =
                    RemoveFlipFlags(
                        rawGlobalTileId);

                var tile =
                    new BattleTile(
                        position:
                            new Point(
                                x,
                                y),

                        isWalkable:
                            globalTileId != 0,

                        elevation:
                            0);

                battleGrid
                    ._tiles[
                        tile.Position] =
                    tile;
            }
        }

        return
            battleGrid;
    }

    public bool TryGetTile(
        Point position,
        out BattleTile? tile)
    {
        return
            _tiles.TryGetValue(
                position,
                out tile);
    }

    public BattleTile GetTile(
        Point position)
    {
        if (!_tiles.TryGetValue(
                position,
                out var tile))
        {
            throw new ArgumentOutOfRangeException(
                nameof(position),
                $"Tile ({position.X}, {position.Y}) does not exist.");
        }

        return
            tile;
    }

    public BattleUnit? GetUnitAt(
        Point position)
    {
        return
            TryGetTile(
                position,
                out var tile)
                ? tile?.OccupyingUnit
                : null;
    }

    public IEnumerable<BattleTile> GetCardinalNeighbors(
        Point position)
    {
        foreach (var direction
                 in CardinalDirections)
        {
            var neighborPosition =
                new Point(
                    x:
                        position.X +
                        direction.X,

                    y:
                        position.Y +
                        direction.Y);

            if (TryGetTile(
                    neighborPosition,
                    out var tile) &&
                tile is not null)
            {
                yield return
                    tile;
            }
        }
    }

    public bool CanEnterTile(
        BattleTile tile,
        BattleUnit movingUnit)
    {
        ArgumentNullException.ThrowIfNull(
            tile);

        ArgumentNullException.ThrowIfNull(
            movingUnit);

        if (!tile.IsWalkable)
        {
            return
                false;
        }

        return
            tile.OccupyingUnit is null ||
            tile.OccupyingUnit ==
            movingUnit;
    }

    public void PlaceUnit(
        BattleUnit unit)
    {
        ArgumentNullException.ThrowIfNull(
            unit);

        var tile =
            GetTile(
                unit.Position);

        if (!CanEnterTile(
                tile,
                unit))
        {
            throw new InvalidOperationException(
                $"Unit '{unit.Name}' cannot be placed at " +
                $"({unit.Position.X}, {unit.Position.Y}).");
        }

        tile.OccupyingUnit =
            unit;
    }

    public void MoveUnit(
    BattleUnit unit,
    Point destination)
    {
        ArgumentNullException.ThrowIfNull(unit);

        var currentTile =
            GetTile(unit.Position);

        var destinationTile =
            GetTile(destination);

        if (!CanEnterTile(destinationTile, unit))
        {
            throw new InvalidOperationException(
                $"Unit '{unit.Name}' cannot move to " +
                $"({destination.X}, {destination.Y}).");
        }

        if (currentTile.OccupyingUnit == unit)
        {
            currentTile.OccupyingUnit = null;
        }

        unit.Position = destination;

        unit.RenderGridPosition =
            new Vector2(
                destination.X,
                destination.Y);

        destinationTile.OccupyingUnit = unit;
    }
    public void RemoveUnit(
    BattleUnit unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        var tile = GetTile(unit.Position);

        if (tile.OccupyingUnit == unit)
        {
            tile.OccupyingUnit = null;
        }
    }

    private static uint RemoveFlipFlags(
        uint rawGlobalTileId)
    {
        return
            rawGlobalTileId &
            ~FlippedHorizontallyFlag &
            ~FlippedVerticallyFlag &
            ~FlippedDiagonallyFlag &
            ~RotatedHexagonal120Flag;
    }
}