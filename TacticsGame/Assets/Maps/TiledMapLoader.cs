using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TacticsGame.Maps;

/// <summary>
/// Loads a finite isometric Tiled JSON map, its external tilesets,
/// and the PNG atlas referenced by each external tileset.
/// </summary>
public static class TiledMapLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static LoadedTiledMap Load(GraphicsDevice graphicsDevice, string mapFilePath)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        if (string.IsNullOrWhiteSpace(mapFilePath))
        {
            throw new ArgumentException("A map file path is required.", nameof(mapFilePath));
        }

        var absoluteMapPath = Path.GetFullPath(mapFilePath);

        if (!File.Exists(absoluteMapPath))
        {
            throw new FileNotFoundException("The Tiled map file could not be found.", absoluteMapPath);
        }

        var mapJson = File.ReadAllText(absoluteMapPath);

        var map = JsonSerializer.Deserialize<TiledMap>(mapJson, JsonOptions);

        if (map is null)
        {
            throw new InvalidOperationException($"The map file could not be deserialized: {absoluteMapPath}");
        }

        ValidateMap(map, absoluteMapPath);

        var mapDirectory = Path.GetDirectoryName(absoluteMapPath);

        if (string.IsNullOrWhiteSpace(mapDirectory))
        {
            throw new InvalidOperationException($"The map directory could not be determined: {absoluteMapPath}");
        }

        var loadedTilesets = map.Tilesets
            .Select(reference => LoadTileset(graphicsDevice, mapDirectory, reference))
            .OrderBy(tileset => tileset.FirstGlobalTileId)
            .ToList();

        return new LoadedTiledMap(
            map,
            loadedTilesets);
    }

    private static LoadedTileset LoadTileset(GraphicsDevice graphicsDevice, string mapDirectory, TiledTilesetReference reference)
    {
        if (string.IsNullOrWhiteSpace(reference.Source))
        {
            throw new NotSupportedException("This starter loader expects external .tsj tilesets.");
        }

        var tilesetFilePath = Path.GetFullPath(Path.Combine(mapDirectory, reference.Source));

        if (!File.Exists(tilesetFilePath))
        {
            throw new FileNotFoundException("The external Tiled tileset file could not be found.", tilesetFilePath);
        }

        var tilesetJson = File.ReadAllText(tilesetFilePath);

        var tileset = JsonSerializer.Deserialize<TiledTileset>(tilesetJson, JsonOptions);

        if (tileset is null)
        {
            throw new InvalidOperationException($"The tileset file could not be deserialized: {tilesetFilePath}");
        }

        ValidateTileset(tileset, tilesetFilePath);

        var tilesetDirectory = Path.GetDirectoryName(tilesetFilePath);

        if (string.IsNullOrWhiteSpace(tilesetDirectory))
        {
            throw new InvalidOperationException($"The tileset directory could not be determined: {tilesetFilePath}");
        }

        var imageFilePath = Path.GetFullPath(Path.Combine(tilesetDirectory, tileset.Image));

        if (!File.Exists(imageFilePath))
        {
            throw new FileNotFoundException("The tileset image could not be found.", imageFilePath);
        }

        var texture = Texture2D.FromFile(graphicsDevice, imageFilePath);

        return new LoadedTileset(reference.FirstGlobalTileId, tileset, texture, tilesetFilePath, imageFilePath);
    }

    private static void ValidateMap(TiledMap map, string mapFilePath)
    {
        if (!string.Equals(map.Orientation, "isometric", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                $"The starter renderer expects an isometric Tiled map. " +
                $"The map orientation was '{map.Orientation}'. " +
                $"File: {mapFilePath}");
        }

        if (map.Infinite)
        {
            throw new NotSupportedException("Infinite Tiled maps are not supported by this starter renderer.");
        }

        if (map.Width <= 0 || map.Height <= 0)
        {
            throw new InvalidOperationException("The map must have a positive width and height.");
        }

        if (map.TileWidth <= 0 || map.TileHeight <= 0)
        {
            throw new InvalidOperationException("The map must have a positive tile width and tile height.");
        }

        if (map.Tilesets.Count == 0)
        {
            throw new InvalidOperationException("The map does not reference any tilesets.");
        }
    }

    private static void ValidateTileset(TiledTileset tileset, string tilesetFilePath)
    {
        if (tileset.TileWidth <= 0 || tileset.TileHeight <= 0)
        {
            throw new InvalidOperationException($"The tileset has invalid tile dimensions: {tilesetFilePath}");
        }

        if (tileset.Columns <= 0)
        {
            throw new InvalidOperationException($"The tileset must contain at least one column: {tilesetFilePath}");
        }

        if (string.IsNullOrWhiteSpace(tileset.Image))
        {
            throw new InvalidOperationException($"The tileset does not reference an image: {tilesetFilePath}");
        }
    }
}

/// <summary>
/// Holds a loaded map and its loaded tilesets.
/// Dispose this object when the game shuts down.
/// </summary>
public sealed class LoadedTiledMap : IDisposable
{
    public TiledMap Map { get; }

    public IReadOnlyList<LoadedTileset> Tilesets { get; }

    public LoadedTiledMap(TiledMap map, IReadOnlyList<LoadedTileset> tilesets)
    {
        Map = map ?? throw new ArgumentNullException(nameof(map));
        Tilesets = tilesets ?? throw new ArgumentNullException(nameof(tilesets));
    }

    public void Dispose()
    {
        foreach (var tileset in Tilesets)
        {
            tileset.Dispose();
        }
    }
}

/// <summary>
/// Holds one external tileset definition and its loaded PNG texture.
/// </summary>
public sealed class LoadedTileset : IDisposable
{
    public uint FirstGlobalTileId { get; }

    public TiledTileset Definition { get; }

    public Texture2D Texture { get; }

    public string TilesetFilePath { get; }

    public string ImageFilePath { get; }

    public LoadedTileset(uint firstGlobalTileId, TiledTileset definition, Texture2D texture, string tilesetFilePath, string imageFilePath)
    {
        FirstGlobalTileId = firstGlobalTileId;
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Texture = texture ?? throw new ArgumentNullException(nameof(texture));
        TilesetFilePath = tilesetFilePath;
        ImageFilePath = imageFilePath;
    }

    public void Dispose()
    {
        Texture.Dispose();
    }
}