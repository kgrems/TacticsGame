using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TacticsGame.Input;

/// <summary>
/// Tracks the current and previous input states so the game can
/// distinguish new input actions from held input states.
/// </summary>
public sealed class InputManager
{
    private MouseState _currentMouseState;
    private MouseState _previousMouseState;

    public InputManager()
    {
        _currentMouseState =
            Mouse.GetState();

        _previousMouseState =
            _currentMouseState;
    }

    public Point MousePosition =>
        new(
            _currentMouseState.X,
            _currentMouseState.Y);

    public Point MousePositionDelta =>
        new(
            _currentMouseState.X -
            _previousMouseState.X,

            _currentMouseState.Y -
            _previousMouseState.Y);

    public bool IsLeftMouseButtonPressed =>
        _currentMouseState.LeftButton ==
            ButtonState.Pressed &&
        _previousMouseState.LeftButton ==
            ButtonState.Released;

    public bool IsRightMouseButtonPressed =>
        _currentMouseState.RightButton ==
            ButtonState.Pressed &&
        _previousMouseState.RightButton ==
            ButtonState.Released;

    public bool IsMiddleMouseButtonDown =>
        _currentMouseState.MiddleButton ==
        ButtonState.Pressed;

    public int ScrollWheelDelta =>
        _currentMouseState.ScrollWheelValue -
        _previousMouseState.ScrollWheelValue;

    public void Update()
    {
        _previousMouseState =
            _currentMouseState;

        _currentMouseState =
            Mouse.GetState();
    }
}