#nullable enable

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace TacticsGame.UI;

public sealed class TextMenuScreen : IDisposable
{
    private const int OuterPadding = 40;
    private const int BackButtonWidth = 140;
    private const int BackButtonHeight = 42;

    private readonly Texture2D _pixelTexture;
    private readonly string _title;
    private readonly IReadOnlyList<string> _lines;

    private bool _isBackHovered;

    public TextMenuScreen(
        GraphicsDevice graphicsDevice,
        string title,
        IReadOnlyList<string> lines)
    {
        ArgumentNullException.ThrowIfNull(
            graphicsDevice);

        _title =
            title;

        _lines =
            lines;

        _pixelTexture = new Texture2D(
            graphicsDevice,
            1,
            1);

        _pixelTexture.SetData(new[]
        {
            Color.White
        });
    }

    public bool Update(
        Point mousePosition,
        bool isLeftMouseButtonPressed,
        Viewport viewport)
    {
        var backButton =
            GetBackButtonRectangle(
                viewport);

        _isBackHovered =
            backButton.Contains(
                mousePosition);

        return _isBackHovered &&
               isLeftMouseButtonPressed;
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
            new Color(13, 17, 26, 255));

        var panel =
            new Rectangle(
                OuterPadding,
                OuterPadding,
                viewport.Width - OuterPadding * 2,
                viewport.Height - OuterPadding * 2);

        DrawPanel(
            spriteBatch,
            panel);

        spriteBatch.DrawString(
            font,
            _title,
            new Vector2(
                panel.X + 24,
                panel.Y + 22),
            Color.White);

        var y =
            panel.Y + 76;

        foreach (var line in _lines)
        {
            spriteBatch.DrawString(
                font,
                line,
                new Vector2(
                    panel.X + 24,
                    y),
                new Color(202, 210, 224, 255));

            y += 30;
        }

        DrawBackButton(
            spriteBatch,
            font,
            viewport);
    }

    public void Dispose()
    {
        _pixelTexture.Dispose();
    }

    private void DrawBackButton(
        SpriteBatch spriteBatch,
        SpriteFont font,
        Viewport viewport)
    {
        var rectangle =
            GetBackButtonRectangle(
                viewport);

        DrawRectangle(
            spriteBatch,
            rectangle,
            _isBackHovered
                ? new Color(74, 111, 176, 255)
                : new Color(48, 56, 73, 255));

        DrawBorder(
            spriteBatch,
            rectangle,
            _isBackHovered
                ? new Color(232, 194, 103, 255)
                : new Color(196, 204, 218, 255),
            thickness: 2);

        var text = "Back";
        var textSize =
            font.MeasureString(
                text);

        spriteBatch.DrawString(
            font,
            text,
            new Vector2(
                rectangle.X +
                ((rectangle.Width - textSize.X) / 2.0f),
                rectangle.Y +
                ((rectangle.Height - textSize.Y) / 2.0f)),
            Color.White);
    }

    private static Rectangle GetBackButtonRectangle(
        Viewport viewport)
    {
        return new Rectangle(
            viewport.Width - OuterPadding - BackButtonWidth,
            viewport.Height - OuterPadding - BackButtonHeight,
            BackButtonWidth,
            BackButtonHeight);
    }

    private void DrawPanel(
        SpriteBatch spriteBatch,
        Rectangle rectangle)
    {
        DrawRectangle(
            spriteBatch,
            rectangle,
            new Color(24, 28, 38, 245));

        DrawBorder(
            spriteBatch,
            rectangle,
            new Color(196, 204, 218, 255),
            thickness: 2);
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
