using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TacticsGame.Battle;

namespace TacticsGame.Rendering;

/// <summary>
/// Draws battle units on the isometric map.
/// This starter version generates a temporary blue swordsman-like
/// placeholder sprite in memory so no external image is required.
/// </summary>
public sealed class BattleUnitRenderer : IDisposable
{
    private const int SpriteWidth = 24;
    private const int SpriteHeight = 40;

    private readonly IsometricMapRenderer _mapRenderer;
    private readonly Texture2D _placeholderTexture;

    private readonly int _tileWidth;
    private readonly int _tileHeight;

    public BattleUnitRenderer(
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

        _tileWidth =
            tileWidth;

        _tileHeight =
            tileHeight;

        _placeholderTexture =
            CreatePlaceholderTexture(
                graphicsDevice);
    }

    public void Draw(
        SpriteBatch spriteBatch,
        BattleUnit unit)
    {
        ArgumentNullException.ThrowIfNull(
            spriteBatch);

        ArgumentNullException.ThrowIfNull(
            unit);

        var drawPosition =
            GetUnitDrawPosition(
                unit);
        var tint = unit.Team == BattleTeam.Player
            ? Color.White
            : new Color(
                r: 255,
                g: 105,
                b: 105,
                alpha: 255);

        spriteBatch.Draw(
            texture: _placeholderTexture,
            position: drawPosition,
            color: tint);
    }

    /// <summary>
    /// Returns a point in world coordinates near the unit.
    /// Game1 converts this into screen coordinates and positions the
    /// command menu beside the unit.
    /// </summary>
    public Vector2 GetMenuAnchorWorld(
        BattleUnit unit)
    {
        ArgumentNullException.ThrowIfNull(
            unit);

        var tileDrawPosition =
            _mapRenderer.GridToScreen(
                unit.RenderGridPosition.X,
                unit.RenderGridPosition.Y);

        return
            new Vector2(
                x:
                    tileDrawPosition.X +
                    _tileWidth +
                    8,

                y:
                    tileDrawPosition.Y -
                    8);
    }
    public void Dispose()
    {
        _placeholderTexture.Dispose();
    }

    private Vector2 GetUnitDrawPosition(
    BattleUnit unit)
    {
        var tileDrawPosition =
            _mapRenderer.GridToScreen(
                unit.RenderGridPosition.X,
                unit.RenderGridPosition.Y);

        var tileCenterX =
            tileDrawPosition.X +
            (_tileWidth /
             2.0f);

        var tileCenterY =
            tileDrawPosition.Y +
            (_tileHeight /
             2.0f);

        return
            new Vector2(
                x:
                    tileCenterX -
                    (SpriteWidth /
                     2.0f),

                y:
                    tileCenterY -
                    SpriteHeight);
    }
    private static Texture2D CreatePlaceholderTexture(
        GraphicsDevice graphicsDevice)
    {
        var texture =
            new Texture2D(
                graphicsDevice,
                SpriteWidth,
                SpriteHeight);

        var pixels =
            new Color[
                SpriteWidth *
                SpriteHeight];

        Array.Fill(
            pixels,
            Color.Transparent);

        var outline =
            new Color(
                r: 20,
                g: 32,
                b: 54,
                alpha: 255);

        var skin =
            new Color(
                r: 218,
                g: 166,
                b: 120,
                alpha: 255);

        var tunic =
            new Color(
                r: 42,
                g: 91,
                b: 173,
                alpha: 255);

        var tunicHighlight =
            new Color(
                r: 65,
                g: 129,
                b: 216,
                alpha: 255);

        var boots =
            new Color(
                r: 75,
                g: 50,
                b: 38,
                alpha: 255);

        var sword =
            new Color(
                r: 205,
                g: 215,
                b: 224,
                alpha: 255);

        FillRectangle(
            pixels,
            x: 8,
            y: 3,
            width: 8,
            height: 8,
            color: outline);

        FillRectangle(
            pixels,
            x: 9,
            y: 4,
            width: 6,
            height: 6,
            color: skin);

        FillRectangle(
            pixels,
            x: 6,
            y: 12,
            width: 12,
            height: 15,
            color: outline);

        FillRectangle(
            pixels,
            x: 7,
            y: 13,
            width: 10,
            height: 13,
            color: tunic);

        FillRectangle(
            pixels,
            x: 11,
            y: 13,
            width: 3,
            height: 13,
            color: tunicHighlight);

        FillRectangle(
            pixels,
            x: 5,
            y: 14,
            width: 3,
            height: 11,
            color: skin);

        FillRectangle(
            pixels,
            x: 17,
            y: 14,
            width: 3,
            height: 11,
            color: skin);

        FillRectangle(
            pixels,
            x: 7,
            y: 27,
            width: 4,
            height: 10,
            color: boots);

        FillRectangle(
            pixels,
            x: 14,
            y: 27,
            width: 4,
            height: 10,
            color: boots);

        FillRectangle(
            pixels,
            x: 19,
            y: 8,
            width: 2,
            height: 21,
            color: sword);

        FillRectangle(
            pixels,
            x: 17,
            y: 26,
            width: 6,
            height: 2,
            color: outline);

        texture.SetData(
            pixels);

        return
            texture;
    }

    private static void FillRectangle(
        Color[] pixels,
        int x,
        int y,
        int width,
        int height,
        Color color)
    {
        for (var row = y;
             row < y + height;
             row++)
        {
            for (var column = x;
                 column < x + width;
                 column++)
            {
                if (column < 0 ||
                    row < 0 ||
                    column >= SpriteWidth ||
                    row >= SpriteHeight)
                {
                    continue;
                }

                pixels[
                    (row * SpriteWidth) +
                    column] =
                    color;
            }
        }
    }
}