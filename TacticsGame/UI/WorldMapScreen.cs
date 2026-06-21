#nullable enable

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using TacticsGame.Campaign;

namespace TacticsGame.UI;

public enum WorldMapActionType
{
    None,
    Move,
    StartBattle,
    OpenParty,
    MainMenu
}

public readonly record struct WorldMapAction(
    WorldMapActionType Type,
    int NodeId)
{
    public static WorldMapAction None { get; } =
        new(
            WorldMapActionType.None,
            NodeId: -1);
}

public sealed class WorldMapScreen : IDisposable
{
    private const int NodeWidth = 154;
    private const int NodeHeight = 66;
    private const int ButtonWidth = 128;
    private const int ButtonHeight = 38;
    private const int OuterPadding = 28;

    private readonly Texture2D _pixelTexture;

    private int? _hoveredNodeId;
    private bool _isPartyHovered;
    private bool _isMainMenuHovered;

    public WorldMapScreen(
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

    public WorldMapAction Update(
        IReadOnlyList<WorldMapNode> nodes,
        int currentNodeId,
        IReadOnlyCollection<int> completedBattleNodeIds,
        Point mousePosition,
        bool isLeftMouseButtonPressed,
        Viewport viewport)
    {
        ArgumentNullException.ThrowIfNull(
            nodes);

        ArgumentNullException.ThrowIfNull(
            completedBattleNodeIds);

        _hoveredNodeId = null;

        var partyButton =
            GetPartyButtonRectangle(
                viewport);

        var mainMenuButton =
            GetMainMenuButtonRectangle(
                viewport);

        _isPartyHovered =
            partyButton.Contains(
                mousePosition);

        _isMainMenuHovered =
            mainMenuButton.Contains(
                mousePosition);

        if (isLeftMouseButtonPressed &&
            _isPartyHovered)
        {
            return new WorldMapAction(
                WorldMapActionType.OpenParty,
                currentNodeId);
        }

        if (isLeftMouseButtonPressed &&
            _isMainMenuHovered)
        {
            return new WorldMapAction(
                WorldMapActionType.MainMenu,
                currentNodeId);
        }

        var currentNode =
            nodes.FirstOrDefault(node =>
                node.Id == currentNodeId);

        if (currentNode is null)
        {
            return WorldMapAction.None;
        }

        foreach (var node in nodes)
        {
            var rectangle =
                GetNodeRectangle(
                    viewport,
                    node);

            if (!rectangle.Contains(
                    mousePosition))
            {
                continue;
            }

            _hoveredNodeId =
                node.Id;

            if (!isLeftMouseButtonPressed ||
                node.Id == currentNodeId ||
                !currentNode.ConnectedNodeIds.Contains(
                    node.Id))
            {
                return WorldMapAction.None;
            }

            if (node.Type == WorldMapNodeType.Battle &&
                !completedBattleNodeIds.Contains(
                    node.Id))
            {
                return new WorldMapAction(
                    WorldMapActionType.StartBattle,
                    node.Id);
            }

            return new WorldMapAction(
                WorldMapActionType.Move,
                node.Id);
        }

        return WorldMapAction.None;
    }

    public void Draw(
        SpriteBatch spriteBatch,
        SpriteFont font,
        IReadOnlyList<WorldMapNode> nodes,
        int currentNodeId,
        IReadOnlyCollection<int> completedBattleNodeIds,
        Viewport viewport)
    {
        ArgumentNullException.ThrowIfNull(
            spriteBatch);

        ArgumentNullException.ThrowIfNull(
            font);

        ArgumentNullException.ThrowIfNull(
            nodes);

        ArgumentNullException.ThrowIfNull(
            completedBattleNodeIds);

        DrawRectangle(
            spriteBatch,
            new Rectangle(
                0,
                0,
                viewport.Width,
                viewport.Height),
            new Color(11, 16, 24, 255));

        DrawHeader(
            spriteBatch,
            font,
            viewport);

        DrawConnections(
            spriteBatch,
            nodes,
            currentNodeId,
            completedBattleNodeIds,
            viewport);

        foreach (var node in nodes)
        {
            DrawNode(
                spriteBatch,
                font,
                nodes,
                node,
                currentNodeId,
                completedBattleNodeIds,
                viewport);
        }

        DrawStatusPanel(
            spriteBatch,
            font,
            nodes,
            currentNodeId,
            completedBattleNodeIds,
            viewport);
    }

    public void Dispose()
    {
        _pixelTexture.Dispose();
    }

    private void DrawHeader(
        SpriteBatch spriteBatch,
        SpriteFont font,
        Viewport viewport)
    {
        spriteBatch.DrawString(
            font,
            "World Map",
            new Vector2(
                OuterPadding,
                22),
            Color.White);

        DrawButton(
            spriteBatch,
            font,
            GetPartyButtonRectangle(
                viewport),
            "Party",
            _isPartyHovered);

        DrawButton(
            spriteBatch,
            font,
            GetMainMenuButtonRectangle(
                viewport),
            "Menu",
            _isMainMenuHovered);
    }

    private void DrawConnections(
        SpriteBatch spriteBatch,
        IReadOnlyList<WorldMapNode> nodes,
        int currentNodeId,
        IReadOnlyCollection<int> completedBattleNodeIds,
        Viewport viewport)
    {
        foreach (var node in nodes)
        {
            foreach (var connectedNodeId in node.ConnectedNodeIds)
            {
                if (connectedNodeId < node.Id)
                {
                    continue;
                }

                var connectedNode =
                    nodes.FirstOrDefault(candidate =>
                        candidate.Id == connectedNodeId);

                if (connectedNode is null)
                {
                    continue;
                }

                var isReachable =
                    node.Id == currentNodeId ||
                    connectedNode.Id == currentNodeId;

                var color =
                    isReachable
                        ? new Color(143, 168, 211, 255)
                        : new Color(54, 64, 84, 255);

                DrawLine(
                    spriteBatch,
                    GetNodeCenter(
                        viewport,
                        node),
                    GetNodeCenter(
                        viewport,
                        connectedNode),
                    color,
                    thickness: isReachable ? 4 : 2);
            }
        }
    }

    private void DrawNode(
        SpriteBatch spriteBatch,
        SpriteFont font,
        IReadOnlyList<WorldMapNode> nodes,
        WorldMapNode node,
        int currentNodeId,
        IReadOnlyCollection<int> completedBattleNodeIds,
        Viewport viewport)
    {
        var rectangle =
            GetNodeRectangle(
                viewport,
                node);

        var currentNode =
            nodes.FirstOrDefault(candidate =>
                candidate.Id == currentNodeId);

        var isCurrent =
            node.Id == currentNodeId;

        var isReachable =
            currentNode?.ConnectedNodeIds.Contains(
                node.Id) == true;

        var isCleared =
            completedBattleNodeIds.Contains(
                node.Id);

        var backgroundColor =
            isCurrent
                ? new Color(87, 79, 42, 255)
                : isCleared
                    ? new Color(43, 83, 69, 255)
                    : node.Type == WorldMapNodeType.Battle
                        ? new Color(86, 48, 56, 255)
                        : new Color(38, 49, 68, 255);

        if (_hoveredNodeId == node.Id &&
            (isCurrent || isReachable))
        {
            backgroundColor =
                new Color(74, 111, 176, 255);
        }

        DrawRectangle(
            spriteBatch,
            rectangle,
            backgroundColor);

        DrawBorder(
            spriteBatch,
            rectangle,
            isCurrent
                ? new Color(232, 194, 103, 255)
                : isReachable
                    ? new Color(196, 204, 218, 255)
                    : new Color(76, 84, 103, 255),
            thickness: isCurrent ? 3 : 2);

        DrawCenteredString(
            spriteBatch,
            font,
            node.Name,
            new Rectangle(
                rectangle.X,
                rectangle.Y + 10,
                rectangle.Width,
                22),
            Color.White);

        var status =
            isCurrent
                ? "Current"
                : isCleared
                    ? "Cleared"
                    : node.Type == WorldMapNodeType.Battle
                        ? "Battle"
                        : "Empty";

        DrawCenteredString(
            spriteBatch,
            font,
            status,
            new Rectangle(
                rectangle.X,
                rectangle.Y + 36,
                rectangle.Width,
                22),
            isReachable || isCurrent
                ? new Color(232, 194, 103, 255)
                : new Color(172, 181, 198, 255));
    }

    private void DrawStatusPanel(
        SpriteBatch spriteBatch,
        SpriteFont font,
        IReadOnlyList<WorldMapNode> nodes,
        int currentNodeId,
        IReadOnlyCollection<int> completedBattleNodeIds,
        Viewport viewport)
    {
        var rectangle =
            new Rectangle(
                OuterPadding,
                viewport.Height - 126,
                viewport.Width - OuterPadding * 2,
                94);

        DrawRectangle(
            spriteBatch,
            rectangle,
            new Color(24, 28, 38, 238));

        DrawBorder(
            spriteBatch,
            rectangle,
            new Color(196, 204, 218, 255),
            thickness: 2);

        var node =
            nodes.FirstOrDefault(candidate =>
                candidate.Id == currentNodeId);

        if (node is null)
        {
            return;
        }

        spriteBatch.DrawString(
            font,
            node.Name,
            new Vector2(
                rectangle.X + 18,
                rectangle.Y + 14),
            Color.White);

        var state =
            node.Type == WorldMapNodeType.Battle &&
            completedBattleNodeIds.Contains(
                node.Id)
                ? "Area secured"
                : node.Type == WorldMapNodeType.Battle
                    ? "Hostile area"
                    : "Quiet area";

        spriteBatch.DrawString(
            font,
            $"{state} - {node.Description}",
            new Vector2(
                rectangle.X + 18,
                rectangle.Y + 48),
            new Color(202, 210, 224, 255));
    }

    private static Rectangle GetPartyButtonRectangle(
        Viewport viewport)
    {
        return new Rectangle(
            viewport.Width - OuterPadding - (ButtonWidth * 2) - 12,
            18,
            ButtonWidth,
            ButtonHeight);
    }

    private static Rectangle GetMainMenuButtonRectangle(
        Viewport viewport)
    {
        return new Rectangle(
            viewport.Width - OuterPadding - ButtonWidth,
            18,
            ButtonWidth,
            ButtonHeight);
    }

    private static Rectangle GetNodeRectangle(
        Viewport viewport,
        WorldMapNode node)
    {
        var center =
            GetNodeCenter(
                viewport,
                node);

        return new Rectangle(
            (int)MathF.Round(
                center.X - (NodeWidth / 2.0f)),
            (int)MathF.Round(
                center.Y - (NodeHeight / 2.0f)),
            NodeWidth,
            NodeHeight);
    }

    private static Vector2 GetNodeCenter(
        Viewport viewport,
        WorldMapNode node)
    {
        return new Vector2(
            x:
                120.0f +
                (node.NormalizedPosition.X *
                 Math.Max(
                     1,
                     viewport.Width - 240)),

            y:
                116.0f +
                (node.NormalizedPosition.Y *
                 Math.Max(
                     1,
                     viewport.Height - 280)));
    }

    private void DrawButton(
        SpriteBatch spriteBatch,
        SpriteFont font,
        Rectangle rectangle,
        string text,
        bool isHovered)
    {
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
                : new Color(196, 204, 218, 255),
            thickness: 2);

        DrawCenteredString(
            spriteBatch,
            font,
            text,
            rectangle,
            Color.White);
    }

    private void DrawLine(
        SpriteBatch spriteBatch,
        Vector2 start,
        Vector2 end,
        Color color,
        int thickness)
    {
        var delta =
            end -
            start;

        var length =
            delta.Length();

        if (length <= 0.0f)
        {
            return;
        }

        spriteBatch.Draw(
            _pixelTexture,
            position: start,
            sourceRectangle: null,
            color: color,
            rotation:
                MathF.Atan2(
                    delta.Y,
                    delta.X),
            origin:
                new Vector2(
                    0.0f,
                    0.5f),
            scale:
                new Vector2(
                    length,
                    thickness),
            effects:
                SpriteEffects.None,
            layerDepth: 0.0f);
    }

    private void DrawCenteredString(
        SpriteBatch spriteBatch,
        SpriteFont font,
        string text,
        Rectangle rectangle,
        Color color)
    {
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
