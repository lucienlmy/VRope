
using System;

/*
 * 
 * created by jeffsturm4nn
 * 
 */

namespace VRope
{
    [Flags]
    public enum TriggerCondition
    {
        NONE = 0, //Disabled Control

        PRESSED = 1,
        HELD = 2,
        RELEASED = 4,

        CUSTOM = 8,
        ANY = (PRESSED | HELD | RELEASED)
    }
}
