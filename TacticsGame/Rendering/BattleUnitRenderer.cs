using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TacticsGame.Battle;

namespace TacticsGame.Rendering;

/// <summary>
/// Draws battle units on the isometric map.
/// </summary>
public sealed class BattleUnitRenderer : IDisposable
{
    private const int SpriteWidth = 24;
    private const int SpriteHeight = 40;
    private const int AnimatedFrameSize = 64;
    private const int IdleStartColumn = 0;
    private const int WalkStartColumn = 2;
    private const int AttackStartColumn = 6;
    private const int ProneStartColumn = 10;

    private readonly IsometricMapRenderer _mapRenderer;
    private readonly Texture2D _placeholderTexture;
    private readonly Texture2D? _batTexture;
    private readonly IReadOnlyDictionary<string, Texture2D> _unitTextures;
    private readonly IReadOnlyDictionary<string, Texture2D> _enemyTextures;

    private readonly int _tileWidth;
    private readonly int _tileHeight;

    public BattleUnitRenderer(
        GraphicsDevice graphicsDevice,
        IsometricMapRenderer mapRenderer,
        int tileWidth,
        int tileHeight,
        Texture2D? batTexture,
        IReadOnlyDictionary<string, Texture2D> unitTextures,
        IReadOnlyDictionary<string, Texture2D> enemyTextures)
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

        _batTexture =
            batTexture;

        _unitTextures =
            unitTextures ??
            throw new ArgumentNullException(
                nameof(unitTextures));

        _enemyTextures =
            enemyTextures ??
            throw new ArgumentNullException(
                nameof(enemyTextures));

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

        if (unit.Team == BattleTeam.Enemy)
        {
            if (_enemyTextures.TryGetValue(
                    unit.SpriteKey,
                    out var enemyTexture))
            {
                DrawAnimatedUnit(
                    spriteBatch,
                    unit,
                    enemyTexture);

                return;
            }

            if (_batTexture is not null)
            {
                DrawAnimatedUnit(
                    spriteBatch,
                    unit,
                    _batTexture);

                return;
            }

        }

        if (unit.Team == BattleTeam.Player &&
            _unitTextures.TryGetValue(
                unit.JobName,
                out var unitTexture))
        {
            DrawAnimatedUnit(
                spriteBatch,
                unit,
                unitTexture);

            return;
        }

        var drawPosition =
            GetUnitDrawPosition(
                unit);
        spriteBatch.Draw(
            texture: _placeholderTexture,
            position: drawPosition,
            color:
                unit.IsDefeated
                    ? new Color(110, 110, 120, 255)
                    : Color.White);
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

    private void DrawAnimatedUnit(
        SpriteBatch spriteBatch,
        BattleUnit unit,
        Texture2D texture)
    {
        var sourceRectangle =
            GetAnimatedSourceRectangle(
                unit);

        var tileDrawPosition =
            _mapRenderer.GridToScreen(
                unit.RenderGridPosition.X,
                unit.RenderGridPosition.Y);

        var tileCenter =
            new Vector2(
                tileDrawPosition.X +
                (_tileWidth / 2.0f),
                tileDrawPosition.Y +
                (_tileHeight / 2.0f));

        var drawPosition =
            new Vector2(
                tileCenter.X -
                (AnimatedFrameSize / 2.0f),
                tileCenter.Y -
                AnimatedFrameSize +
                10);

        spriteBatch.Draw(
            texture: texture,
            position: drawPosition,
            sourceRectangle: sourceRectangle,
            color: Color.White);
    }

    private static Rectangle GetAnimatedSourceRectangle(
        BattleUnit unit)
    {
        var directionRow =
            unit.Facing switch
            {
                UnitFacing.FrontLeft => 0,
                UnitFacing.FrontRight => 1,
                UnitFacing.BackLeft => 2,
                UnitFacing.BackRight => 3,
                _ => 1
            };

        var animationState =
            unit.IsDefeated
                ? BattleUnitAnimationState.Prone
                : unit.AnimationState;

        var (startColumn, frameCount, frameDuration, shouldLoop) =
            animationState switch
            {
                BattleUnitAnimationState.Walk =>
                    (WalkStartColumn, 4, 0.12f, true),

                BattleUnitAnimationState.Attack =>
                    (AttackStartColumn, 4, 0.10f, false),

                BattleUnitAnimationState.Prone =>
                    (ProneStartColumn, 2, 0.55f, true),

                _ =>
                    (IdleStartColumn, 2, 0.38f, true)
            };

        var frameIndex =
            (int)(unit.AnimationElapsedSeconds /
                  frameDuration);

        if (shouldLoop)
        {
            frameIndex %= frameCount;
        }
        else
        {
            frameIndex =
                Math.Min(
                    frameIndex,
                    frameCount - 1);
        }

        return new Rectangle(
            (startColumn + frameIndex) *
            AnimatedFrameSize,
            directionRow *
            AnimatedFrameSize,
            AnimatedFrameSize,
            AnimatedFrameSize);
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
