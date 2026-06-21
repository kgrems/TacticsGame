#nullable enable

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace TacticsGame.Campaign;

public sealed class WorldMapNode
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public WorldMapNodeType Type { get; init; }

    public Vector2 NormalizedPosition { get; init; }

    public IReadOnlyList<int> ConnectedNodeIds { get; init; } =
        Array.Empty<int>();
}
