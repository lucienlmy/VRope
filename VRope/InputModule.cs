using System;
using System.Collections.Generic;
using System.Windows.Forms;

using GTA;

using static VRope.Core;
using static VRope.RopeModule;
using static VRope.TransportModule;
using static VRope.ForceModule;

/*
 * 
 * created by jeffsturm4nn
 * 
 */

namespace VRope
{
    public static class InputModule
    {

        public static void InitControlKeysFromConfig(ScriptSettings settings)
        {
            RegisterControlKey("ToggleModActiveKey", settings.GetValue<String>("CONTROL_KEYBOARD", "ToggleModActiveKey", "None"),
                (Action)ToggleModActiveProc, TriggerCondition.PRESSED);
            RegisterControlKey("ToggleNoSubtitlesModeKey", settings.GetValue<String>("CONTROL_KEYBOARD", "ToggleNoSubtitlesModeKey", "None"),
                (Action)ToggleNoSubtitlesModeProc, TriggerCondition.PRESSED);

            RegisterControlKey("ToggleDebugInfoKey", settings.GetValue<String>("DEV_STUFF", "ToggleDebugInfoKey", "None"),
            (Action)delegate { DebugMode = !DebugMode; }, TriggerCondition.PRESSED);
           
            //RegisterControlKey("ToggleTestAction1Key", settings.GetValue<String>("DEV_STUFF", "ToggleTestAction1Key", "None"),
            //(Action)delegate { TestAction1 = !TestAction1; }, TriggerCondition.PRESSED);
            //RegisterControlKey("ToggleTestAction2Key", settings.GetValue<String>("DEV_STUFF", "ToggleTestAction2Key", "None"),
            //(Action)delegate { ThisIsATestFunction(); }, TriggerCondition.PRESSED);


            if (ENABLE_ROPE_MODULE)
            {
                RegisterControlKey("MultipleObjectSelectionKey", settings.GetValue<String>("CONTROL_KEYBOARD", "MultipleObjectSelectionKey", "None"),
                        (Action)MultipleObjectSelectionProc, TriggerCondition.PRESSED);

                RegisterControlKey("AttachPlayerToEntityKey", settings.GetValue<String>("CONTROL_KEYBOARD", "AttachPlayerToEntityKey", "None"),
                    (Action)AttachPlayerToEntityProc, TriggerCondition.PRESSED);
                RegisterControlKey("AttachEntityToEntityKey", settings.GetValue<String>("CONTROL_KEYBOARD", "AttachEntityToEntityKey", "None"),
                    (Action)AttachEntityToEntityRopeProc, TriggerCondition.PRESSED);
                RegisterControlKey("DeleteLastHookKey", settings.GetValue<String>("CONTROL_KEYBOARD", "DeleteLastHookKey", "None"),
                    (Action)DeleteLastHookProc, TriggerCondition.PRESSED);
                RegisterControlKey("DeleteFirstHookKey", settings.GetValue<String>("CONTROL_KEYBOARD", "DeleteFirstHookKey", "None"),
                    (Action)DeleteFirstHookProc, TriggerCondition.PRESSED);
                RegisterControlKey("DeleteAllHooksKey", settings.GetValue<String>("CONTROL_KEYBOARD", "DeleteAllHooksKey", "None"),
                    (Action)DeleteAllHooks, TriggerCondition.PRESSED);

                //registerControlKey("ToggleSolidRopesKey", settings.GetValue<String>("CONTROL_KEYBOARD", "ToggleSolidRopesKey", "None"),
                //    (Action)ToggleSolidRopesProc, TriggerCondition.PRESSED);
                //registerControlKey("IncreaseMinRopeLengthKey", settings.GetValue<String>("CONTROL_KEYBOARD", "IncreaseMinRopeLengthKey", "None"),
                //    (Action)(() => IncrementMinRopeLength(false)), TriggerCondition.HELD);
                //registerControlKey("DecreaseMinRopeLengthKey", settings.GetValue<String>("CONTROL_KEYBOARD", "DecreaseMinRopeLengthKey", "None"),
                //    (Action)(() => IncrementMinRopeLength(true)), TriggerCondition.HELD);

                RegisterControlKey("WindLastHookRopeKey", settings.GetValue<String>("CONTROL_KEYBOARD", "WindLastHookRopeKey", "None"),
                    (Action)(() => SetLastHookRopeWindingProc(true)), TriggerCondition.HELD);
                RegisterControlKey("WindAllHookRopesKey", settings.GetValue<String>("CONTROL_KEYBOARD", "WindAllHookRopesKey", "None"),
                    (Action)(() => SetAllHookRopesWindingProc(true)), TriggerCondition.HELD);
                RegisterControlKey("UnwindLastHookRopeKey", settings.GetValue<String>("CONTROL_KEYBOARD", "UnwindLastHookRopeKey", "None"),
                    (Action)(() => SetLastHookRopeUnwindingProc(true)), TriggerCondition.HELD);
                RegisterControlKey("UnwindAllHookRopesKey", settings.GetValue<String>("CONTROL_KEYBOARD", "UnwindAllHookRopesKey", "None"),
                    (Action)(() => SetAllHookRopesUnwindingProc(true)), TriggerCondition.HELD); 
            }


            if (ENABLE_FORCE_MODULE)
            {
                RegisterControlKey("ApplyForceKey", settings.GetValue<String>("CONTROL_KEYBOARD", "ApplyForceKey", "None"),
                        (Action)(() => ApplyForceAtAimedProc(false)), (ContinuousForce ? TriggerCondition.HELD : TriggerCondition.PRESSED));
                RegisterControlKey("ApplyInvertedForceKey", settings.GetValue<String>("CONTROL_KEYBOARD", "ApplyInvertedForceKey", "None"),
                    (Action)(() => ApplyForceAtAimedProc(true)), (ContinuousForce ? TriggerCondition.HELD : TriggerCondition.PRESSED));
                RegisterControlKey("IncreaseForceKey", settings.GetValue<String>("CONTROL_KEYBOARD", "IncreaseForceKey", "None"),
                    (Action)(() => IncrementForceProc(false)), TriggerCondition.HELD);
                RegisterControlKey("DecreaseForceKey", settings.GetValue<String>("CONTROL_KEYBOARD", "DecreaseForceKey", "None"),
                    (Action)(() => IncrementForceProc(true)), TriggerCondition.HELD);
                RegisterControlKey("ApplyForceObjectPairKey", settings.GetValue<String>("CONTROL_KEYBOARD", "ApplyForceObjectPairKey", "None"),
                    (Action)ApplyForceObjectPairProc, TriggerCondition.PRESSED);
                RegisterControlKey("ApplyForcePlayerKey", settings.GetValue<String>("CONTROL_KEYBOARD", "ApplyForcePlayerKey", "None"),
                    (Action)ApplyForcePlayerProc, (ContinuousForce ? TriggerCondition.HELD : TriggerCondition.PRESSED));

                RegisterControlKey("ToggleBalloonHookModeKey", settings.GetValue<String>("CONTROL_KEYBOARD", "ToggleBalloonHookModeKey", "None"),
                    (Action)ToggleBalloonHookModeProc, TriggerCondition.PRESSED);
                RegisterControlKey("IncreaseBalloonUpForceKey", settings.GetValue<String>("CONTROL_KEYBOARD", "IncreaseBalloonUpForceKey", "None"),
                    (Action)(() => IncrementBalloonUpForce(false)), TriggerCondition.HELD);
                RegisterControlKey("DecreaseBalloonUpForceKey", settings.GetValue<String>("CONTROL_KEYBOARD", "DecreaseBalloonUpForceKey", "None"),
                    (Action)(() => IncrementBalloonUpForce(true)), TriggerCondition.HELD); 
            }


            if (ENABLE_TRANSPORT_MODULE)
            {
                RegisterControlKey("AttachMultiTransportHooksKey", settings.GetValue<String>("CONTROL_KEYBOARD", "AttachMultiTransportHooksKey", "None"),
                        (Action)delegate { AttachTransportHooksProc(TransportHookType.MULTIPLE); }, TriggerCondition.PRESSED);
                RegisterControlKey("AttachSingleTransportHookKey", settings.GetValue<String>("CONTROL_KEYBOARD", "AttachSingleTransportHookKey", "None"),
                    (Action)delegate { AttachTransportHooksProc(TransportHookType.SINGLE); }, TriggerCondition.PRESSED);
                RegisterControlKey("NextTransportHookFilterKey", settings.GetValue<String>("CONTROL_KEYBOARD", "NextTransportHookFilterKey", "None"),
                    (Action)delegate { CycleTransportHookFilterProc(true); }, TriggerCondition.PRESSED);
                RegisterControlKey("NextTransportHookModeKey", settings.GetValue<String>("CONTROL_KEYBOARD", "NextTransportHookModeKey", "None"),
                    (Action)delegate { CycleTransportHookModeProc(true); }, TriggerCondition.PRESSED); 
            }
            
        }

        public static void InitControllerButtonsFromConfig(ScriptSettings settings)
        {
            RegisterControlButton("AttachPlayerToEntityButton",
                settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "AttachPlayerToEntityButton", "None"),
                (Action)AttachPlayerToEntityProc, TriggerCondition.PRESSED);
            RegisterControlButton("AttachEntityToEntityButton",
                settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "AttachEntityToEntityButton", "None"),
                (Action)(() => AttachEntityToEntityProc(false)), TriggerCondition.PRESSED);
            RegisterControlButton("DeleteLastHookButton",
                settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "DeleteLastHookButton", "None"),
                (Action)DeleteLastHookProc, TriggerCondition.PRESSED);
            RegisterControlButton("DeleteAllHooksButton",
                settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "DeleteAllHooksButton", "None"),
                (Action)DeleteAllHooks, TriggerCondition.PRESSED);
            RegisterControlButton("DeleteFirstHookButton",
                settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "DeleteFirstHookButton", "None"),
                (Action)DeleteFirstHookProc, TriggerCondition.PRESSED);

            RegisterControlButton("WindLastHookRopeButton",
                settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "WindLastHookRopeButton", "None"),
                (Action)delegate { SetLastHookRopeWindingProc(true); }, TriggerCondition.HELD);
            RegisterControlButton("WindAllHookRopesButton",
                settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "WindAllHookRopesButton", "None"),
                (Action)delegate { SetAllHookRopesWindingProc(true); }, TriggerCondition.HELD);
            RegisterControlButton("UnwindLastHookRopeButton",
                settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "UnwindLastHookRopeButton", "None"),
                (Action)delegate { SetLastHookRopeUnwindingProc(true); }, TriggerCondition.HELD);
            RegisterControlButton("UnwindAllHookRopesButton",
                settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "UnwindAllHookRopesButton", "None"),
                (Action)delegate { SetAllHookRopesUnwindingProc(true); }, TriggerCondition.HELD);

            RegisterControlButton("ApplyForceButton",
                settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "ApplyForceButton", "None"),
                (Action)delegate { ApplyForceAtAimedProc(false); }, (ContinuousForce ? TriggerCondition.HELD : TriggerCondition.PRESSED));
            RegisterControlButton("ApplyInvertedForceButton",
                settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "ApplyInvertedForceButton", "None"),
                (Action)delegate { ApplyForceAtAimedProc(true); }, (ContinuousForce ? TriggerCondition.HELD : TriggerCondition.PRESSED));

            RegisterControlButton("IncreaseForceButton",
                settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "IncreaseForceButton", "None"),
                (Action)delegate { IncrementForceProc(false, true); }, TriggerCondition.HELD);
            RegisterControlButton("DecreaseForceButton",
                settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "DecreaseForceButton", "None"),
                (Action)delegate { IncrementForceProc(true, true); }, TriggerCondition.HELD);
            RegisterControlButton("ApplyForceObjectPairButton",
                settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "ApplyForceObjectPairButton", "None"),
                (Action)ApplyForceObjectPairProc, TriggerCondition.PRESSED);

            RegisterControlButton("ApplyForcePlayerButton",
                settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "ApplyForcePlayerButton", "None"),
                (Action)ApplyForcePlayerProc, (ContinuousForce ? TriggerCondition.HELD : TriggerCondition.PRESSED));

            RegisterControlButton("WindLastHookRopeButton",
                settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "WindLastHookRopeButton", "None"),
                (Action)delegate { SetLastHookRopeWindingProc(false); }, TriggerCondition.RELEASED);
            RegisterControlButton("WindAllHookRopesButton",
                settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "WindAllHookRopesButton", "None"),
                (Action)delegate { SetAllHookRopesWindingProc(false); }, TriggerCondition.RELEASED);
            RegisterControlButton("UnwindLastHookRopeButton",
                settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "UnwindLastHookRopeButton", "None"),
                (Action)delegate { SetLastHookRopeUnwindingProc(false); }, TriggerCondition.RELEASED);
            RegisterControlButton("UnwindAllHookRopesButton",
                settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "UnwindAllHookRopesButton", "None"),
                (Action)delegate { SetAllHookRopesUnwindingProc(false); }, TriggerCondition.RELEASED);

            RegisterControlButton("ToggleBalloonHookModeButton", settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "ToggleBalloonHookModeButton", "None"),
                (Action)ToggleBalloonHookModeProc, TriggerCondition.PRESSED);

            RegisterControlButton("IncreaseBalloonUpForceButton", settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "IncreaseBalloonUpForceButton", "None"),
                (Action)(() => IncrementBalloonUpForce(false, true)), TriggerCondition.HELD);
            RegisterControlButton("DecreaseBalloonUpForceButton", settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "DecreaseBalloonUpForceButton", "None"),
                (Action)(() => IncrementBalloonUpForce(true, true)), TriggerCondition.HELD);

            RegisterControlButton("MultipleObjectSelectionButton", settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "MultipleObjectSelectionButton", "None"),
                (Action)MultipleObjectSelectionProc, TriggerCondition.PRESSED);

            RegisterControlButton("AttachMultiTransportHooksButton", settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "AttachMultiTransportHooksButton", "None"),
                (Action)delegate { AttachTransportHooksProc(TransportHookType.MULTIPLE); }, TriggerCondition.PRESSED);
            RegisterControlKey("AttachSingleTransportHookButton", settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "AttachSingleTransportHookButton", "None"),
                (Action)delegate { AttachTransportHooksProc(TransportHookType.SINGLE); }, TriggerCondition.PRESSED);

            RegisterControlButton("NextTransportHookFilterButton", settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "NextTransportHookFilterButton", "None"),
                (Action)delegate { CycleTransportHookFilterProc(true); }, TriggerCondition.PRESSED);

            RegisterControlButton("NextTransportHookModeButton", settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "NextTransportHookModeButton", "None"),
                (Action)delegate { CycleTransportHookModeProc(true); }, TriggerCondition.PRESSED);
        }


        public static void RegisterControlKey(String name, String keyData, Action callback, TriggerCondition condition)
        {
            List<Keys> keys = Keyboard.TranslateKeyDataToKeyList(keyData);

            if (keys.Count == 0)
            {
                UI.Notify("VRope ControlKey Error:\n Key combination for \"" + name + "\" is invalid. \nThe control was disabled.");
                return;
            }

            ControlKeys.Add(new ControlKey(name, keys, callback, condition));
        }

        public static void RegisterControlButton(String name, String buttonData, Action callback, TriggerCondition condition)
        {
            ControllerState buttonState = XBoxController.TranslateButtonStringToButtonData(buttonData);

            if (buttonState.buttonPressedCount == -1)
            {
                UI.Notify("VRope ControlButton Error:\n Button combination for \"" + name + "\" is invalid. \nThe control was disabled.");
                return;
            }

            ControlButtons.Add(new ControlButton(name, buttonState, callback, condition));
        }

        public static void CheckForKeysHeldDown()
        {
            for (int i = 0; i < ControlKeys.Count; i++)
            {
                var controlKey = ControlKeys[i];

                if (controlKey.condition == TriggerCondition.HELD && Keyboard.IsKeyListPressed(controlKey.keys))
                {
                    controlKey.callback.Invoke();
                    controlKey.wasPressed = true;
                    break;
                }
            }
        }

        public static void CheckForKeysReleased()
        {
            for (int i = 0; i < ControlKeys.Count; i++)
            {
                var control = ControlKeys[i];

                if (control.wasPressed)
                {
                    if (Keyboard.IsKeyListUp(control.keys))
                    {
                        if (control.name == "WindLastHookRopeKey") SetLastHookRopeWindingProc(false);
                        else if (control.name == "WindAllHookRopesKey") SetAllHookRopesWindingProc(false);
                        else if (control.name == "UnwindLastHookRopeKey") SetLastHookRopeUnwindingProc(false);
                        else if (control.name == "UnwindAllHookRopesKey") SetAllHookRopesUnwindingProc(false);

                        else if (control.condition.HasFlag(TriggerCondition.RELEASED)) control.callback.Invoke();

                        control.wasPressed = false;
                        //break;
                    }
                }
            }
        }

        public static void ProcessXBoxControllerInput()
        {
            XBoxController.UpdateStateBegin();

            for (int i = 0; i < ControlButtons.Count; i++)
            {
                var controlButton = ControlButtons[i];
                ControllerState button = controlButton.state;
                TriggerCondition condition = controlButton.condition;

                if ((condition == TriggerCondition.PRESSED && XBoxController.WasControllerButtonPressed(button)) ||
                    (condition == TriggerCondition.RELEASED && XBoxController.WasControllerButtonReleased(button)) ||
                    (condition == TriggerCondition.HELD && XBoxController.IsControllerButtonPressed(button)) ||
                    (condition == TriggerCondition.ANY))
                {
                    controlButton.callback.Invoke();
                    break;
                }
            }

            XBoxController.UpdateStateEnd();
        }



    }
}
