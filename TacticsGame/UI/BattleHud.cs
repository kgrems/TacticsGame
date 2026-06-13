using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TacticsGame.Battle;

namespace TacticsGame.UI;

public sealed class BattleHud : IDisposable
{
    private readonly Texture2D _pixel;

    public BattleHud(GraphicsDevice graphicsDevice)
    {
        _pixel = new Texture2D(
            graphicsDevice,
            1,
            1);

        _pixel.SetData(new[]
        {
            Color.White
        });
    }

    public void Draw(
        SpriteBatch spriteBatch,
        SpriteFont font,
        BattleTurnController turnController,
        BattleUnit? selectedUnit,
        BattleUnit? hoveredUnit)
    {
        DrawTurnPanel(
            spriteBatch,
            font,
            turnController);

        if (selectedUnit != null)
        {
            DrawSelectedUnitPanel(
                spriteBatch,
                font,
                selectedUnit);
        }

        if (hoveredUnit != null)
        {
            DrawHoveredUnitPanel(
                spriteBatch,
                font,
                hoveredUnit);
        }
    }

    private void DrawTurnPanel(
        SpriteBatch spriteBatch,
        SpriteFont font,
        BattleTurnController turnController)
    {
        var rect =
            new Rectangle(
                10,
                10,
                220,
                96);

        DrawPanel(
            spriteBatch,
            rect);

        spriteBatch.DrawString(
            font,
            $"Round {turnController.RoundNumber}",
            new Vector2(20, 20),
            Color.White);

        spriteBatch.DrawString(
            font,
            $"{turnController.ActiveTeam} Turn",
            new Vector2(20, 45),
            Color.White);

        if (turnController.ActiveUnit is null)
        {
            return;
        }

        spriteBatch.DrawString(
            font,
            $"Command: {turnController.ActiveUnit.Name}",
            new Vector2(20, 70),
            new Color(232, 194, 103, 255));
    }

    private void DrawSelectedUnitPanel(
        SpriteBatch spriteBatch,
        SpriteFont font,
        BattleUnit unit)
    {
        var rect =
            new Rectangle(
                10,
                700,
                260,
                120);

        DrawPanel(
            spriteBatch,
            rect);

        spriteBatch.DrawString(
            font,
            unit.Name,
            new Vector2(20, 710),
            Color.White);

        spriteBatch.DrawString(
            font,
            $"HP {unit.CurrentHealth}/{unit.EffectiveMaximumHealth}",
            new Vector2(20, 740),
            Color.White);

        spriteBatch.DrawString(
            font,
            unit.TurnState.HasMoved
                ? "Move: Used"
                : "Move: Ready",
            new Vector2(20, 770),
            Color.White);

        spriteBatch.DrawString(
            font,
            unit.TurnState.HasActed
                ? "Attack: Used"
                : "Attack: Ready",
            new Vector2(20, 795),
            Color.White);
    }

    private void DrawHoveredUnitPanel(
        SpriteBatch spriteBatch,
        SpriteFont font,
        BattleUnit unit)
    {
        var rect =
            new Rectangle(
                1300,
                700,
                260,
                90);

        DrawPanel(
            spriteBatch,
            rect);

        spriteBatch.DrawString(
            font,
            unit.Name,
            new Vector2(1310, 710),
            Color.White);

        spriteBatch.DrawString(
            font,
            $"HP {unit.CurrentHealth}/{unit.EffectiveMaximumHealth}",
            new Vector2(1310, 740),
            Color.White);
    }

    private void DrawPanel(
        SpriteBatch spriteBatch,
        Rectangle rect)
    {
        spriteBatch.Draw(
            _pixel,
            rect,
            new Color(
                r: 20,
                g: 20,
                b: 30,
                alpha: 220));

        spriteBatch.Draw(
            _pixel,
            new Rectangle(
                rect.X,
                rect.Y,
                rect.Width,
                2),
            Color.White);

        spriteBatch.Draw(
            _pixel,
            new Rectangle(
                rect.X,
                rect.Bottom - 2,
                rect.Width,
                2),
            Color.White);

        spriteBatch.Draw(
            _pixel,
            new Rectangle(
                rect.X,
                rect.Y,
                2,
                rect.Height),
            Color.White);

        spriteBatch.Draw(
            _pixel,
            new Rectangle(
                rect.Right - 2,
                rect.Y,
                2,
                rect.Height),
            Color.White);
    }

    public void Dispose()
    {
        _pixel.Dispose();
    }
}
