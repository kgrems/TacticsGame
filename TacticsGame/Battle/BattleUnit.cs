#nullable enable

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using TacticsGame.Items;

namespace TacticsGame.Battle;

public sealed class BattleUnit
{
    public string Name { get; init; } = string.Empty;

    public BattleTeam Team { get; init; }

    public Point Position { get; set; }

    public Vector2 RenderGridPosition { get; set; }

    public int MaximumHealth { get; init; }

    public int CurrentHealth { get; set; }

    public int AttackDamage { get; init; }

    public int AttackRange { get; init; }

    public int MovementRange { get; init; }

    public int JumpHeight { get; init; }

    public EquipmentItem? HeadItem { get; set; }

    public EquipmentItem? ChestItem { get; set; }

    public EquipmentItem? ArmsItem { get; set; }

    public EquipmentItem? LegsItem { get; set; }

    public EquipmentItem? Charm1Item { get; set; }

    public EquipmentItem? Charm2Item { get; set; }

    public UnitFacing Facing { get; set; } = UnitFacing.FrontRight;

    public UnitTurnState TurnState { get; } = new();

    public bool IsDefeated => CurrentHealth <= 0;

    public IEnumerable<EquipmentItem> EquippedItems
    {
        get
        {
            if (HeadItem is not null)
            {
                yield return HeadItem;
            }

            if (ChestItem is not null)
            {
                yield return ChestItem;
            }

            if (ArmsItem is not null)
            {
                yield return ArmsItem;
            }

            if (LegsItem is not null)
            {
                yield return LegsItem;
            }

            if (Charm1Item is not null)
            {
                yield return Charm1Item;
            }

            if (Charm2Item is not null)
            {
                yield return Charm2Item;
            }
        }
    }

    public int HealthBonus =>
        GetTotalBonus(item =>
            item.HealthBonus);

    public int AttackBonus =>
        GetTotalBonus(item =>
            item.AttackBonus);

    public int DefenseBonus =>
        GetTotalBonus(item =>
            item.DefenseBonus);

    public int MovementBonus =>
        GetTotalBonus(item =>
            item.MovementBonus);

    public int EffectiveMaximumHealth =>
        Math.Max(
            1,
            MaximumHealth + HealthBonus);

    public int EffectiveAttackDamage =>
        Math.Max(
            0,
            AttackDamage + AttackBonus);

    public int EffectiveDefense =>
        Math.Max(
            0,
            DefenseBonus);

    public int EffectiveMovementRange =>
        Math.Max(
            0,
            MovementRange + MovementBonus);

    public EquipmentItem? GetEquippedItem(
        EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Head => HeadItem,
            EquipmentSlot.Chest => ChestItem,
            EquipmentSlot.Arms => ArmsItem,
            EquipmentSlot.Legs => LegsItem,
            EquipmentSlot.Charm1 => Charm1Item,
            EquipmentSlot.Charm2 => Charm2Item,
            _ => throw new ArgumentOutOfRangeException(
                nameof(slot))
        };
    }

    public void SetEquippedItem(
        EquipmentSlot slot,
        EquipmentItem? item)
    {
        if (item is not null &&
            !CanEquipItemInSlot(
                item,
                slot))
        {
            throw new InvalidOperationException(
                $"Item '{item.Name}' cannot be equipped in {slot}.");
        }

        var previousMaximumHealth =
            EffectiveMaximumHealth;

        switch (slot)
        {
            case EquipmentSlot.Head:
                HeadItem = item;
                break;

            case EquipmentSlot.Chest:
                ChestItem = item;
                break;

            case EquipmentSlot.Arms:
                ArmsItem = item;
                break;

            case EquipmentSlot.Legs:
                LegsItem = item;
                break;

            case EquipmentSlot.Charm1:
                Charm1Item = item;
                break;

            case EquipmentSlot.Charm2:
                Charm2Item = item;
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(slot));
        }

        var maximumHealthDelta =
            EffectiveMaximumHealth -
            previousMaximumHealth;

        if (CurrentHealth > 0)
        {
            CurrentHealth +=
                maximumHealthDelta;
        }

        ClampCurrentHealthToEffectiveMaximum();
    }

    public void ClampCurrentHealthToEffectiveMaximum()
    {
        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            return;
        }

        CurrentHealth =
            Math.Clamp(
                CurrentHealth,
                1,
                EffectiveMaximumHealth);
    }

    public static bool CanEquipItemInSlot(
        EquipmentItem item,
        EquipmentSlot slot)
    {
        ArgumentNullException.ThrowIfNull(
            item);

        if (item.Slot == slot)
        {
            return true;
        }

        return IsCharmSlot(item.Slot) &&
               IsCharmSlot(slot);
    }

    public void ResetRenderPosition()
    {
        RenderGridPosition =
            new Vector2(
                Position.X,
                Position.Y);
    }

    private int GetTotalBonus(
        Func<EquipmentItem, int> selectBonus)
    {
        var total = 0;

        foreach (var item in EquippedItems)
        {
            total +=
                selectBonus(item);
        }

        return total;
    }

    private static bool IsCharmSlot(
        EquipmentSlot slot)
    {
        return slot is EquipmentSlot.Charm1 or EquipmentSlot.Charm2;
    }
}
