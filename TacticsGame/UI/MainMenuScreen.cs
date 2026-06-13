#nullable enable

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace TacticsGame.UI;

public enum MainMenuAction
{
    None,
    Start,
    Instructions,
    Options,
    Quit
}

public sealed class MainMenuScreen : IDisposable
{
    private const int ButtonWidth = 260;
    private const int ButtonHeight = 48;
    private const int ButtonSpacing = 12;

    private static readonly MainMenuAction[] Actions =
    {
        MainMenuAction.Start,
        MainMenuAction.Instructions,
        MainMenuAction.Options,
        MainMenuAction.Quit
    };

    private readonly Texture2D _pixelTexture;

    private int? _hoveredButtonIndex;

    public MainMenuScreen(
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

    public MainMenuAction Update(
        Point mousePosition,
        bool isLeftMouseButtonPressed,
        Viewport viewport)
    {
        _hoveredButtonIndex = null;

        for (var index = 0; index < Actions.Length; index++)
        {
            var buttonRectangle =
                GetButtonRectangle(
                    viewport,
                    index);

            if (!buttonRectangle.Contains(mousePosition))
            {
                continue;
            }

            _hoveredButtonIndex = index;

            return isLeftMouseButtonPressed
                ? Actions[index]
                : MainMenuAction.None;
        }

        return MainMenuAction.None;
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

        var title = "Tactics Game";
        var titleSize =
            font.MeasureString(
                title);

        spriteBatch.DrawString(
            font,
            title,
            new Vector2(
                (viewport.Width - titleSize.X) / 2.0f,
                140),
            Color.White);

        for (var index = 0; index < Actions.Length; index++)
        {
            DrawButton(
                spriteBatch,
                font,
                viewport,
                index);
        }
    }

    public void Dispose()
    {
        _pixelTexture.Dispose();
    }

    private void DrawButton(
        SpriteBatch spriteBatch,
        SpriteFont font,
        Viewport viewport,
        int index)
    {
        var rectangle =
            GetButtonRectangle(
                viewport,
                index);

        var isHovered =
            _hoveredButtonIndex == index;

        DrawRectangle(
            spriteBatch,
            rectangle,
            isHovered
                ? new Color(74, 111, 176, 255)
                : new Color(48, 56, 73, 255));

        DrawBorder(
            spriteBatch,
            rectangle,
            isHovered
                ? new Color(232, 194, 103, 255)
                : new Color(196, 204, 218, 255));

        var text =
            GetActionLabel(
                Actions[index]);

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

    private static Rectangle GetButtonRectangle(
        Viewport viewport,
        int index)
    {
        var totalHeight =
            (Actions.Length * ButtonHeight) +
            ((Actions.Length - 1) * ButtonSpacing);

        return new Rectangle(
            (viewport.Width - ButtonWidth) / 2,
            280 +
            index * (ButtonHeight + ButtonSpacing) -
            totalHeight / 8,
            ButtonWidth,
            ButtonHeight);
    }

    private static string GetActionLabel(
        MainMenuAction action)
    {
        return action switch
        {
            MainMenuAction.Start => "Start",
            MainMenuAction.Instructions => "Instructions",
            MainMenuAction.Options => "Options",
            MainMenuAction.Quit => "Quit",
            _ => string.Empty
        };
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
        Color color)
    {
        const int thickness = 2;

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
