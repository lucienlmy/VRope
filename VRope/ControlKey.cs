using System;
using System.Collections.Generic;
using System.Windows.Forms;

/*
 * 
 * created by jeffsturm4nn
 * 
 */

namespace VRope
{
    public class ControlKey
    {
        public String name = "";
        public List<Keys> keys = null;
        public Action callback = null;
        public TriggerCondition condition = TriggerCondition.NONE;
        public bool wasPressed = false;

        public ControlKey(string name, List<Keys> keys, Action callback, TriggerCondition condition)
        {
            this.name = name;
            this.keys = keys;
            this.callback = callback;
            this.condition = condition;
            this.wasPressed = false;
        }
    }
}
