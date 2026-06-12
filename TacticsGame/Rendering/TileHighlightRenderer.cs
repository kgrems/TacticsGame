using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace TacticsGame.Rendering;

/// <summary>
/// Draws an in-memory diamond overlay over hovered and selected tiles.
/// </summary>
public sealed class TileHighlightRenderer : IDisposable
{
    private readonly IsometricMapRenderer _mapRenderer;
    private readonly Texture2D _diamondTexture;

    public TileHighlightRenderer(
        GraphicsDevice graphicsDevice,
        IsometricMapRenderer mapRenderer,
        int tileWidth,
        int tileHeight)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        _mapRenderer =
            mapRenderer ??
            throw new ArgumentNullException(nameof(mapRenderer));

        _diamondTexture =
            CreateDiamondTexture(
                graphicsDevice,
                tileWidth,
                tileHeight);
    }

    public void Draw(
        SpriteBatch spriteBatch,
        Point? hoveredTile,
        Point? selectedTile)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);

        if (selectedTile.HasValue)
        {
            DrawTileHighlight(
                spriteBatch,
                selectedTile.Value,
                new Color(
                    r: 255,
                    g: 215,
                    b: 0,
                    alpha: 210));
        }

        if (hoveredTile.HasValue &&
            (!selectedTile.HasValue ||
             !hoveredTile.Value.Equals(
                 selectedTile.Value)))
        {
            DrawTileHighlight(
                spriteBatch,
                hoveredTile.Value,
                new Color(
                    r: 255,
                    g: 255,
                    b: 255,
                    alpha: 180));
        }
    }

    public void Dispose()
    {
        _diamondTexture.Dispose();
    }

    private void DrawTileHighlight(
        SpriteBatch spriteBatch,
        Point tile,
        Color color)
    {
        var drawPosition =
            _mapRenderer.GridToScreen(
                tile.X,
                tile.Y);

        spriteBatch.Draw(
            texture: _diamondTexture,
            position: drawPosition,
            color: color);
    }

    private static Texture2D CreateDiamondTexture(
        GraphicsDevice graphicsDevice,
        int width,
        int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(height));
        }

        var texture =
            new Texture2D(
                graphicsDevice,
                width,
                height);

        var pixels =
            new Color[width * height];

        var centerX =
            (width - 1) /
            2.0f;

        var centerY =
            (height - 1) /
            2.0f;

        var halfWidth =
            width /
            2.0f;

        var halfHeight =
            height /
            2.0f;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var normalizedDistance =
                    MathF.Abs(
                        (x - centerX) /
                        halfWidth) +
                    MathF.Abs(
                        (y - centerY) /
                        halfHeight);

                var pixelIndex =
                    (y * width) +
                    x;

                if (normalizedDistance > 1.0f)
                {
                    pixels[pixelIndex] =
                        Color.Transparent;

                    continue;
                }

                var isOutline =
                    normalizedDistance >= 0.82f;

                pixels[pixelIndex] =
                    isOutline
                        ? new Color(
                            r: 255,
                            g: 255,
                            b: 255,
                            alpha: 255)
                        : new Color(
                            r: 255,
                            g: 255,
                            b: 255,
                            alpha: 105);
            }
        }

        texture.SetData(
            pixels);

        return texture;
    }
}