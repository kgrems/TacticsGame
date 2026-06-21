#nullable enable

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using TacticsGame.Battle;
using TacticsGame.Items;

namespace TacticsGame.UI;

public sealed class PartyManagementScreen : IDisposable
{
    private const int OuterPadding = 24;
    private const int HeaderHeight = 48;
    private const int PanelSpacing = 18;
    private const int LeftPanelWidth = 320;
    private const int UnitRowHeight = 58;
    private const int SlotRowHeight = 64;
    private const int GearRowHeight = 52;

    private static readonly EquipmentSlot[] LoadoutSlots =
    {
        EquipmentSlot.Head,
        EquipmentSlot.Chest,
        EquipmentSlot.Legs,
        EquipmentSlot.Arms,
        EquipmentSlot.Charm1,
        EquipmentSlot.Charm2
    };

    private readonly Texture2D _pixelTexture;

    private IReadOnlyList<BattleUnit> _playerUnits =
        Array.Empty<BattleUnit>();

    private IReadOnlyList<EquipmentItem> _availableGear =
        Array.Empty<EquipmentItem>();

    private BattleUnit? _selectedUnit;
    private EquipmentSlot _selectedSlot = EquipmentSlot.Head;
    private int? _hoveredUnitIndex;
    private EquipmentSlot? _hoveredSlot;
    private int? _hoveredGearOptionIndex;
    private bool _isBackHovered;

    public bool WantsClose { get; private set; }

    public PartyManagementScreen(
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
        IReadOnlyList<BattleUnit> playerUnits,
        IReadOnlyList<EquipmentItem> availableGear)
    {
        ArgumentNullException.ThrowIfNull(
            playerUnits);

        ArgumentNullException.ThrowIfNull(
            availableGear);

        _playerUnits =
            playerUnits.ToList();

        _availableGear =
            availableGear.ToList();

        if (_selectedUnit is null ||
            !_playerUnits.Contains(_selectedUnit))
        {
            _selectedUnit =
                _playerUnits.FirstOrDefault();
        }

        if (!LoadoutSlots.Contains(_selectedSlot))
        {
            _selectedSlot =
                EquipmentSlot.Head;
        }

        WantsClose = false;
    }

    public void Update(
        Point mousePosition,
        bool isLeftMouseButtonPressed,
        Viewport viewport)
    {
        WantsClose = false;
        _hoveredUnitIndex = null;
        _hoveredSlot = null;
        _hoveredGearOptionIndex = null;

        var backButtonRectangle =
            GetBackButtonRectangle(
                viewport);

        _isBackHovered =
            backButtonRectangle.Contains(
                mousePosition);

        if (isLeftMouseButtonPressed &&
            _isBackHovered)
        {
            WantsClose = true;
            return;
        }

        UpdateUnitRows(
            mousePosition,
            isLeftMouseButtonPressed,
            viewport);

        if (_selectedUnit is null)
        {
            return;
        }

        UpdateSlotRows(
            mousePosition,
            isLeftMouseButtonPressed,
            viewport);

        UpdateGearRows(
            mousePosition,
            isLeftMouseButtonPressed,
            viewport);
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

        var screenRectangle =
            new Rectangle(
                0,
                0,
                viewport.Width,
                viewport.Height);

        DrawRectangle(
            spriteBatch,
            screenRectangle,
            new Color(12, 15, 22, 238));

        DrawHeader(
            spriteBatch,
            font,
            viewport);

        DrawUnitList(
            spriteBatch,
            font,
            viewport);

        DrawCharacterPanel(
            spriteBatch,
            font,
            viewport);
    }

    public void Dispose()
    {
        _pixelTexture.Dispose();
    }

    private void UpdateUnitRows(
        Point mousePosition,
        bool isLeftMouseButtonPressed,
        Viewport viewport)
    {
        for (var index = 0; index < _playerUnits.Count; index++)
        {
            var rowRectangle =
                GetUnitRowRectangle(
                    viewport,
                    index);

            if (!rowRectangle.Contains(mousePosition))
            {
                continue;
            }

            _hoveredUnitIndex = index;

            if (!isLeftMouseButtonPressed)
            {
                return;
            }

            _selectedUnit =
                _playerUnits[index];

            _selectedSlot =
                EquipmentSlot.Head;

            return;
        }
    }

    private void UpdateSlotRows(
        Point mousePosition,
        bool isLeftMouseButtonPressed,
        Viewport viewport)
    {
        for (var index = 0; index < LoadoutSlots.Length; index++)
        {
            var slot =
                LoadoutSlots[index];

            var rowRectangle =
                GetSlotRowRectangle(
                    viewport,
                    index);

            if (!rowRectangle.Contains(mousePosition))
            {
                continue;
            }

            _hoveredSlot = slot;

            if (isLeftMouseButtonPressed)
            {
                _selectedSlot = slot;
            }

            return;
        }
    }

    private void UpdateGearRows(
        Point mousePosition,
        bool isLeftMouseButtonPressed,
        Viewport viewport)
    {
        var gearOptions =
            GetGearOptions(
                _selectedSlot);

        for (var optionIndex = 0;
             optionIndex <= gearOptions.Count;
             optionIndex++)
        {
            var rowRectangle =
                GetGearRowRectangle(
                    viewport,
                    optionIndex);

            if (!rowRectangle.Contains(mousePosition))
            {
                continue;
            }

            _hoveredGearOptionIndex =
                optionIndex;

            if (!isLeftMouseButtonPressed ||
                _selectedUnit is null)
            {
                return;
            }

            var selectedItem =
                optionIndex == 0
                    ? null
                    : gearOptions[optionIndex - 1];

            _selectedUnit.SetEquippedItem(
                _selectedSlot,
                selectedItem);

            return;
        }
    }

    private void DrawHeader(
        SpriteBatch spriteBatch,
        SpriteFont font,
        Viewport viewport)
    {
        spriteBatch.DrawString(
            font,
            "Party Management",
            new Vector2(
                OuterPadding,
                16),
            Color.White);

        var backButtonRectangle =
            GetBackButtonRectangle(
                viewport);

        DrawRectangle(
            spriteBatch,
            backButtonRectangle,
            _isBackHovered
                ? new Color(86, 108, 150, 255)
                : new Color(48, 56, 73, 255));

        DrawBorder(
            spriteBatch,
            backButtonRectangle,
            new Color(196, 204, 218, 255),
            thickness: 1);

        var text = "Battle";
        var textSize =
            font.MeasureString(
                text);

        spriteBatch.DrawString(
            font,
            text,
            new Vector2(
                backButtonRectangle.X +
                ((backButtonRectangle.Width - textSize.X) / 2.0f),
                backButtonRectangle.Y +
                ((backButtonRectangle.Height - textSize.Y) / 2.0f)),
            Color.White);
    }

    private void DrawUnitList(
        SpriteBatch spriteBatch,
        SpriteFont font,
        Viewport viewport)
    {
        var panelRectangle =
            GetUnitListRectangle(
                viewport);

        DrawPanel(
            spriteBatch,
            panelRectangle);

        spriteBatch.DrawString(
            font,
            "Team",
            new Vector2(
                panelRectangle.X + 14,
                panelRectangle.Y + 14),
            Color.White);

        if (_playerUnits.Count == 0)
        {
            spriteBatch.DrawString(
                font,
                "No player units",
                new Vector2(
                    panelRectangle.X + 14,
                    panelRectangle.Y + 52),
                new Color(172, 179, 192, 255));

            return;
        }

        for (var index = 0; index < _playerUnits.Count; index++)
        {
            DrawUnitRow(
                spriteBatch,
                font,
                viewport,
                index);
        }
    }

    private void DrawUnitRow(
        SpriteBatch spriteBatch,
        SpriteFont font,
        Viewport viewport,
        int index)
    {
        var unit =
            _playerUnits[index];

        var rowRectangle =
            GetUnitRowRectangle(
                viewport,
                index);

        var isSelected =
            ReferenceEquals(
                unit,
                _selectedUnit);

        var backgroundColor =
            isSelected
                ? new Color(66, 92, 136, 255)
                : _hoveredUnitIndex == index
                    ? new Color(48, 56, 73, 255)
                    : new Color(31, 36, 49, 255);

        DrawRectangle(
            spriteBatch,
            rowRectangle,
            backgroundColor);

        DrawBorder(
            spriteBatch,
            rowRectangle,
            isSelected
                ? new Color(232, 194, 103, 255)
                : new Color(70, 80, 102, 255),
            thickness: 1);

        spriteBatch.DrawString(
            font,
            unit.Name,
            new Vector2(
                rowRectangle.X + 12,
                rowRectangle.Y + 8),
            Color.White);

        spriteBatch.DrawString(
            font,
            $"Lv {unit.Level}  HP {unit.CurrentHealth}/{unit.EffectiveMaximumHealth}",
            new Vector2(
                rowRectangle.X + 12,
                rowRectangle.Y + 31),
            new Color(202, 210, 224, 255));
    }

    private void DrawCharacterPanel(
        SpriteBatch spriteBatch,
        SpriteFont font,
        Viewport viewport)
    {
        var panelRectangle =
            GetCharacterPanelRectangle(
                viewport);

        DrawPanel(
            spriteBatch,
            panelRectangle);

        if (_selectedUnit is null)
        {
            spriteBatch.DrawString(
                font,
                "Choose a unit",
                new Vector2(
                    panelRectangle.X + 18,
                    panelRectangle.Y + 18),
                Color.White);

            return;
        }

        spriteBatch.DrawString(
            font,
            _selectedUnit.Name,
            new Vector2(
                panelRectangle.X + 18,
                panelRectangle.Y + 16),
            Color.White);

        DrawStatSummary(
            spriteBatch,
            font,
            panelRectangle);

        DrawLoadoutSlots(
            spriteBatch,
            font,
            viewport);

        DrawGearOptions(
            spriteBatch,
            font,
            viewport);
    }

    private void DrawStatSummary(
        SpriteBatch spriteBatch,
        SpriteFont font,
        Rectangle panelRectangle)
    {
        if (_selectedUnit is null)
        {
            return;
        }

        var summary =
            $"Lv {_selectedUnit.Level}  XP {_selectedUnit.ExperienceIntoLevel}/{BattleUnit.ExperiencePerLevel}  " +
            $"HP {_selectedUnit.EffectiveMaximumHealth}  " +
            $"ATK {_selectedUnit.EffectiveAttackDamage}  " +
            $"DEF {_selectedUnit.EffectiveDefense}  " +
            $"MOV {_selectedUnit.EffectiveMovementRange}";

        spriteBatch.DrawString(
            font,
            summary,
            new Vector2(
                panelRectangle.X + 18,
                panelRectangle.Y + 44),
            new Color(202, 210, 224, 255));
    }

    private void DrawLoadoutSlots(
        SpriteBatch spriteBatch,
        SpriteFont font,
        Viewport viewport)
    {
        for (var index = 0; index < LoadoutSlots.Length; index++)
        {
            var slot =
                LoadoutSlots[index];

            var rowRectangle =
                GetSlotRowRectangle(
                    viewport,
                    index);

            var isSelected =
                slot == _selectedSlot;

            var backgroundColor =
                isSelected
                    ? new Color(55, 81, 125, 255)
                    : _hoveredSlot == slot
                        ? new Color(48, 56, 73, 255)
                        : new Color(31, 36, 49, 255);

            DrawRectangle(
                spriteBatch,
                rowRectangle,
                backgroundColor);

            DrawBorder(
                spriteBatch,
                rowRectangle,
                isSelected
                    ? new Color(232, 194, 103, 255)
                    : new Color(70, 80, 102, 255),
                thickness: 1);

            var equippedItem =
                _selectedUnit is null
                    ? null
                    : _selectedUnit.GetEquippedItem(
                        slot);

            spriteBatch.DrawString(
                font,
                GetSlotLabel(slot),
                new Vector2(
                    rowRectangle.X + 12,
                    rowRectangle.Y + 8),
                Color.White);

            spriteBatch.DrawString(
                font,
                equippedItem?.Name ?? "Empty",
                new Vector2(
                    rowRectangle.X + 12,
                    rowRectangle.Y + 32),
                equippedItem is null
                    ? new Color(148, 157, 174, 255)
                    : new Color(202, 210, 224, 255));
        }
    }

    private void DrawGearOptions(
        SpriteBatch spriteBatch,
        SpriteFont font,
        Viewport viewport)
    {
        var titlePosition =
            GetGearTitlePosition(
                viewport);

        spriteBatch.DrawString(
            font,
            $"Available {GetSlotLabel(_selectedSlot)} Gear",
            titlePosition,
            Color.White);

        var gearOptions =
            GetGearOptions(
                _selectedSlot);

        DrawGearOptionRow(
            spriteBatch,
            font,
            viewport,
            optionIndex: 0,
            item: null);

        for (var index = 0; index < gearOptions.Count; index++)
        {
            DrawGearOptionRow(
                spriteBatch,
                font,
                viewport,
                optionIndex: index + 1,
                item: gearOptions[index]);
        }
    }

    private void DrawGearOptionRow(
        SpriteBatch spriteBatch,
        SpriteFont font,
        Viewport viewport,
        int optionIndex,
        EquipmentItem? item)
    {
        var rowRectangle =
            GetGearRowRectangle(
                viewport,
                optionIndex);

        var currentItem =
            _selectedUnit is null
                ? null
                : _selectedUnit.GetEquippedItem(
                    _selectedSlot);

        var isSelected =
            ReferenceEquals(
                currentItem,
                item);

        var backgroundColor =
            isSelected
                ? new Color(55, 81, 125, 255)
                : _hoveredGearOptionIndex == optionIndex
                    ? new Color(48, 56, 73, 255)
                    : new Color(31, 36, 49, 255);

        DrawRectangle(
            spriteBatch,
            rowRectangle,
            backgroundColor);

        DrawBorder(
            spriteBatch,
            rowRectangle,
            isSelected
                ? new Color(232, 194, 103, 255)
                : new Color(70, 80, 102, 255),
            thickness: 1);

        var nameText =
            item?.Name ?? "Unequip";

        spriteBatch.DrawString(
            font,
            nameText,
            new Vector2(
                rowRectangle.X + 12,
                rowRectangle.Y + 7),
            Color.White);

        spriteBatch.DrawString(
            font,
            item is null
                ? "Empty slot"
                : FormatBonuses(item),
            new Vector2(
                rowRectangle.X + 12,
                rowRectangle.Y + 29),
            new Color(202, 210, 224, 255));
    }

    private IReadOnlyList<EquipmentItem> GetGearOptions(
        EquipmentSlot slot)
    {
        return _availableGear
            .Where(item =>
                BattleUnit.CanEquipItemInSlot(
                    item,
                    slot))
            .ToList();
    }

    private Rectangle GetUnitListRectangle(
        Viewport viewport)
    {
        return new Rectangle(
            OuterPadding,
            HeaderHeight + OuterPadding,
            LeftPanelWidth,
            viewport.Height -
            HeaderHeight -
            (OuterPadding * 2));
    }

    private Rectangle GetCharacterPanelRectangle(
        Viewport viewport)
    {
        return new Rectangle(
            OuterPadding + LeftPanelWidth + PanelSpacing,
            HeaderHeight + OuterPadding,
            viewport.Width -
            LeftPanelWidth -
            PanelSpacing -
            (OuterPadding * 2),
            viewport.Height -
            HeaderHeight -
            (OuterPadding * 2));
    }

    private Rectangle GetBackButtonRectangle(
        Viewport viewport)
    {
        return new Rectangle(
            viewport.Width - OuterPadding - 120,
            12,
            120,
            34);
    }

    private Rectangle GetUnitRowRectangle(
        Viewport viewport,
        int index)
    {
        var panelRectangle =
            GetUnitListRectangle(
                viewport);

        return new Rectangle(
            panelRectangle.X + 12,
            panelRectangle.Y + 50 + (index * (UnitRowHeight + 8)),
            panelRectangle.Width - 24,
            UnitRowHeight);
    }

    private Rectangle GetSlotRowRectangle(
        Viewport viewport,
        int index)
    {
        var panelRectangle =
            GetCharacterPanelRectangle(
                viewport);

        var contentX =
            panelRectangle.X + 18;

        var contentWidth =
            panelRectangle.Width - 36;

        var columnSpacing =
            12;

        var columnWidth =
            (contentWidth - columnSpacing) / 2;

        var column =
            index % 2;

        var row =
            index / 2;

        return new Rectangle(
            contentX + (column * (columnWidth + columnSpacing)),
            panelRectangle.Y + 82 + (row * (SlotRowHeight + 10)),
            columnWidth,
            SlotRowHeight);
    }

    private Vector2 GetGearTitlePosition(
        Viewport viewport)
    {
        var panelRectangle =
            GetCharacterPanelRectangle(
                viewport);

        return new Vector2(
            panelRectangle.X + 18,
            panelRectangle.Y + 300);
    }

    private Rectangle GetGearRowRectangle(
        Viewport viewport,
        int optionIndex)
    {
        var panelRectangle =
            GetCharacterPanelRectangle(
                viewport);

        return new Rectangle(
            panelRectangle.X + 18,
            panelRectangle.Y + 334 + (optionIndex * (GearRowHeight + 8)),
            panelRectangle.Width - 36,
            GearRowHeight);
    }

    private static string GetSlotLabel(
        EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Head => "Head",
            EquipmentSlot.Chest => "Chest",
            EquipmentSlot.Arms => "Arms",
            EquipmentSlot.Legs => "Legs",
            EquipmentSlot.Charm1 => "Charm 1",
            EquipmentSlot.Charm2 => "Charm 2",
            _ => throw new ArgumentOutOfRangeException(
                nameof(slot))
        };
    }

    private static string FormatBonuses(
        EquipmentItem item)
    {
        var parts =
            new List<string>();

        AddBonus(
            parts,
            "HP",
            item.HealthBonus);

        AddBonus(
            parts,
            "ATK",
            item.AttackBonus);

        AddBonus(
            parts,
            "DEF",
            item.DefenseBonus);

        AddBonus(
            parts,
            "MOV",
            item.MovementBonus);

        return parts.Count == 0
            ? "No stat bonuses"
            : string.Join(
                "  ",
                parts);
    }

    private static void AddBonus(
        List<string> parts,
        string label,
        int value)
    {
        if (value == 0)
        {
            return;
        }

        parts.Add(
            $"{label} +{value}");
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
