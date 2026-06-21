using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using TacticsGame.Maps;

namespace TacticsGame.Rendering;

/// <summary>
/// Draws finite isometric Tiled tile layers and converts between
/// logical grid coordinates and screen coordinates.
/// </summary>
public sealed class IsometricMapRenderer
{
    private const uint FlippedHorizontallyFlag = 0x80000000;
    private const uint FlippedVerticallyFlag = 0x40000000;
    private const uint FlippedDiagonallyFlag = 0x20000000;
    private const uint RotatedHexagonal120Flag = 0x10000000;

    private const string GroundLayerName = "Ground_Elevation_0";

    private readonly LoadedTiledMap _loadedMap;

    public Vector2 MapOrigin { get; set; }

    public IsometricMapRenderer(
        LoadedTiledMap loadedMap,
        Vector2 mapOrigin)
    {
        _loadedMap = loadedMap
            ?? throw new ArgumentNullException(nameof(loadedMap));

        MapOrigin = mapOrigin;
    }

    /// <summary>
    /// Draws all visible tile layers in the order they appear in Tiled.
    /// </summary>
    public void Draw(
        SpriteBatch spriteBatch)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);

        foreach (var layer in _loadedMap.Map.Layers)
        {
            if (!layer.Visible)
            {
                continue;
            }

            if (!string.Equals(
                    layer.Type,
                    "tilelayer",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            DrawTileLayer(
                spriteBatch,
                layer);
        }
    }

    /// <summary>
    /// Converts a mouse position into a valid logical grid coordinate.
    /// Returns false when the cursor is outside the map or over an empty
    /// cell in the base ground layer.
    /// </summary>
    public bool TryScreenToGrid(
        Vector2 screenPosition,
        out Point gridPosition)
    {
        var tileWidth =
            _loadedMap.Map.TileWidth;

        var tileHeight =
            _loadedMap.Map.TileHeight;

        var halfTileWidth =
            tileWidth /
            2.0f;

        var halfTileHeight =
            tileHeight /
            2.0f;

        var localX =
            screenPosition.X -
            MapOrigin.X;

        var localY =
            screenPosition.Y -
            MapOrigin.Y;

        var gridX =
            ((localY / halfTileHeight) +
             (localX / halfTileWidth)) /
            2.0f;

        var gridY =
            ((localY / halfTileHeight) -
             (localX / halfTileWidth)) /
            2.0f;

        gridPosition =
            new Point(
                (int)MathF.Floor(
                    gridX),
                (int)MathF.Floor(
                    gridY));

        if (!IsInsideMap(
                gridPosition))
        {
            return
                false;
        }

        return
            HasGroundTileAt(
                gridPosition);
    }

    /// <summary>
    /// Converts an integer logical grid coordinate into the top-left
    /// screen position where its tile image should be drawn.
    /// </summary>
    public Vector2 GridToScreen(
        int gridX,
        int gridY)
    {
        return
            GridToScreen(
                (float)gridX,
                (float)gridY);
    }

    /// <summary>
    /// Converts a potentially fractional logical grid coordinate into
    /// a screen position. Fractional positions are used while units walk
    /// smoothly between tiles.
    /// </summary>
    public Vector2 GridToScreen(
        float gridX,
        float gridY)
    {
        var tileWidth =
            _loadedMap.Map.TileWidth;

        var tileHeight =
            _loadedMap.Map.TileHeight;

        var halfTileWidth =
            tileWidth /
            2.0f;

        var halfTileHeight =
            tileHeight /
            2.0f;

        var screenX =
            MapOrigin.X +
            ((gridX - gridY) *
             halfTileWidth) -
            halfTileWidth;

        var screenY =
            MapOrigin.Y +
            ((gridX + gridY) *
             halfTileHeight);

        return
            new Vector2(
                screenX,
                screenY);
    }

    private void DrawTileLayer(
        SpriteBatch spriteBatch,
        TiledLayer layer)
    {
        var map = _loadedMap.Map;

        var layerWidth = layer.Width > 0
            ? layer.Width
            : map.Width;

        var layerHeight = layer.Height > 0
            ? layer.Height
            : map.Height;

        var expectedTileCount =
            layerWidth *
            layerHeight;

        if (layer.Data.Count != expectedTileCount)
        {
            throw new InvalidOperationException(
                $"Layer '{layer.Name}' has {layer.Data.Count} tile entries, " +
                $"but {expectedTileCount} were expected.");
        }

        for (var gridY = 0; gridY < layerHeight; gridY++)
        {
            for (var gridX = 0; gridX < layerWidth; gridX++)
            {
                var tileIndex =
                    (gridY * layerWidth) +
                    gridX;

                var rawGlobalTileId =
                    layer.Data[tileIndex];

                DrawTile(
                    spriteBatch,
                    layer,
                    rawGlobalTileId,
                    gridX,
                    gridY);
            }
        }
    }

    private void DrawTile(
        SpriteBatch spriteBatch,
        TiledLayer layer,
        uint rawGlobalTileId,
        int gridX,
        int gridY)
    {
        var globalTileId =
            RemoveFlipFlags(
                rawGlobalTileId);

        if (globalTileId == 0)
        {
            return;
        }

        var loadedTileset =
            FindTileset(
                globalTileId);

        var localTileId = checked(
            (int)(
                globalTileId -
                loadedTileset.FirstGlobalTileId));

        var tileset =
            loadedTileset.Definition;

        var sourceRectangle =
            new Rectangle(
                x:
                    (localTileId % tileset.Columns) *
                    tileset.TileWidth,
                y:
                    (localTileId / tileset.Columns) *
                    tileset.TileHeight,
                width:
                    tileset.TileWidth,
                height:
                    tileset.TileHeight);

        var drawPosition =
            GridToScreen(
                gridX,
                gridY);

        drawPosition.X += layer.OffsetX;
        drawPosition.Y += layer.OffsetY;

        spriteBatch.Draw(
            texture: loadedTileset.Texture,
            position: drawPosition,
            sourceRectangle: sourceRectangle,
            color: Color.White * layer.Opacity);
    }

    private bool IsInsideMap(
        Point gridPosition)
    {
        return
            gridPosition.X >= 0 &&
            gridPosition.Y >= 0 &&
            gridPosition.X < _loadedMap.Map.Width &&
            gridPosition.Y < _loadedMap.Map.Height;
    }

    private bool HasGroundTileAt(
        Point gridPosition)
    {
        var groundLayer =
            _loadedMap.Map.Layers
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
                : _loadedMap.Map.Width;

        var tileIndex =
            (gridPosition.Y * layerWidth) +
            gridPosition.X;

        if (tileIndex < 0 ||
            tileIndex >= groundLayer.Data.Count)
        {
            return false;
        }

        return
            RemoveFlipFlags(
                groundLayer.Data[tileIndex]) != 0;
    }

    private LoadedTileset FindTileset(
        uint globalTileId)
    {
        var loadedTileset =
            _loadedMap.Tilesets
                .Where(tileset =>
                    tileset.FirstGlobalTileId <=
                    globalTileId)
                .OrderByDescending(tileset =>
                    tileset.FirstGlobalTileId)
                .FirstOrDefault();

        if (loadedTileset is null)
        {
            throw new InvalidOperationException(
                $"No tileset could be found for global tile ID {globalTileId}.");
        }

        return loadedTileset;
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
