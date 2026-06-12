using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace TacticsGame.Rendering;

/// <summary>
/// Draws blue tile overlays for valid movement destinations and a
/// brighter gold overlay for the currently previewed movement path.
/// </summary>
public sealed class MovementRangeRenderer : IDisposable
{
    private readonly IsometricMapRenderer _mapRenderer;
    private readonly Texture2D _diamondTexture;

    public MovementRangeRenderer(
        GraphicsDevice graphicsDevice,
        IsometricMapRenderer mapRenderer,
        int tileWidth,
        int tileHeight)
    {
        ArgumentNullException.ThrowIfNull(
            graphicsDevice);

        _mapRenderer =
            mapRenderer ??
            throw new ArgumentNullException(
                nameof(mapRenderer));

        _diamondTexture =
            CreateDiamondTexture(
                graphicsDevice,
                tileWidth,
                tileHeight);
    }

    public void Draw(
        SpriteBatch spriteBatch,
        IEnumerable<Point> reachableTiles,
        IEnumerable<Point> previewPath)
    {
        ArgumentNullException.ThrowIfNull(
            spriteBatch);

        ArgumentNullException.ThrowIfNull(
            reachableTiles);

        ArgumentNullException.ThrowIfNull(
            previewPath);

        DrawReachableTiles(
            spriteBatch,
            reachableTiles);

        DrawPreviewPath(
            spriteBatch,
            previewPath);
    }

    public void Dispose()
    {
        _diamondTexture.Dispose();
    }

    private void DrawReachableTiles(
        SpriteBatch spriteBatch,
        IEnumerable<Point> reachableTiles)
    {
        var movementColor =
            new Color(
                r: 52,
                g: 137,
                b: 235,
                alpha: 125);

        foreach (var tile
                 in reachableTiles)
        {
            DrawTileOverlay(
                spriteBatch,
                tile,
                movementColor);
        }
    }

    private void DrawPreviewPath(
        SpriteBatch spriteBatch,
        IEnumerable<Point> previewPath)
    {
        var pathColor =
            new Color(
                r: 255,
                g: 188,
                b: 56,
                alpha: 215);

        foreach (var tile
                 in previewPath)
        {
            DrawTileOverlay(
                spriteBatch,
                tile,
                pathColor);
        }
    }

    private void DrawTileOverlay(
        SpriteBatch spriteBatch,
        Point tile,
        Color color)
    {
        var drawPosition =
            _mapRenderer.GridToScreen(
                tile.X,
                tile.Y);

        spriteBatch.Draw(
            texture:
                _diamondTexture,

            position:
                drawPosition,

            color:
                color);
    }

    private static Texture2D CreateDiamondTexture(
        GraphicsDevice graphicsDevice,
        int width,
        int height)
    {
        var texture =
            new Texture2D(
                graphicsDevice,
                width,
                height);

        var pixels =
            new Color[
                width *
                height];

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

        for (var y = 0;
             y < height;
             y++)
        {
            for (var x = 0;
                 x < width;
                 x++)
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

                if (normalizedDistance >
                    1.0f)
                {
                    pixels[
                        pixelIndex] =
                        Color.Transparent;

                    continue;
                }

                var isOutline =
                    normalizedDistance >=
                    0.84f;

                pixels[
                    pixelIndex] =
                    isOutline
                        ? new Color(
                            r: 255,
                            g: 255,
                            b: 255,
                            alpha: 245)

                        : new Color(
                            r: 255,
                            g: 255,
                            b: 255,
                            alpha: 120);
            }
        }

        texture.SetData(
            pixels);

        return
            texture;
    }
}