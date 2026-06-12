using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace TacticsGame.Rendering;

public sealed class AttackRangeRenderer : IDisposable
{
    private readonly IsometricMapRenderer _mapRenderer;
    private readonly Texture2D _diamondTexture;

    public AttackRangeRenderer(
        GraphicsDevice graphicsDevice,
        IsometricMapRenderer mapRenderer,
        int tileWidth,
        int tileHeight)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        _mapRenderer = mapRenderer ??
            throw new ArgumentNullException(nameof(mapRenderer));

        _diamondTexture = CreateDiamondTexture(
            graphicsDevice,
            tileWidth,
            tileHeight);
    }

    public void Draw(
        SpriteBatch spriteBatch,
        IEnumerable<Point> attackableTiles)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);
        ArgumentNullException.ThrowIfNull(attackableTiles);

        var attackColor = new Color(
            r: 214,
            g: 57,
            b: 57,
            alpha: 165);

        foreach (var tile in attackableTiles)
        {
            var drawPosition = _mapRenderer.GridToScreen(
                tile.X,
                tile.Y);

            spriteBatch.Draw(
                texture: _diamondTexture,
                position: drawPosition,
                color: attackColor);
        }
    }

    public void Dispose()
    {
        _diamondTexture.Dispose();
    }

    private static Texture2D CreateDiamondTexture(
        GraphicsDevice graphicsDevice,
        int width,
        int height)
    {
        var texture = new Texture2D(
            graphicsDevice,
            width,
            height);

        var pixels = new Color[width * height];

        var centerX = (width - 1) / 2.0f;
        var centerY = (height - 1) / 2.0f;
        var halfWidth = width / 2.0f;
        var halfHeight = height / 2.0f;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var normalizedDistance =
                    MathF.Abs((x - centerX) / halfWidth) +
                    MathF.Abs((y - centerY) / halfHeight);

                var pixelIndex = (y * width) + x;

                if (normalizedDistance > 1.0f)
                {
                    pixels[pixelIndex] = Color.Transparent;
                    continue;
                }

                var isOutline = normalizedDistance >= 0.84f;

                pixels[pixelIndex] = isOutline
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

        texture.SetData(pixels);

        return texture;
    }
}