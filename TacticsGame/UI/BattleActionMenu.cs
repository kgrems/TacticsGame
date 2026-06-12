using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TacticsGame.Battle;

namespace TacticsGame.UI;

public sealed class BattleActionMenu : IDisposable
{
    private const int MenuWidth = 160;
    private const int HeaderHeight = 34;
    private const int ButtonHeight = 34;
    private const int ButtonSpacing = 4;
    private const int OuterPadding = 8;

    private static readonly BattleAction[] Actions =
    {
        BattleAction.Move,
        BattleAction.Attack,
        BattleAction.Spells,
        BattleAction.Items,
        BattleAction.Wait
    };

    private readonly Texture2D _pixelTexture;

    private readonly HashSet<BattleAction> _disabledActions = new();

    private Vector2 _position;
    private int? _hoveredButtonIndex;

    public bool IsVisible { get; private set; }

    public string UnitName { get; private set; } = string.Empty;

    public BattleActionMenu(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        _pixelTexture = new Texture2D(
            graphicsDevice,
            width: 1,
            height: 1);

        _pixelTexture.SetData(new[]
        {
            Color.White
        });
    }

    public void Show(string unitName)
    {
        UnitName = unitName;
        IsVisible = true;
    }

    public void Hide()
    {
        UnitName = string.Empty;
        IsVisible = false;
        _hoveredButtonIndex = null;
        _disabledActions.Clear();
    }

    public void SetPosition(Vector2 position)
    {
        _position = position;
    }

    public void SetDisabledActions(
        IEnumerable<BattleAction> disabledActions)
    {
        ArgumentNullException.ThrowIfNull(disabledActions);

        _disabledActions.Clear();
        _disabledActions.UnionWith(disabledActions);
    }

    public bool Contains(Point mousePosition)
    {
        return IsVisible &&
               GetMenuRectangle().Contains(mousePosition);
    }

    public void Update(Point mousePosition)
    {
        _hoveredButtonIndex = null;

        if (!IsVisible)
        {
            return;
        }

        for (var index = 0; index < Actions.Length; index++)
        {
            if (!GetButtonRectangle(index).Contains(mousePosition))
            {
                continue;
            }

            _hoveredButtonIndex = index;
            return;
        }
    }

    public BattleAction? TrySelectAction(Point mousePosition)
    {
        if (!IsVisible)
        {
            return null;
        }

        for (var index = 0; index < Actions.Length; index++)
        {
            if (!GetButtonRectangle(index).Contains(mousePosition))
            {
                continue;
            }

            var action = Actions[index];

            return _disabledActions.Contains(action)
                ? null
                : action;
        }

        return null;
    }

    public void Draw(
        SpriteBatch spriteBatch,
        SpriteFont font)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);
        ArgumentNullException.ThrowIfNull(font);

        if (!IsVisible)
        {
            return;
        }

        var menuRectangle = GetMenuRectangle();

        DrawRectangle(
            spriteBatch,
            menuRectangle,
            new Color(
                r: 24,
                g: 28,
                b: 38,
                alpha: 235));

        DrawBorder(
            spriteBatch,
            menuRectangle,
            new Color(
                r: 196,
                g: 204,
                b: 218,
                alpha: 255));

        spriteBatch.DrawString(
            spriteFont: font,
            text: UnitName,
            position: new Vector2(
                menuRectangle.X + OuterPadding,
                menuRectangle.Y + 7),
            color: Color.White);

        for (var index = 0; index < Actions.Length; index++)
        {
            DrawButton(
                spriteBatch,
                font,
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
        int index)
    {
        var action = Actions[index];
        var buttonRectangle = GetButtonRectangle(index);

        var isHovered = _hoveredButtonIndex == index;
        var isDisabled = _disabledActions.Contains(action);

        var backgroundColor = isDisabled
            ? new Color(
                r: 58,
                g: 58,
                b: 64,
                alpha: 255)
            : isHovered
                ? new Color(
                    r: 74,
                    g: 111,
                    b: 176,
                    alpha: 255)
                : new Color(
                    r: 48,
                    g: 56,
                    b: 73,
                    alpha: 255);

        var textColor = isDisabled
            ? new Color(
                r: 145,
                g: 145,
                b: 152,
                alpha: 255)
            : Color.White;

        DrawRectangle(
            spriteBatch,
            buttonRectangle,
            backgroundColor);

        var text = action.ToString();
        var textSize = font.MeasureString(text);

        spriteBatch.DrawString(
            spriteFont: font,
            text: text,
            position: new Vector2(
                buttonRectangle.X + 10,
                buttonRectangle.Y +
                ((buttonRectangle.Height - textSize.Y) / 2.0f)),
            color: textColor);
    }

    private Rectangle GetMenuRectangle()
    {
        var totalButtonHeight =
            (Actions.Length * ButtonHeight) +
            ((Actions.Length - 1) * ButtonSpacing);

        return new Rectangle(
            x: (int)_position.X,
            y: (int)_position.Y,
            width: MenuWidth,
            height:
                HeaderHeight +
                totalButtonHeight +
                (OuterPadding * 2));
    }

    private Rectangle GetButtonRectangle(int index)
    {
        return new Rectangle(
            x: (int)_position.X + OuterPadding,
            y:
                (int)_position.Y +
                HeaderHeight +
                OuterPadding +
                (index * (ButtonHeight + ButtonSpacing)),
            width: MenuWidth - (OuterPadding * 2),
            height: ButtonHeight);
    }

    private void DrawRectangle(
        SpriteBatch spriteBatch,
        Rectangle rectangle,
        Color color)
    {
        spriteBatch.Draw(
            texture: _pixelTexture,
            destinationRectangle: rectangle,
            color: color);
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