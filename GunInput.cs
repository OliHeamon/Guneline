using Celeste;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;

namespace Guneline
{
    public static class GunInput
    {
        public static VirtualJoystick joystickAim;

        private static Vector2 currentCursorPos;

        private static Vector2 oldMousePos;

        public static bool GunShot => (Guneline.Settings?.ShootButton?.Pressed ?? false) || MouseShot;

        public static Vector2 CursorPosition
        {
            get => currentCursorPos;
            set
            {
                currentCursorPos = value;
                oldMousePos = value;
            }
        }

        private static bool MouseShot => MInput.Mouse.PressedLeftButton;

        private static MouseState State => Mouse.GetState();

        private static Vector2 MouseCursorPos => Vector2.Transform(new Vector2(State.X, State.Y), Matrix.Invert(Engine.ScreenMatrix));

        public static void UpdateInput(Player self)
        {
            currentCursorPos += oldMousePos - currentCursorPos;

            oldMousePos = MouseCursorPos;

            if (joystickAim.Value.LengthSquared() > 0.04f && self.Scene != null)
            {
                currentCursorPos = (self.Center - (self.Scene as Level).Camera.Position + (joystickAim.Value * 70)) * 6;
            }
        }

        public static void RegisterInputs()
        {
            joystickAim = new VirtualJoystick(true, new VirtualJoystick.PadRightStick(Input.Gamepad, 0.2f));
        }

        public static void DeregisterInputs()
        {
            joystickAim?.Deregister();
        }
    }
}
