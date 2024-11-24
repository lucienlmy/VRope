
using System;
using SharpDX.XInput;

/*
 * 
 * created by jeffsturm4nn
 * 
 */

namespace VRope
{
    public static class XBoxController
    {
        private const char SEPARATOR_CHAR = '+';

        public static byte LEFT_TRIGGER_THRESHOLD = 255;
        public static byte RIGHT_TRIGGER_THRESHOLD = 255;
        public static short LEFT_STICK_THRESHOLD = 32767;
        public static short RIGHT_STICK_THRESHOLD = 32767;

        private static Controller xboxController = null;
        private static State oldControllerState;
        private static State newControllerState;

        private static UserIndex[] UserIndexes = new UserIndex[] { UserIndex.One, UserIndex.Two, UserIndex.Three, UserIndex.Four };
        private static GamepadButtonFlags[] ButtonFlags = (GamepadButtonFlags[])Enum.GetValues(typeof(GamepadButtonFlags));

        public static bool CheckForController()
        {
            bool controllerFound = false;

            for (int i = 0; i < UserIndexes.Length; i++)
            {
                Controller Controller = new Controller(UserIndexes[i]);

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
            int pressedCount = 0;

            //Skip i=0: GamepadButtonFlags.None
            for(int i=1; i<ButtonFlags.Length; i++)
            {
                if (buttonData.Buttons.HasFlag(ButtonFlags[i]))
                    pressedCount++;
            }

            if (buttonData.LeftTrigger >= LEFT_TRIGGER_THRESHOLD)
                pressedCount++;

            if (buttonData.RightTrigger >= RIGHT_TRIGGER_THRESHOLD)
                pressedCount++;

            if (buttonData.LeftThumbX > Gamepad.LeftThumbDeadZone ||
                buttonData.LeftThumbX < -Gamepad.LeftThumbDeadZone)
                pressedCount++;
            if (buttonData.LeftThumbY > Gamepad.LeftThumbDeadZone ||
                buttonData.LeftThumbY < -Gamepad.LeftThumbDeadZone)
                pressedCount++;

            if (buttonData.RightThumbX > Gamepad.RightThumbDeadZone ||
                buttonData.RightThumbX < -Gamepad.RightThumbDeadZone)
                pressedCount++;
            if (buttonData.RightThumbY > Gamepad.RightThumbDeadZone ||
                buttonData.RightThumbY < -Gamepad.RightThumbDeadZone)
                pressedCount++;

            return pressedCount;
        }

        public static bool IsControllerConnected()
        {
            return (xboxController != null && xboxController.IsConnected);
        }

        public static bool WasControllerButtonPressed(ControllerState button)
        {
            return (IsControllerButtonPressed(button, newControllerState) &&
                    !IsControllerButtonPressed(button, oldControllerState));
        }

        public static bool WasControllerButtonReleased(ControllerState button)
        {
            return (IsControllerButtonPressed(button, oldControllerState) &&
                    !IsControllerButtonPressed(button, newControllerState));
        }

        public static bool IsControllerButtonPressed(ControllerState button, State state)
        {
            Gamepad stateData = state.Gamepad;
            Gamepad buttonData = button.buttonData;

            bool isPressed = (button.buttonPressedCount > 0 && 
                (buttonData.Buttons == GamepadButtonFlags.None || stateData.Buttons.HasFlag(buttonData.Buttons)));

            if (buttonData.LeftTrigger > Gamepad.TriggerThreshold)
                isPressed = isPressed && (stateData.LeftTrigger >= buttonData.LeftTrigger);
            if (buttonData.RightTrigger > Gamepad.TriggerThreshold)
                isPressed = isPressed && (stateData.RightTrigger >= buttonData.RightTrigger);


            if (buttonData.LeftThumbX > Gamepad.LeftThumbDeadZone)
                isPressed = isPressed && (stateData.LeftThumbX >= buttonData.LeftThumbX);
            if (buttonData.LeftThumbX < -Gamepad.LeftThumbDeadZone)
                isPressed = isPressed && (stateData.LeftThumbX <= buttonData.LeftThumbX);

            if (buttonData.LeftThumbY > Gamepad.LeftThumbDeadZone)
                isPressed = isPressed && (stateData.LeftThumbY >= buttonData.LeftThumbY);
            if (buttonData.LeftThumbY < -Gamepad.LeftThumbDeadZone)
                isPressed = isPressed && (stateData.LeftThumbY <= buttonData.LeftThumbY);

            if (buttonData.RightThumbX > Gamepad.RightThumbDeadZone)
                isPressed = isPressed && (stateData.RightThumbX >= buttonData.RightThumbX);
            if (buttonData.RightThumbX < -Gamepad.RightThumbDeadZone)
                isPressed = isPressed && (stateData.RightThumbX <= buttonData.RightThumbX);

            if (buttonData.RightThumbY > Gamepad.RightThumbDeadZone)
                isPressed = isPressed && (stateData.RightThumbY >= buttonData.RightThumbY);
            if (buttonData.RightThumbY < -Gamepad.RightThumbDeadZone)
                isPressed = isPressed && (stateData.RightThumbY <= buttonData.RightThumbY);

            return isPressed;
        }

        public static bool IsControllerButtonPressed(ControllerState button)
        {
            return IsControllerButtonPressed(button, newControllerState);
        }

        //public static bool IsControllerButtonReleased(ControllerState buttonData, State state)
        //{
        //    Gamepad stateData = state.Gamepad;

        //    bool isPressed = !stateData.Buttons.HasFlag(buttonData.Buttons);

        //    if (buttonData.LeftTrigger > 0)
        //        isPressed = isPressed && (stateData.LeftTrigger < buttonData.LeftTrigger);

        //    if (buttonData.RightTrigger > 0)
        //        isPressed = isPressed && (stateData.RightTrigger < buttonData.RightTrigger);

        //    return isPressed;
        //}

        //public static bool IsControllerButtonReleased(ControllerState buttonData)
        //{
        //    return IsControllerButtonReleased(buttonData, newControllerState);
        //}

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

        public static ControllerState TranslateButtonStringToButtonData(String buttonData)
        {
            if (buttonData == null || buttonData.Length == 0)
                return new ControllerState();

            buttonData = buttonData.Replace(" ", "");

            Gamepad resultData = new Gamepad();
            bool hasInvalidKey = false;

            String[] buttonStrings = buttonData.Split(SEPARATOR_CHAR);

            for (int i = 0; i < buttonStrings.Length; i++)
            {
                String buttonString = buttonStrings[i];

                if (buttonString == "LeftTrigger")
                {
                    resultData.LeftTrigger = XBoxController.LEFT_TRIGGER_THRESHOLD;
                }
                else if (buttonString == "RightTrigger")
                {
                    resultData.RightTrigger = XBoxController.RIGHT_TRIGGER_THRESHOLD;
                }

                else if (buttonString == "LeftStickUp")
                {
                    resultData.LeftThumbY = XBoxController.LEFT_STICK_THRESHOLD;
                }
                else if (buttonString == "LeftStickDown")
                {
                    resultData.LeftThumbY = (short)-XBoxController.LEFT_STICK_THRESHOLD;
                }
                else if (buttonString == "LeftStickLeft")
                {
                    resultData.LeftThumbX = (short)-XBoxController.LEFT_STICK_THRESHOLD;
                }
                else if (buttonString == "LeftStickRight")
                {
                    resultData.LeftThumbX = XBoxController.LEFT_STICK_THRESHOLD;
                }

                else if (buttonString == "RightStickUp")
                {
                    resultData.RightThumbY = XBoxController.RIGHT_STICK_THRESHOLD;
                }
                else if (buttonString == "RightStickDown")
                {
                    resultData.RightThumbY = (short)-XBoxController.RIGHT_STICK_THRESHOLD;
                }
                else if (buttonString == "RightStickLeft")
                {
                    resultData.RightThumbX = (short)-XBoxController.RIGHT_STICK_THRESHOLD;
                }
                else if (buttonString == "RightStickRight")
                {
                    resultData.RightThumbX = XBoxController.RIGHT_STICK_THRESHOLD;
                }

                else if(Enum.IsDefined(typeof(GamepadButtonFlags), buttonString))
                {
                    GamepadButtonFlags buttonFlag = (GamepadButtonFlags)Enum.Parse(typeof(GamepadButtonFlags), buttonString);

                    resultData.Buttons |= buttonFlag;
                }
                else
                {
                    hasInvalidKey = true;
                    break;
                }
            }

            if (!hasInvalidKey)
                return new ControllerState(resultData, XBoxController.GetButtonPressedCount(resultData));
            else
                return new ControllerState(resultData, -1);
        }

    }
}
