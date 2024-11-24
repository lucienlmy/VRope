
using SharpDX.XInput;
using System;

/*
 * 
 * created by jeffsturm4nn
 * 
 */

namespace VRope
{
    public class ControllerState
    {
        public Gamepad buttonData;
        public int buttonPressedCount = 0;

        public ControllerState() { }

        public ControllerState(Gamepad buttonData, int buttonPressedCount)
        {
            this.buttonData = buttonData;
            this.buttonPressedCount = buttonPressedCount;
        }
    }

    public class ControlButton
    {
        public String name = "";
        public ControllerState state;
        public Action callback = null;
        public TriggerCondition condition = TriggerCondition.NONE;

        public ControlButton()
        {
        }

        public ControlButton(string name, ControllerState state, Action callback, TriggerCondition condition)
        {
            this.name = name;
            this.state = state;
            this.callback = callback;
            this.condition = condition;
        }
    }
}
