
using GTA;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using System.Windows.Forms;

namespace VRope
{
    //class KeyState
    //{
    //    public Keys key;
    //    public bool isPressed = false;

    //    public KeyState(Keys key, bool isPressed)
    //    {
    //        this.key = key;
    //        this.isPressed = isPressed;
    //    }
    //}

    class Keyboard
    {
        private static Keys[] AllKeys = (Keys[])Enum.GetValues(typeof(Keys));

        private Dictionary<Keys, bool> oldKeyState = new Dictionary<Keys, bool>(30);
        private Dictionary<Keys, bool> newKeyState = new Dictionary<Keys, bool>(30);

        private List<Keys> UniqueKeyList = new List<Keys>(30);

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

        private bool IsKeyInUniqueList(Keys key)
        {
            for (int i = 0; i < UniqueKeyList.Count; i++)
                if (UniqueKeyList[i] == key)
                    return true;

            return false;
        }

        public void ExtractUniqueKeys(KeyLists keyLists)
        {
            for(int i=0; i<keyLists.GetLists().Count; i++)
            {
                KeyData keyData = keyLists.GetLists()[i];

                for (int j=0; j<keyData.list.Count; j++)
                {
                    Keys key = keyData.list[j];

                    if (!IsKeyInUniqueList(key))
                        UniqueKeyList.Add(key);
                }
            }
        }

        public void UpdateStateBegin()
        {
            foreach(var pair in newKeyState)
            {
                newKeyState[pair.Key] = Keyboard.IsKeyPressed(pair.Key);
            }
        }

        public void UpdateStateEnd()
        {
            foreach (var pair in oldKeyState)
            {
                oldKeyState[pair.Key] = newKeyState[pair.Key];
            }
        }
    }
}
