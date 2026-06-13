#nullable enable

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using TacticsGame.Battle;

namespace TacticsGame.UI;

public enum TeamSelectionAction
{
    None,
    Start,
    Back
}

public sealed class TeamMemberSelection
{
    public string Name { get; init; } = string.Empty;

    public UnitJobDefinition Job { get; init; } = new();
}

public sealed class TeamSelectionScreen : IDisposable
{
    private const int OuterPadding = 28;
    private const int PanelSpacing = 18;
    private const int LeftPanelWidth = 430;
    private const int MemberRowHeight = 112;
    private const int JobButtonHeight = 76;
    private const int ButtonHeight = 42;
    private const int NameLimit = 12;

    private readonly Texture2D _pixelTexture;
    private readonly IReadOnlyList<UnitJobDefinition> _jobs;
    private readonly List<MemberDraft> _members = new();

    private int _selectedMemberIndex;
    private int? _activeNameIndex;
    private int? _hoveredMemberIndex;
    private int? _hoveredJobIndex;
    private bool _isBackHovered;
    private bool _isStartHovered;

    public TeamSelectionScreen(
        GraphicsDevice graphicsDevice,
        IReadOnlyList<UnitJobDefinition> jobs,
        int partySize)
    {
        ArgumentNullException.ThrowIfNull(
            graphicsDevice);

        ArgumentNullException.ThrowIfNull(
            jobs);

        if (jobs.Count == 0)
        {
            throw new ArgumentException(
                "At least one job is required.",
                nameof(jobs));
        }

        if (partySize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(partySize));
        }

        _jobs =
            jobs;

        _pixelTexture = new Texture2D(
            graphicsDevice,
            1,
            1);

        _pixelTexture.SetData(new[]
        {
            Color.White
        });

        ResetDrafts(
            partySize);
    }

    public TeamSelectionAction Update(
        Point mousePosition,
        bool isLeftMouseButtonPressed,
        bool isBackspacePressed,
        Viewport viewport)
    {
        _hoveredMemberIndex = null;
        _hoveredJobIndex = null;

        var backButton =
            GetBackButtonRectangle(
                viewport);

        var startButton =
            GetStartButtonRectangle(
                viewport);

        _isBackHovered =
            backButton.Contains(
                mousePosition);

        _isStartHovered =
            startButton.Contains(
                mousePosition);

        if (isBackspacePressed)
        {
            RemoveLastNameCharacter();
        }

        if (isLeftMouseButtonPressed &&
            _isBackHovered)
        {
            return TeamSelectionAction.Back;
        }

        if (isLeftMouseButtonPressed &&
            _isStartHovered &&
            CanStart())
        {
            return TeamSelectionAction.Start;
        }

        UpdateMemberRows(
            mousePosition,
            isLeftMouseButtonPressed,
            viewport);

        UpdateJobButtons(
            mousePosition,
            isLeftMouseButtonPressed,
            viewport);

        return TeamSelectionAction.None;
    }

    public void HandleTextInput(
        char character)
    {
        if (!_activeNameIndex.HasValue ||
            char.IsControl(character))
        {
            return;
        }

        if (!char.IsLetterOrDigit(character) &&
            character != ' ' &&
            character != '-' &&
            character != '\'')
        {
            return;
        }

        var member =
            _members[_activeNameIndex.Value];

        if (member.Name.Length >= NameLimit)
        {
            return;
        }

        member.Name +=
            character;
    }

    public IReadOnlyList<TeamMemberSelection> GetSelections()
    {
        return _members
            .Select((member, index) =>
                new TeamMemberSelection
                {
                    Name = string.IsNullOrWhiteSpace(
                        member.Name)
                        ? $"Hero {index + 1}"
                        : member.Name.Trim(),
                    Job = _jobs[member.JobIndex]
                })
            .ToList();
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

        spriteBatch.DrawString(
            font,
            "Assemble Your Team",
            new Vector2(
                OuterPadding,
                18),
            Color.White);

        DrawMemberPanel(
            spriteBatch,
            font,
            viewport);

        DrawJobPanel(
            spriteBatch,
            font,
            viewport);

        DrawFooterButtons(
            spriteBatch,
            font,
            viewport);
    }

    public void Dispose()
    {
        _pixelTexture.Dispose();
    }

    private void ResetDrafts(
        int partySize)
    {
        var defaultNames =
            new[]
            {
                "Alden",
                "Bryn",
                "Cora"
            };

        _members.Clear();

        for (var index = 0; index < partySize; index++)
        {
            _members.Add(
                new MemberDraft
                {
                    Name =
                        index < defaultNames.Length
                            ? defaultNames[index]
                            : $"Hero {index + 1}",
                    JobIndex =
                        index % _jobs.Count
                });
        }
    }

    private void UpdateMemberRows(
        Point mousePosition,
        bool isLeftMouseButtonPressed,
        Viewport viewport)
    {
        for (var index = 0; index < _members.Count; index++)
        {
            var row =
                GetMemberRowRectangle(
                    viewport,
                    index);

            if (!row.Contains(mousePosition))
            {
                continue;
            }

            _hoveredMemberIndex = index;

            if (!isLeftMouseButtonPressed)
            {
                return;
            }

            _selectedMemberIndex = index;

            var nameField =
                GetNameFieldRectangle(
                    row);

            _activeNameIndex =
                nameField.Contains(mousePosition)
                    ? index
                    : null;

            return;
        }
    }

    private void UpdateJobButtons(
        Point mousePosition,
        bool isLeftMouseButtonPressed,
        Viewport viewport)
    {
        for (var index = 0; index < _jobs.Count; index++)
        {
            var button =
                GetJobButtonRectangle(
                    viewport,
                    index);

            if (!button.Contains(mousePosition))
            {
                continue;
            }

            _hoveredJobIndex = index;

            if (isLeftMouseButtonPressed)
            {
                _members[_selectedMemberIndex].JobIndex =
                    index;
            }

            return;
        }
    }

    private void RemoveLastNameCharacter()
    {
        if (!_activeNameIndex.HasValue)
        {
            return;
        }

        var member =
            _members[_activeNameIndex.Value];

        if (member.Name.Length == 0)
        {
            return;
        }

        member.Name =
            member.Name[..^1];
    }

    private bool CanStart()
    {
        return _members.All(member =>
            !string.IsNullOrWhiteSpace(
                member.Name));
    }

    private void DrawMemberPanel(
        SpriteBatch spriteBatch,
        SpriteFont font,
        Viewport viewport)
    {
        var panel =
            GetMemberPanelRectangle(
                viewport);

        DrawPanel(
            spriteBatch,
            panel);

        spriteBatch.DrawString(
            font,
            "Party",
            new Vector2(
                panel.X + 16,
                panel.Y + 14),
            Color.White);

        for (var index = 0; index < _members.Count; index++)
        {
            DrawMemberRow(
                spriteBatch,
                font,
                viewport,
                index);
        }
    }

    private void DrawMemberRow(
        SpriteBatch spriteBatch,
        SpriteFont font,
        Viewport viewport,
        int index)
    {
        var member =
            _members[index];

        var row =
            GetMemberRowRectangle(
                viewport,
                index);

        var isSelected =
            _selectedMemberIndex == index;

        DrawRectangle(
            spriteBatch,
            row,
            isSelected
                ? new Color(55, 81, 125, 255)
                : _hoveredMemberIndex == index
                    ? new Color(48, 56, 73, 255)
                    : new Color(31, 36, 49, 255));

        DrawBorder(
            spriteBatch,
            row,
            isSelected
                ? new Color(232, 194, 103, 255)
                : new Color(70, 80, 102, 255),
            thickness: 1);

        spriteBatch.DrawString(
            font,
            $"Member {index + 1}",
            new Vector2(
                row.X + 12,
                row.Y + 10),
            Color.White);

        var nameField =
            GetNameFieldRectangle(
                row);

        DrawRectangle(
            spriteBatch,
            nameField,
            _activeNameIndex == index
                ? new Color(18, 23, 34, 255)
                : new Color(24, 28, 38, 255));

        DrawBorder(
            spriteBatch,
            nameField,
            _activeNameIndex == index
                ? new Color(232, 194, 103, 255)
                : new Color(70, 80, 102, 255),
            thickness: 1);

        spriteBatch.DrawString(
            font,
            member.Name,
            new Vector2(
                nameField.X + 8,
                nameField.Y + 7),
            Color.White);

        var job =
            _jobs[member.JobIndex];

        spriteBatch.DrawString(
            font,
            job.Name,
            new Vector2(
                row.X + 12,
                row.Y + 78),
            new Color(202, 210, 224, 255));
    }

    private void DrawJobPanel(
        SpriteBatch spriteBatch,
        SpriteFont font,
        Viewport viewport)
    {
        var panel =
            GetJobPanelRectangle(
                viewport);

        DrawPanel(
            spriteBatch,
            panel);

        var selectedMember =
            _members[_selectedMemberIndex];

        spriteBatch.DrawString(
            font,
            $"Jobs for {selectedMember.Name}",
            new Vector2(
                panel.X + 16,
                panel.Y + 14),
            Color.White);

        for (var index = 0; index < _jobs.Count; index++)
        {
            DrawJobButton(
                spriteBatch,
                font,
                viewport,
                index);
        }
    }

    private void DrawJobButton(
        SpriteBatch spriteBatch,
        SpriteFont font,
        Viewport viewport,
        int index)
    {
        var rectangle =
            GetJobButtonRectangle(
                viewport,
                index);

        var selectedMember =
            _members[_selectedMemberIndex];

        var isSelected =
            selectedMember.JobIndex == index;

        var isHovered =
            _hoveredJobIndex == index;

        DrawRectangle(
            spriteBatch,
            rectangle,
            isSelected
                ? new Color(55, 81, 125, 255)
                : isHovered
                    ? new Color(48, 56, 73, 255)
                    : new Color(31, 36, 49, 255));

        DrawBorder(
            spriteBatch,
            rectangle,
            isSelected
                ? new Color(232, 194, 103, 255)
                : new Color(70, 80, 102, 255),
            thickness: 1);

        var job =
            _jobs[index];

        spriteBatch.DrawString(
            font,
            job.Name,
            new Vector2(
                rectangle.X + 12,
                rectangle.Y + 8),
            Color.White);

        spriteBatch.DrawString(
            font,
            $"HP {job.MaximumHealth}  ATK {job.AttackDamage}  RNG {job.AttackRange}  MOV {job.MovementRange}",
            new Vector2(
                rectangle.X + 12,
                rectangle.Y + 31),
            new Color(202, 210, 224, 255));

        spriteBatch.DrawString(
            font,
            job.Description,
            new Vector2(
                rectangle.X + 12,
                rectangle.Y + 53),
            new Color(148, 157, 174, 255));
    }

    private void DrawFooterButtons(
        SpriteBatch spriteBatch,
        SpriteFont font,
        Viewport viewport)
    {
        DrawFooterButton(
            spriteBatch,
            font,
            GetBackButtonRectangle(viewport),
            "Back",
            _isBackHovered,
            isEnabled: true);

        DrawFooterButton(
            spriteBatch,
            font,
            GetStartButtonRectangle(viewport),
            "Start Battle",
            _isStartHovered,
            CanStart());
    }

    private void DrawFooterButton(
        SpriteBatch spriteBatch,
        SpriteFont font,
        Rectangle rectangle,
        string text,
        bool isHovered,
        bool isEnabled)
    {
        DrawRectangle(
            spriteBatch,
            rectangle,
            !isEnabled
                ? new Color(58, 58, 64, 255)
                : isHovered
                    ? new Color(74, 111, 176, 255)
                    : new Color(48, 56, 73, 255));

        DrawBorder(
            spriteBatch,
            rectangle,
            isEnabled && isHovered
                ? new Color(232, 194, 103, 255)
                : new Color(196, 204, 218, 255),
            thickness: 2);

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
            isEnabled
                ? Color.White
                : new Color(145, 145, 152, 255));
    }

    private Rectangle GetMemberPanelRectangle(
        Viewport viewport)
    {
        return new Rectangle(
            OuterPadding,
            72,
            LeftPanelWidth,
            viewport.Height - 144);
    }

    private Rectangle GetJobPanelRectangle(
        Viewport viewport)
    {
        return new Rectangle(
            OuterPadding + LeftPanelWidth + PanelSpacing,
            72,
            viewport.Width -
            LeftPanelWidth -
            PanelSpacing -
            OuterPadding * 2,
            viewport.Height - 144);
    }

    private Rectangle GetMemberRowRectangle(
        Viewport viewport,
        int index)
    {
        var panel =
            GetMemberPanelRectangle(
                viewport);

        return new Rectangle(
            panel.X + 12,
            panel.Y + 50 + index * (MemberRowHeight + 10),
            panel.Width - 24,
            MemberRowHeight);
    }

    private static Rectangle GetNameFieldRectangle(
        Rectangle row)
    {
        return new Rectangle(
            row.X + 12,
            row.Y + 38,
            row.Width - 24,
            32);
    }

    private Rectangle GetJobButtonRectangle(
        Viewport viewport,
        int index)
    {
        var panel =
            GetJobPanelRectangle(
                viewport);

        var columnSpacing =
            12;

        var rowSpacing =
            10;

        var columnWidth =
            (panel.Width - 44 - columnSpacing) / 2;

        var column =
            index % 2;

        var row =
            index / 2;

        return new Rectangle(
            panel.X + 16 + column * (columnWidth + columnSpacing),
            panel.Y + 50 + row * (JobButtonHeight + rowSpacing),
            columnWidth,
            JobButtonHeight);
    }

    private static Rectangle GetBackButtonRectangle(
        Viewport viewport)
    {
        return new Rectangle(
            OuterPadding,
            viewport.Height - 56,
            140,
            ButtonHeight);
    }

    private static Rectangle GetStartButtonRectangle(
        Viewport viewport)
    {
        return new Rectangle(
            viewport.Width - OuterPadding - 180,
            viewport.Height - 56,
            180,
            ButtonHeight);
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

    private sealed class MemberDraft
    {
        public string Name { get; set; } = string.Empty;

        public int JobIndex { get; set; }
    }
}
