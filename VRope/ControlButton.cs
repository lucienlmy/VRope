
using SharpDX.XInput;
using System;

namespace VRope
{
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
