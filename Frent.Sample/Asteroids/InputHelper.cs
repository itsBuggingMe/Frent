using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;

namespace FrentSandbox;
internal static class InputHelper
{
    public static KeyboardState KeyboardState { get; private set; }
    public static MouseState MouseState { get; private set; }
    public static TouchCollection Touches { get; private set; }



    public static KeyboardState PrevKeyboardState { get; private set; }
    public static MouseState PrevMouseState { get; private set; }
    public static TouchCollection PrevTouches { get; private set; }


    public static int DeltaScroll => MouseState.ScrollWheelValue - PrevMouseState.ScrollWheelValue;

    public static Point MouseLocation => MouseState.Position;

    public static void TickUpdate(bool isActive)
    {
        PrevKeyboardState = KeyboardState;
        PrevMouseState = MouseState;
        PrevTouches = Touches;

        KeyboardState = Keyboard.GetState();
        MouseState = Mouse.GetState();
        Touches = TouchPanel.GetState();

        if (!isActive)
        {
            PrevKeyboardState = default;
            PrevMouseState = default;
            KeyboardState = default;
            MouseState = default;
        }
    }

    /// <summary>
    /// Checks if a specific key has a rising edge (transition from released to pressed) in the current frame.
    /// </summary>
    /// <param name="key">The key to check for a rising edge.</param>
    /// <returns>True if the key has a rising edge, otherwise false.</returns>
    public static bool RisingEdge(this Keys key)
    {
        return KeyboardState.IsKeyDown(key) && !PrevKeyboardState.IsKeyDown(key);
    }

    /// <summary>
    /// Checks if a specific key has a falling edge (transition from pressed to released) in the current frame.
    /// </summary>
    /// <param name="key">The key to check for a falling edge.</param>
    /// <returns>True if the key has a falling edge, otherwise false.</returns>
    public static bool FallingEdge(this Keys key)
    {
        return !KeyboardState.IsKeyDown(key) && PrevKeyboardState.IsKeyDown(key);
    }

    /// <summary>
    /// Checks if the left or right mouse button has a rising edge (transition from released to pressed) in the current frame.
    /// </summary>
    /// <param name="IsLeftButton">True for the left mouse button, false for the right mouse button.</param>
    /// <returns>True if the specified mouse button has a rising edge, otherwise false.</returns>
    public static bool RisingEdge(this MouseButton Button)
    {
        if (Button == MouseButton.Left)
        {
            return MouseState.LeftButton != PrevMouseState.LeftButton && MouseState.LeftButton == ButtonState.Pressed;
        }
        return MouseState.RightButton != PrevMouseState.RightButton && MouseState.RightButton == ButtonState.Pressed;
    }

    /// <summary>
    /// Checks if the left or right mouse button has a falling edge (transition from pressed to released) in the current frame.
    /// </summary>
    /// <param name="IsLeftButton">True for the left mouse button, false for the right mouse button.</param>
    /// <returns>True if the specified mouse button has a falling edge, otherwise false.</returns>
    public static bool FallingEdge(this MouseButton Button)
    {
        if (Button == MouseButton.Left)
        {
            return MouseState.LeftButton != PrevMouseState.LeftButton && MouseState.LeftButton == ButtonState.Released;
        }
        return MouseState.RightButton != PrevMouseState.RightButton && MouseState.RightButton == ButtonState.Released;
    }

    /// <summary>
    /// Checks if a specific key is currently being held down.
    /// </summary>
    /// <param name="key">The key to check for being held down.</param>
    /// <returns>True if the key is currently held down, otherwise false.</returns>
    public static bool Down(this Keys key)
    {
        return KeyboardState.IsKeyDown(key);
    }

    /// <summary>
    /// Checks if the left or right mouse button is currently being held down.
    /// </summary>
    /// <param name="IsLeftButton">True for the left mouse button, false for the right mouse button.</param>
    /// <returns>True if the specified mouse button is currently held down, otherwise false.</returns>
    public static bool Down(this MouseButton button)
    {
        if (button == MouseButton.Left)
        {
            return MouseState.LeftButton == ButtonState.Pressed;
        }
        return MouseState.RightButton == ButtonState.Pressed;
    }
}

/// <summary>
/// Indicates the mouse button pressed
/// </summary>
internal enum MouseButton
{
    Left = 0, Right = 1,
}