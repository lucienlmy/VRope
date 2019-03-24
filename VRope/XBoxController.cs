using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTA;
using GTA.Math;
using GTA.Native;

using SharpDX.XInput;

namespace VRope
{
    static class XBoxController
    {
        private static Controller xboxController = null;
        private static State oldControllerState;
        private static State newControllerState;

        private static UserIndex[] Indexes = new UserIndex[] { UserIndex.One, UserIndex.Two, UserIndex.Three, UserIndex.Four };

        public static bool CheckForController()
        {
            bool controllerFound = false;

            for (int i = 0; i < Indexes.Length; i++)
            {
                Controller Controller = new Controller(Indexes[i]);

                if (Controller.IsConnected)
                {
                    xboxController = Controller;
                    controllerFound = true;
                    break;
                }
                else
                {
                    Controller = null;
                }
            }

            return controllerFound;
        }

        public static bool IsControllerConnected()
        {
            return (xboxController != null && xboxController.IsConnected);
        }

        public static bool WasControllerButtonPressed(Gamepad buttonData)
        {
            return (IsControllerButtonPressed(buttonData, newControllerState) &&
                    !IsControllerButtonPressed(buttonData, oldControllerState));
        }

        public static bool WasControllerButtonReleased(Gamepad buttonData)
        {
            return (IsControllerButtonPressed(buttonData, oldControllerState) &&
                    !IsControllerButtonPressed(buttonData, newControllerState));
        }

        public static bool IsControllerButtonPressed(Gamepad buttonData, State state)
        {
            Gamepad stateData = state.Gamepad;

            bool isPressed = stateData.Buttons.HasFlag(buttonData.Buttons);

            if (buttonData.LeftTrigger > 0)
                isPressed = isPressed && (stateData.LeftTrigger >= buttonData.LeftTrigger);

            if (buttonData.RightTrigger > 0)
                isPressed = isPressed && (stateData.RightTrigger >= buttonData.RightTrigger);

            return isPressed;
        }

        public static bool IsControllerButtonReleased(Gamepad buttonData, State state)
        {
            Gamepad stateData = state.Gamepad;

            bool isPressed = !stateData.Buttons.HasFlag(buttonData.Buttons);

            if (buttonData.LeftTrigger > 0)
                isPressed = isPressed && (stateData.LeftTrigger < buttonData.LeftTrigger);

            if (buttonData.RightTrigger > 0)
                isPressed = isPressed && (stateData.RightTrigger < buttonData.RightTrigger);

            return isPressed;
        }

        public static bool IsControllerButtonPressed(Gamepad buttonData)
        {
            return IsControllerButtonPressed(buttonData, newControllerState);
        }

        public static bool IsControllerButtonReleased(Gamepad buttonData)
        {
            return IsControllerButtonReleased(buttonData, newControllerState);
        }

        public static void UpdateStateBegin()
        {
            if(xboxController != null)
            {
                newControllerState = xboxController.GetState();
            }
        }

        public static void UpdateStateEnd()
        {
            if (xboxController != null)
            {
                oldControllerState = newControllerState;
            }
        }
    }
}
