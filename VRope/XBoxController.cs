
using System;
using SharpDX.XInput;

namespace VRope
{
    public enum TriggerCondition
    {
        NONE = 0,

        PRESSED,
        HELD,
        RELEASED,

        ANY,
        CUSTOM
    }

    public struct ControllerState
    {
        public Gamepad buttonData;
        public int buttonPressedCount;

        public ControllerState(Gamepad buttonData, int buttonPressedCount)
        {
            this.buttonData = buttonData;
            this.buttonPressedCount = buttonPressedCount;
        }
    }

    static class XBoxController
    {
        public static byte LEFT_TRIGGER_THRESHOLD = 255;
        public static byte RIGHT_TRIGGER_THRESHOLD = 255;

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

        public static int GetButtonPressedCount(Gamepad buttonData)
        {
            if (!IsControllerConnected())
                return 0;

            State currentState = xboxController.GetState();
            GamepadButtonFlags[] buttonArray = (GamepadButtonFlags[])Enum.GetValues(typeof(GamepadButtonFlags));
            int pressedCount = 0;

            for(int i=1; i<buttonArray.Length; i++)
            {
                if (currentState.Gamepad.Buttons.HasFlag(buttonArray[i]))
                    pressedCount++;
            }

            if (buttonData.LeftTrigger >= LEFT_TRIGGER_THRESHOLD)
                pressedCount++;

            if (buttonData.RightTrigger >= RIGHT_TRIGGER_THRESHOLD)
                pressedCount++;

            return pressedCount;
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
