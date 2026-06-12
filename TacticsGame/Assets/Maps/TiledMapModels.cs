using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TacticsGame.Maps;

/// <summary>
/// Represents the subset of Tiled's .tmj JSON map format that the game
/// currently needs. Additional fields can be added as the engine grows.
/// </summary>
public sealed class TiledMap
{
    [JsonPropertyName("width")]
    public int Width { get; init; }

    [JsonPropertyName("height")]
    public int Height { get; init; }

    [JsonPropertyName("tilewidth")]
    public int TileWidth { get; init; }

    [JsonPropertyName("tileheight")]
    public int TileHeight { get; init; }

    [JsonPropertyName("orientation")]
    public string Orientation { get; init; } = string.Empty;

    [JsonPropertyName("infinite")]
    public bool Infinite { get; init; }

    [JsonPropertyName("layers")]
    public List<TiledLayer> Layers { get; init; } = [];

    [JsonPropertyName("tilesets")]
    public List<TiledTilesetReference> Tilesets { get; init; } = [];
}

/// <summary>
/// Represents a layer from the Tiled map.
/// This first renderer supports finite tile layers.
/// </summary>
public sealed class TiledLayer
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("width")]
    public int Width { get; init; }

    [JsonPropertyName("height")]
    public int Height { get; init; }

    [JsonPropertyName("visible")]
    public bool Visible { get; init; } = true;

    [JsonPropertyName("opacity")]
    public float Opacity { get; init; } = 1.0f;

    [JsonPropertyName("offsetx")]
    public float OffsetX { get; init; }

    [JsonPropertyName("offsety")]
    public float OffsetY { get; init; }

    [JsonPropertyName("data")]
    public List<uint> Data { get; init; } = [];
}

/// <summary>
/// Represents a tileset reference stored inside a .tmj map.
/// The actual tileset definition is stored in the external .tsj file.
/// </summary>
public sealed class TiledTilesetReference
{
    [JsonPropertyName("firstgid")]
    public uint FirstGlobalTileId { get; init; }

    [JsonPropertyName("source")]
    public string Source { get; init; } = string.Empty;
}

/// <summary>
/// Represents the subset of an external Tiled .tsj tileset definition
/// that the renderer currently needs.
/// </summary>
public sealed class TiledTileset
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("tilewidth")]
    public int TileWidth { get; init; }

    [JsonPropertyName("tileheight")]
    public int TileHeight { get; init; }

    [JsonPropertyName("tilecount")]
    public int TileCount { get; init; }

    [JsonPropertyName("columns")]
    public int Columns { get; init; }

    [JsonPropertyName("image")]
    public string Image { get; init; } = string.Empty;

    [JsonPropertyName("imagewidth")]
    public int ImageWidth { get; init; }

    [JsonPropertyName("imageheight")]
    public int ImageHeight { get; init; }
}