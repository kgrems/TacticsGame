#nullable enable

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TacticsGame.Battle;
using TacticsGame.Campaign;

namespace TacticsGame.UI;

public sealed class VictoryScreen : IDisposable
{
    private const int PanelWidth = 720;
    private const int PanelHeight = 520;
    private const int ButtonWidth = 190;
    private const int ButtonHeight = 44;

    private readonly Texture2D _pixelTexture;

    private BattleReward? _reward;
    private IReadOnlyList<BattleUnit> _party =
        Array.Empty<BattleUnit>();

    private bool _isContinueHovered;

    public VictoryScreen(
        GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(
            graphicsDevice);

        _pixelTexture = new Texture2D(
            graphicsDevice,
            1,
            1);

        _pixelTexture.SetData(new[]
        {
            Color.White
        });
    }

    public void Show(
        BattleReward reward,
        IReadOnlyList<BattleUnit> party)
    {
        ArgumentNullException.ThrowIfNull(
            reward);

        ArgumentNullException.ThrowIfNull(
            party);

        _reward =
            reward;

        _party =
            party;

        _isContinueHovered =
            false;
    }

    public bool Update(
        Point mousePosition,
        bool isLeftMouseButtonPressed,
        bool isEnterPressed,
        Viewport viewport)
    {
        var button =
            GetContinueButtonRectangle(
                viewport);

        _isContinueHovered =
            button.Contains(
                mousePosition);

        return isEnterPressed ||
               (_isContinueHovered &&
                isLeftMouseButtonPressed);
    }

    public void Draw(
        SpriteBatch spriteBatch,
        SpriteFont font,
        Viewport viewport)
    {
        ArgumentNullException.ThrowIfNull(
            spriteBatch);

        ArgumentNullException.ThrowIfNull(
            font);

        DrawRectangle(
            spriteBatch,
            new Rectangle(
                0,
                0,
                viewport.Width,
                viewport.Height),
            new Color(10, 13, 20, 255));

        var panel =
            GetPanelRectangle(
                viewport);

        DrawRectangle(
            spriteBatch,
            panel,
            new Color(24, 28, 38, 245));

        DrawBorder(
            spriteBatch,
            panel,
            new Color(232, 194, 103, 255),
            thickness: 3);

        spriteBatch.DrawString(
            font,
            "Victory",
            new Vector2(
                panel.X + 28,
                panel.Y + 24),
            Color.White);

        DrawRewardSummary(
            spriteBatch,
            font,
            panel);

        DrawPartyProgress(
            spriteBatch,
            font,
            panel);

        DrawContinueButton(
            spriteBatch,
            font,
            viewport);
    }

    public void Dispose()
    {
        _pixelTexture.Dispose();
    }

    private void DrawRewardSummary(
        SpriteBatch spriteBatch,
        SpriteFont font,
        Rectangle panel)
    {
        var reward =
            _reward;

        if (reward is null)
        {
            return;
        }

        var y =
            panel.Y + 86;

        DrawLine(
            spriteBatch,
            font,
            $"Cleared: {reward.EncounterName}",
            panel.X + 32,
            y,
            Color.White);

        y += 34;

        DrawLine(
            spriteBatch,
            font,
            $"Party XP: +{reward.Experience}",
            panel.X + 32,
            y,
            new Color(202, 210, 224, 255));

        y += 34;

        DrawLine(
            spriteBatch,
            font,
            reward.Gear is null
                ? "Gear Found: None"
                : $"Gear Found: {reward.Gear.Name}",
            panel.X + 32,
            y,
            new Color(202, 210, 224, 255));

        y += 34;

        if (reward.FullyRecovered)
        {
            DrawLine(
                spriteBatch,
                font,
                "Party recovered",
                panel.X + 32,
                y,
                new Color(136, 215, 172, 255));
        }
    }

    private void DrawPartyProgress(
        SpriteBatch spriteBatch,
        SpriteFont font,
        Rectangle panel)
    {
        var y =
            panel.Y + 246;

        DrawLine(
            spriteBatch,
            font,
            "Party",
            panel.X + 32,
            y,
            Color.White);

        y += 34;

        foreach (var unit in _party)
        {
            DrawLine(
                spriteBatch,
                font,
                $"{unit.Name}  Lv {unit.Level}  XP {unit.ExperienceIntoLevel}/{BattleUnit.ExperiencePerLevel}",
                panel.X + 48,
                y,
                new Color(202, 210, 224, 255));

            y += 30;
        }
    }

    private void DrawContinueButton(
        SpriteBatch spriteBatch,
        SpriteFont font,
        Viewport viewport)
    {
        var rectangle =
            GetContinueButtonRectangle(
                viewport);

        DrawRectangle(
            spriteBatch,
            rectangle,
            _isContinueHovered
                ? new Color(74, 111, 176, 255)
                : new Color(48, 56, 73, 255));

        DrawBorder(
            spriteBatch,
            rectangle,
            _isContinueHovered
                ? new Color(232, 194, 103, 255)
                : new Color(196, 204, 218, 255),
            thickness: 2);

        var text =
            "World Map";

        var size =
            font.MeasureString(
                text);

        spriteBatch.DrawString(
            font,
            text,
            new Vector2(
                rectangle.X +
                ((rectangle.Width - size.X) / 2.0f),
                rectangle.Y +
                ((rectangle.Height - size.Y) / 2.0f)),
            Color.White);
    }

    private static Rectangle GetPanelRectangle(
        Viewport viewport)
    {
        return new Rectangle(
            (viewport.Width - PanelWidth) / 2,
            (viewport.Height - PanelHeight) / 2,
            PanelWidth,
            PanelHeight);
    }

    private static Rectangle GetContinueButtonRectangle(
        Viewport viewport)
    {
        var panel =
            GetPanelRectangle(
                viewport);

        return new Rectangle(
            panel.Right - ButtonWidth - 28,
            panel.Bottom - ButtonHeight - 28,
            ButtonWidth,
            ButtonHeight);
    }

    private static void DrawLine(
        SpriteBatch spriteBatch,
        SpriteFont font,
        string text,
        int x,
        int y,
        Color color)
    {
        spriteBatch.DrawString(
            font,
            text,
            new Vector2(
                x,
                y),
            color);
    }

    private void DrawRectangle(
        SpriteBatch spriteBatch,
        Rectangle rectangle,
        Color color)
    {
        spriteBatch.Draw(
            _pixelTexture,
            rectangle,
            color);
    }

    private void DrawBorder(
        SpriteBatch spriteBatch,
        Rectangle rectangle,
        Color color,
        int thickness)
    {
        DrawRectangle(
            spriteBatch,
            new Rectangle(
                rectangle.X,
                rectangle.Y,
                rectangle.Width,
                thickness),
            color);

        DrawRectangle(
            spriteBatch,
            new Rectangle(
                rectangle.X,
                rectangle.Bottom - thickness,
                rectangle.Width,
                thickness),
            color);

        DrawRectangle(
            spriteBatch,
            new Rectangle(
                rectangle.X,
                rectangle.Y,
                thickness,
                rectangle.Height),
            color);

        DrawRectangle(
            spriteBatch,
            new Rectangle(
                rectangle.Right - thickness,
                rectangle.Y,
                thickness,
                rectangle.Height),
            color);
    }
}
