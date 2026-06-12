using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace TacticsGame.Rendering;

/// <summary>
/// Applies zooming and panning to the game world.
/// </summary>
public sealed class Camera2D
{
    private const float MinimumZoom = 0.50f;
    private const float MaximumZoom = 3.00f;
    private const float ZoomStep = 0.10f;

    /// <summary>
    /// Current zoom scale applied to the world.
    /// </summary>
    public float Zoom { get; private set; } = 1.5f;

    /// <summary>
    /// Screen-space translation applied after scaling.
    /// This makes drag panning feel consistent at every zoom level.
    /// </summary>
    public Vector2 PanOffset { get; private set; } =
        Vector2.Zero;

    /// <summary>
    /// Applies one zoom step based on the direction of the mouse wheel.
    /// Returns true when the zoom level changed.
    /// </summary>
    public bool AdjustZoom(
        int scrollWheelDelta)
    {
        if (scrollWheelDelta == 0)
        {
            return false;
        }

        var previousZoom =
            Zoom;

        var direction =
            Math.Sign(
                scrollWheelDelta);

        Zoom =
            Math.Clamp(
                Zoom + (direction * ZoomStep),
                MinimumZoom,
                MaximumZoom);

        return
            Math.Abs(
                Zoom - previousZoom) >
            float.Epsilon;
    }

    /// <summary>
    /// Moves the world by the supplied number of screen pixels.
    /// Positive X moves the world right.
    /// Positive Y moves the world down.
    /// </summary>
    public void Pan(
        Vector2 screenDelta)
    {
        PanOffset +=
            screenDelta;
    }

    /// <summary>
    /// Converts a world-space position into a screen-space position.
    /// This is used to attach screen-space UI to world-space units.
    /// </summary>
    public Vector2 WorldToScreen(
        Vector2 worldPosition,
        Viewport viewport)
    {
        return
            Vector2.Transform(
                worldPosition,
                GetTransform(
                    viewport));
    }

    /// <summary>
    /// Restores the initial camera position and zoom.
    /// </summary>
    public void Reset()
    {
        Zoom =
            1.00f;

        PanOffset =
            Vector2.Zero;
    }

    /// <summary>
    /// Creates the transform applied by SpriteBatch when drawing the world.
    /// Zooming occurs around the viewport center and panning is then applied
    /// as a screen-space translation.
    /// </summary>
    public Matrix GetTransform(
        Viewport viewport)
    {
        var viewportCenter =
            new Vector2(
                viewport.Width / 2.0f,
                viewport.Height / 2.0f);

        return
            Matrix.CreateTranslation(
                xPosition: -viewportCenter.X,
                yPosition: -viewportCenter.Y,
                zPosition: 0.0f) *
            Matrix.CreateScale(
                Zoom) *
            Matrix.CreateTranslation(
                xPosition:
                    viewportCenter.X +
                    PanOffset.X,
                yPosition:
                    viewportCenter.Y +
                    PanOffset.Y,
                zPosition: 0.0f);
    }

    /// <summary>
    /// Converts a screen-space mouse position back into world coordinates.
    /// This keeps tile hover and selection aligned after zooming or panning.
    /// </summary>
    public Vector2 ScreenToWorld(
        Vector2 screenPosition,
        Viewport viewport)
    {
        var inverseTransform =
            Matrix.Invert(
                GetTransform(
                    viewport));

        return
            Vector2.Transform(
                screenPosition,
                inverseTransform);
    }
}