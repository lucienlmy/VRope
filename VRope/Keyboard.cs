
using GTA;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using System.Windows.Forms;

/*
 * 
 * created by jeffsturm4nn
 * 
 */

namespace VRope
{
    public static class Keyboard
    {
        private const char SEPARATOR_CHAR = '+';

        public const Keys MOUSE_WHEEL_UP_KEY = Keys.F22;
        public const Keys MOUSE_WHEEL_DOWN_KEY = Keys.F23;

        [DllImport("User32.dll")]
        public static extern short GetAsyncKeyState(Keys vKey);

        public static bool IsKeyPressed(Keys key)
        {
            if(key == Keys.None)
            {
                return false;
            }
            else if (key == MOUSE_WHEEL_UP_KEY &&
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

        public static bool IsKeyUp(Keys key)
        {
            return !IsKeyPressed(key);
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

        public static bool IsKeyListUp(List<Keys> keys)
        {
            return !IsKeyListPressed(keys);
        }

        public static bool IsKeyValid(Keys key)
        {
            return Enum.IsDefined(typeof(Keys), key);
        }

        public static bool IsKeyListValid(List<Keys> keys)
        {
            if (keys == null || keys.Count == 0)
                return false;

            bool isValid = true;

            foreach (var key in keys)
            {
                if (!IsKeyValid(key))
                {
                    isValid = false;
                    break;
                }
            }

            return isValid;
        }

        public static List<Keys> TranslateKeyDataToKeyList(String keyData)
        {
            if (keyData == null || keyData.Length == 0)
                return new List<Keys>(0);

            keyData = keyData.Replace(" ", "");

            List<Keys> resultList = new List<Keys>(4);

            String[] keyStrings = keyData.Split(SEPARATOR_CHAR);

            for (int i = 0; i < keyStrings.Length; i++)
            {
                String keyString = keyStrings[i];

                if (keyString == "WeaponPrev")
                {
                    resultList.Add(Keyboard.MOUSE_WHEEL_UP_KEY);
                }
                else if (keyString == "WeaponNext")
                {
                    resultList.Add(Keyboard.MOUSE_WHEEL_DOWN_KEY);
                }
                else if(Enum.IsDefined(typeof(Keys), keyString))
                {
                    Keys key = (Keys)Enum.Parse(typeof(Keys), keyString);

                    resultList.Add(key);
                }
                else
                {
                    resultList.Clear();
                    break;
                }
            }

            return resultList;
        }

    }
}
