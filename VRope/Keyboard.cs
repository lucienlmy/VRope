
using GTA;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VRope
{
    static class Keyboard
    {
        public const Keys MOUSE_WHEEL_UP_KEY = Keys.F22;
        public const Keys MOUSE_WHEEL_DOWN_KEY = Keys.F23;

        [DllImport("User32.dll")]
        public static extern short GetAsyncKeyState(Keys vKey);

        public static bool IsKeyPressed(Keys key)
        {
            if (key == MOUSE_WHEEL_UP_KEY &&
                Game.IsControlPressed(2, GTA.Control.WeaponWheelPrev))
            {
                return true;
            }
            else if (key == MOUSE_WHEEL_DOWN_KEY &&
                Game.IsControlPressed(2, GTA.Control.WeaponWheelNext))
            {
                return true;
            }
            else
            {
                //return Game.IsKeyPressed(key);
                return ((GetAsyncKeyState(key) & 0x8000) == 0x8000);
            }
        }

        public static bool IsKeyListPressed(List<Keys> keys)
        {
            if (keys == null || keys.Count == 0)
                return false;

            bool isPressed = true;

            for (int i = 0; i < keys.Count; i++)
            {
                if (!IsKeyPressed(keys[i]))
                {
                    isPressed = false;
                    break;
                }
            }

            return isPressed;
        }


    }
}
