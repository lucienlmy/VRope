
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using GTA;
using GTA.Math;
using GTA.Native;

using SharpDX.XInput;

/*
Bone1Chest = 24818
Bone2Chest = 20719
Bone3LeftArm = 61007
Bone4RightArm = 24818
*/

namespace VRope
{
    public class ScriptMain : Script
    {
        public const String MOD_NAME = "VRope";
        public const String MOD_DEVELOPER = "jeffsturm4nn"; // :D
        public const int VERSION_MINOR = 0;
        public const int VERSION_BUILD = 10;
        public const String VERSION_SUFFIX = "a DevBuild";

        private const int UPDATE_INTERVAL = 13;
        private const float UPDATE_FPS = (1000f / UPDATE_INTERVAL);

        private bool ENABLE_XBOX_CONTROLLER_INPUT;
        private bool FREE_RANGE_MODE;
        private String CONFIG_FILE_NAME;
        private float MIN_ROPE_LENGTH; 
        private float MAX_HOOK_CREATION_DISTANCE; 
        private float MAX_HOOKED_ENTITY_DISTANCE;

        private bool CONTINUOUS_FORCE;
        private float FORCE_INCREMENT_VALUE;
        private float FORCE_SCALE_FACTOR = 1.4f;

        private const int INIT_HOOK_LIST_CAPACITY = 500;
        private const float  MAX_HOOKED_PED_SPEED = 0.57f;
        private const int PED_RAGDOLL_DURATION = 7000;
        private const char SEPARATOR_CHAR = '+';

        private SubtitleQueue subQueue = new SubtitleQueue();

        private bool ModActive = false;
        public bool ModRunning = false;
        private bool FirstTime = true;
        private bool DebugMode = false;
        
        private Model targetPropModel;

        private String DebugInfo = "";
        private String GlobalSubtitle = ""; 

        private List<HookGroup> hooks = new List<HookGroup>(INIT_HOOK_LIST_CAPACITY);
        private HookGroup ropeHook = new HookGroup();
        private HookGroup forceHook = new HookGroup();

        private List<ControlKey> controlKeys = new List<ControlKey>(30);
        private List<ControlButton> controlButtons = new List<ControlButton>(30);

        private RopeType EntityToEntityHookRopeType;
        private RopeType PlayerToEntityHookRopeType;

        private float ForceMagnitude;

        public ScriptMain()
        {
            try
            {
                CONFIG_FILE_NAME = (Directory.GetCurrentDirectory() + "\\scripts\\VRope.ini");

                ProcessConfigFile();

                SortKeyTuples();

                if (ENABLE_XBOX_CONTROLLER_INPUT)
                {
                    XBoxController.CheckForController();
                    SortButtonTuples();
                }

                targetPropModel = new Model("prop_golf_ball"); //We don't talk about this. Keep scrolling.

                Tick += OnTick;
                KeyDown += OnKeyDown;
                KeyUp += OnKeyUp;
                
                Interval = UPDATE_INTERVAL;
            }
            catch(Exception exc)
            {
                UI.Notify("VRope Init Error:\n" + exc.ToString());
            }
        }

        ~ScriptMain()
        {
            DeleteAllHooks();
            ModActive = false;
            ModRunning = false;
        }

        public String GetModVersion()
        {
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

            return ("v" + version.Major + "." + VERSION_MINOR + "." + VERSION_BUILD + "." + version.Revision + VERSION_SUFFIX);
        }

        private ControllerState TranslateButtonStringToButtonData(String buttonData)
        {
            if (buttonData == null || buttonData.Length == 0)
                return new ControllerState();

            buttonData = buttonData.Replace(" ", "");

            Gamepad resultData = new Gamepad();

            String[] buttonStrings = buttonData.Split(SEPARATOR_CHAR);

            for (int i=0; i<buttonStrings.Length; i++)
            {
                String buttonString = buttonStrings[i];

                if (buttonString == "LeftTrigger")
                {
                    resultData.LeftTrigger = XBoxController.LEFT_TRIGGER_THRESHOLD;
                }
                else if(buttonString == "RightTrigger")
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

                else
                {
                    GamepadButtonFlags buttonFlag = (GamepadButtonFlags)Enum.Parse(typeof(GamepadButtonFlags), buttonStrings[i]);

                    resultData.Buttons |= buttonFlag;
                }
            }

            return new ControllerState(resultData, XBoxController.GetButtonPressedCount(resultData));
        }
        
        private List<Keys> TranslateKeyDataToKeyList(String keyData)
        {
            if (keyData == null || keyData.Length == 0)
                return new List<Keys>(0);

            keyData = keyData.Replace(" ", "");

            List<Keys> resultList = new List<Keys>(4);

            String[] keyStrings = keyData.Split(SEPARATOR_CHAR);

            for (int i = 0; i < keyStrings.Length; i++)
            {
                String keyString = keyStrings[i];

                if(keyString == "WeaponPrev")
                {
                    resultList.Add(Keyboard.MOUSE_WHEEL_UP_KEY);
                }
                else if(keyString == "WeaponNext")
                {
                    resultList.Add(Keyboard.MOUSE_WHEEL_DOWN_KEY);
                }
                else
                {
                    Keys key = (Keys)Enum.Parse(typeof(Keys), keyStrings[i]);

                    resultList.Add(key);
                }
            }

            return resultList;
        }


        private void SortKeyTuples()
        {
            for(int i=0; i<controlKeys.Count; i++)
            {
                for(int j=0; j<controlKeys.Count-1; j++)
                {
                    if(controlKeys[j].keys.Count < controlKeys[j+1].keys.Count)
                    {
                        var keyPair = controlKeys[j];

                        controlKeys[j] = controlKeys[j+1];
                        controlKeys[j + 1] = keyPair;
                    }
                }
            }
        }

        private void SortButtonTuples()
        {
            for (int i = 0; i < controlButtons.Count; i++)
            {
                for (int j = 0; j < controlButtons.Count - 1; j++)
                {
                    if (controlButtons[j].state.buttonPressedCount < controlButtons[j + 1].state.buttonPressedCount)
                    {
                        var buttonPair = controlButtons[j];

                        controlButtons[j] = controlButtons[j + 1];
                        controlButtons[j + 1] = buttonPair;
                    }
                }
            }
        }

        private void InitControlKeysFromConfig(ScriptSettings settings)
        {
            controlKeys.Add(new ControlKey("ToggleModActiveKey", TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "ToggleModActiveKey", "None")),
                (Action)ToggleModActiveProc, TriggerCondition.PRESSED));
            controlKeys.Add(new ControlKey("AttachPlayerToEntityKey", TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "AttachPlayerToEntityKey", "None")),
                (Action)AttachPlayerToEntityProc, TriggerCondition.PRESSED));
            controlKeys.Add(new ControlKey("AttachEntityToEntityKey", TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "AttachEntityToEntityKey", "None")),
                (Action)AttachEntityToEntityProc, TriggerCondition.PRESSED));
            controlKeys.Add(new ControlKey("DeleteLastHookKey", TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "DeleteLastHookKey", "None")),
                (Action)DeleteLastHookProc, TriggerCondition.PRESSED));
            controlKeys.Add(new ControlKey("DeleteAllHooksKey", TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "DeleteAllHooksKey", "None")),
                (Action)DeleteAllHooks, TriggerCondition.PRESSED));

            controlKeys.Add(new ControlKey("WindLastHookRopeKey", TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "WindLastHookRopeKey", "None")),
                (Action)(() => SetLastHookRopeWindingProc(true)), TriggerCondition.HELD));
            controlKeys.Add(new ControlKey("WindAllHookRopesKey", TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "WindAllHookRopesKey", "None")),
                (Action)(() => SetAllHookRopesWindingProc(true)), TriggerCondition.HELD));
            controlKeys.Add(new ControlKey("UnwindLastHookRopeKey", TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "UnwindLastHookRopeKey", "None")),
                (Action)(() => SetLastHookRopeUnwindingProc(true)), TriggerCondition.HELD));
            controlKeys.Add(new ControlKey("UnwindAllHookRopesKey", TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "UnwindAllHookRopesKey", "None")),
                (Action)(() => SetAllHookRopesUnwindingProc(true)), TriggerCondition.HELD));

            controlKeys.Add(new ControlKey("ApplyForceKey", TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "ApplyForceKey", "None")),
                (Action)(() => ApplyForceAtAimedProc(false)), (CONTINUOUS_FORCE ? TriggerCondition.HELD : TriggerCondition.PRESSED)));
            controlKeys.Add(new ControlKey("ApplyInvertedForceKey", TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "ApplyInvertedForceKey", "None")),
                (Action)(() => ApplyForceAtAimedProc(true)), (CONTINUOUS_FORCE ? TriggerCondition.HELD : TriggerCondition.PRESSED)));

            controlKeys.Add(new ControlKey("IncreaseForceKey", TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "IncreaseForceKey", "None")),
                (Action)(()=> IncrementForceValueProc(false)), TriggerCondition.HELD));
            controlKeys.Add(new ControlKey("DecreaseForceKey", TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "DecreaseForceKey", "None")),
                (Action)(() => IncrementForceValueProc(true)), TriggerCondition.HELD));
            controlKeys.Add(new ControlKey("ApplyForceObjectPairKey", TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "ApplyForceObjectPairKey", "None")),
                (Action)ApplyForceObjectPairProc, TriggerCondition.PRESSED));
            controlKeys.Add(new ControlKey("ApplyForcePlayerKey", TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "ApplyForcePlayerKey", "None")),
                (Action)ApplyForcePlayerProc, TriggerCondition.NONE)); //(CONTINUOUS_FORCE ? TriggerCondition.HELD : TriggerCondition.PRESSED)));

            controlKeys.Add(new ControlKey("ToggleDebugInfoKey", TranslateKeyDataToKeyList(settings.GetValue<String>("DEV_STUFF", "ToggleDebugInfoKey", "None")),
                (Action) delegate { DebugMode = !DebugMode; }, TriggerCondition.PRESSED));
        }

        private void InitControllerButtonsFromConfig(ScriptSettings settings)
        {
            controlButtons.Add(new ControlButton("AttachPlayerToEntityButton", 
                TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "AttachPlayerToEntityButton", "None")),
                (Action)AttachPlayerToEntityProc, TriggerCondition.PRESSED));
            controlButtons.Add(new ControlButton("AttachEntityToEntityButton", 
                TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "AttachEntityToEntityButton", "None")),
                (Action)AttachEntityToEntityProc, TriggerCondition.PRESSED));
            controlButtons.Add(new ControlButton("DeleteLastHookButton",
                TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "DeleteLastHookButton", "None")),
                (Action)DeleteLastHookProc, TriggerCondition.PRESSED));
            controlButtons.Add(new ControlButton("DeleteAllHooksButton",
                TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "DeleteAllHooksButton", "None")),
                (Action)DeleteAllHooks, TriggerCondition.PRESSED));

            controlButtons.Add(new ControlButton("WindLastHookRopeButton",
                TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "WindLastHookRopeButton", "None")),
                (Action)delegate { SetLastHookRopeWindingProc(true); }, TriggerCondition.HELD));
            controlButtons.Add(new ControlButton("WindAllHookRopesButton",
                TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "WindAllHookRopesButton", "None")),
                (Action)delegate { SetAllHookRopesWindingProc(true); }, TriggerCondition.HELD));
            controlButtons.Add(new ControlButton("UnwindLastHookRopeButton",
                TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "UnwindLastHookRopeButton", "None")),
                (Action)delegate { SetLastHookRopeUnwindingProc(true); }, TriggerCondition.HELD));
            controlButtons.Add(new ControlButton("UnwindAllHookRopesButton",
                TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "UnwindAllHookRopesButton", "None")),
                (Action)delegate { SetAllHookRopesUnwindingProc(true); }, TriggerCondition.HELD));

            controlButtons.Add(new ControlButton("ApplyForceButton",
                TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "ApplyForceButton", "None")),
                (Action)delegate { ApplyForceAtAimedProc(false); }, (CONTINUOUS_FORCE ? TriggerCondition.HELD : TriggerCondition.PRESSED)));
            controlButtons.Add(new ControlButton("ApplyInvertedForceButton",
                TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "ApplyInvertedForceButton", "None")),
                (Action)delegate { ApplyForceAtAimedProc(true); }, (CONTINUOUS_FORCE ? TriggerCondition.HELD : TriggerCondition.PRESSED)));

            controlButtons.Add(new ControlButton("IncreaseForceButton",
                TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "IncreaseForceButton", "None")),
                (Action)delegate { IncrementForceValueProc(false); }, TriggerCondition.HELD));
            controlButtons.Add(new ControlButton("DecreaseForceButton",
                TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "DecreaseForceButton", "None")),
                (Action)delegate { IncrementForceValueProc(true); }, TriggerCondition.HELD));
            controlButtons.Add(new ControlButton("ApplyForceObjectPairButton",
                TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "ApplyForceObjectPairButton", "None")),
                (Action)ApplyForceObjectPairProc, TriggerCondition.PRESSED));

            controlButtons.Add(new ControlButton("ApplyForcePlayerButton",
                TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "ApplyForcePlayerButton", "None")),
                (Action)ApplyForcePlayerProc, TriggerCondition.NONE)); //(CONTINUOUS_FORCE ? TriggerCondition.HELD : TriggerCondition.PRESSED) ));

            controlButtons.Add(new ControlButton("WindLastHookRopeButton",
                TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "WindLastHookRopeButton", "None")),
                (Action)delegate { SetLastHookRopeWindingProc(false); }, TriggerCondition.RELEASED));
            controlButtons.Add(new ControlButton("WindAllHookRopesButton",
                TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "WindAllHookRopesButton", "None")),
                (Action)delegate { SetAllHookRopesWindingProc(false); }, TriggerCondition.RELEASED));
            controlButtons.Add(new ControlButton("UnwindLastHookRopeButton",
                TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "UnwindLastHookRopeButton", "None")),
                (Action)delegate { SetLastHookRopeUnwindingProc(false); }, TriggerCondition.RELEASED));
            controlButtons.Add(new ControlButton("UnwindAllHookRopesButton",
                TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "UnwindAllHookRopesButton", "None")),
                (Action)delegate { SetAllHookRopesUnwindingProc(false); }, TriggerCondition.RELEASED));
        }


        private void ProcessConfigFile()
        {
            try
            {
                ScriptSettings settings = ScriptSettings.Load(CONFIG_FILE_NAME);

                if (!File.Exists(CONFIG_FILE_NAME))
                {
                    UI.Notify("VRope File Error:\n" + CONFIG_FILE_NAME + " could not be found.\nAll settings were set to default.", true);
                }

                ModActive = settings.GetValue<bool>("GLOBAL_VARS", "ENABLE_ON_GAME_LOAD", false);
                ENABLE_XBOX_CONTROLLER_INPUT = settings.GetValue<bool>("GLOBAL_VARS", "ENABLE_XBOX_CONTROLLER_INPUT", true);
                FREE_RANGE_MODE = settings.GetValue<bool>("GLOBAL_VARS", "FREE_RANGE_MODE", true);
                
                //UPDATE_INTERVAL = settings.GetValue<int>("GLOBAL_VARS", "UPDATE_INTERVAL", 13);
                MIN_ROPE_LENGTH = (float)settings.GetValue<double>("GLOBAL_VARS", "MIN_ROPE_LENGTH", 1.0);
                MAX_HOOK_CREATION_DISTANCE = (float)settings.GetValue<double>("GLOBAL_VARS", "MAX_HOOK_CREATION_DISTANCE", 70.0);
                MAX_HOOKED_ENTITY_DISTANCE = (float)settings.GetValue<double>("GLOBAL_VARS", "MAX_HOOKED_ENTITY_DISTANCE", 145.0);

                XBoxController.LEFT_TRIGGER_THRESHOLD = settings.GetValue<byte>("CONTROL_XBOX_CONTROLLER", "LEFT_TRIGGER_THRESHOLD", 255);
                XBoxController.RIGHT_TRIGGER_THRESHOLD = settings.GetValue<byte>("CONTROL_XBOX_CONTROLLER", "RIGHT_TRIGGER_THRESHOLD", 255);

                EntityToEntityHookRopeType = settings.GetValue<RopeType>("HOOK_ROPE_TYPES", "EntityToEntityHookRopeType", (RopeType)4);
                PlayerToEntityHookRopeType = settings.GetValue<RopeType>("HOOK_ROPE_TYPES", "PlayerToEntityHookRopeType", (RopeType)3);

                ForceMagnitude = settings.GetValue<int>("FORCE_GUN_VARS", "DEFAULT_FORCE_VALUE", 20);
                FORCE_INCREMENT_VALUE = settings.GetValue<int>("FORCE_GUN_VARS", "FORCE_INCREMENT_VALUE", 1);
                CONTINUOUS_FORCE = settings.GetValue<bool>("FORCE_GUN_VARS", "CONTINUOUS_FORCE", false);

                InitControlKeysFromConfig(settings);

                if(ENABLE_XBOX_CONTROLLER_INPUT)
                    InitControllerButtonsFromConfig(settings);
            }
            catch (Exception e)
            {
                UI.Notify("VRope Config Error: " + e.Message, false);
            }

        }

        private void ProcessHooks()
        {
            Entity playerEntity = Game.Player.Character;
            Vector3 playerPosition = Game.Player.Character.Position;

            if (hooks.Count > 0)
            {
                if (playerEntity.Exists() && playerEntity.IsDead)
                {
                    DeleteAllHooks();
                } 

                for(int i=0; i<hooks.Count; i++)
                {
                    if (hooks[i] == null || !hooks[i].Exists() || !hooks[i].IsValid())
                    {
                        DeleteHookByIndex(i--);
                        continue;
                    }

                    if (!FREE_RANGE_MODE || 
                        (Util.IsPed(hooks[i].entity1) && !Util.IsPlayer(hooks[i].entity1)) || 
                        Util.IsPed(hooks[i].entity2))
                    {
                        if ((hooks[i].entity1 != playerEntity &&
                            playerPosition.DistanceTo(hooks[i].entity1.Position) > MAX_HOOKED_ENTITY_DISTANCE) ||
                            (playerPosition.DistanceTo(hooks[i].entity2.Position) > MAX_HOOKED_ENTITY_DISTANCE))
                        {
                            DeleteHookByIndex(i--);
                            continue;
                        } 
                    }

                    ProcessPedsInHook(i);
                }
            }
        }

        private void ProcessPedsInHook(int hookIndex)
        {
            Entity entity1 = hooks[hookIndex].entity1;
            Entity entity2 = hooks[hookIndex].entity2;
            bool ropeWinding = hooks[hookIndex].isWinding;
            bool ropeUnwinding = hooks[hookIndex].isUnwinding;

            if (Util.IsPed(entity1) && !Util.IsPlayer(entity1))
            {
                Ped ped1 = (Ped)entity1;

                if (ped1.IsAlive)
                {
                    if (!ped1.IsRagdoll)
                    {
                        if (!ped1.IsInAir && !ped1.IsInWater)
                        {
                            if (ped1.Velocity.Length() > MAX_HOOKED_PED_SPEED)
                            {
                                Util.MakePedRagdoll(ped1, PED_RAGDOLL_DURATION);

                                if (ropeWinding)
                                    SetHookRopeWindingByIndex(hookIndex, false);
                                else if (ropeUnwinding)
                                    SetHookRopeUnwindingByIndex(hookIndex, false);

                                RecreateEntityHook(hookIndex);

                                if (ropeWinding)
                                    SetHookRopeWindingByIndex(hookIndex, true);
                                else if (ropeUnwinding)
                                    SetHookRopeUnwindingByIndex(hookIndex, true);
                            }
                        }
                        else
                        {
                            Util.MakePedRagdoll(ped1, PED_RAGDOLL_DURATION);

                            if (ropeWinding)
                                SetHookRopeWindingByIndex(hookIndex, false);
                            else if (ropeUnwinding)
                                SetHookRopeUnwindingByIndex(hookIndex, false);

                            RecreateEntityHook(hookIndex);

                            if (ropeWinding)
                                SetHookRopeWindingByIndex(hookIndex, true);
                            else if (ropeUnwinding)
                                SetHookRopeUnwindingByIndex(hookIndex, true);
                        }
                    }
                }
                else
                {
                    if (ped1.Velocity.Length() < 0.1f && (!ped1.IsInAir && !ped1.IsInWater))
                    {
                        DeleteHookByIndex(hookIndex);
                        return;
                    }
                }
            }

            if (Util.IsPlayer(entity2))
            {
                Ped ped2 = (Ped)entity2;

                if (ped2.IsAlive)
                {
                    if (!ped2.IsRagdoll)
                    {
                        if (!ped2.IsInAir && !ped2.IsInWater)
                        {
                            if (ped2.Velocity.Length() > MAX_HOOKED_PED_SPEED)
                            {
                                Util.MakePedRagdoll(ped2, PED_RAGDOLL_DURATION);
                                RecreateEntityHook(hookIndex);
                            }
                        }
                        else
                        {
                            Util.MakePedRagdoll(ped2, PED_RAGDOLL_DURATION);
                            RecreateEntityHook(hookIndex);
                        }
                    }
                }
                else
                {
                    if (ped2.Velocity.Length() < 0.1f && (!ped2.IsInAir && !ped2.IsInWater))
                    {
                        DeleteHookByIndex(hookIndex);
                    }
                }
            }
        }


        public bool IsEntityHooked(Entity entity)
        {
            if (entity == null || !entity.Exists())
                return false;

            for(int i=0; i<hooks.Count; i++)
            {
                if ((hooks[i].entity1 != null && hooks[i].entity1.Equals(entity)) ||
                    (hooks[i].entity2 != null && hooks[i].entity2.Equals(entity)) )
                    return true;
            }

            return false;
        }

        private void UpdateDebugStuff()
        {
            DebugInfo += "Active Hooks: " + hooks.Count;

            if (Game.Player.Exists() && !Game.Player.IsDead &&
                Game.Player.CanControlCharacter)
            {
                if(Game.Player.IsAiming)
                {
                    RaycastResult rayResult = Util.CameraRaycastForward();
                    Entity targetEntity = rayResult.HitEntity;

                    if (rayResult.DitHitEntity && Util.IsValid(targetEntity))
                    {
                        String format = "0.00";
                        Vector3 pos = targetEntity.Position;
                        Vector3 rot = targetEntity.Rotation;
                        Vector3 vel = targetEntity.Velocity;
                        float dist = targetEntity.Position.DistanceTo(Game.Player.Character.Position);
                        float speed = vel.Length();
                        
                        DebugInfo += " | Entity Detected: " + targetEntity.GetType() +
                                    "\nPosition(X:" + pos.X.ToString(format) + ", Y:" + pos.Y.ToString(format) + ", Z:" + pos.Z.ToString(format) + ")" +
                                    "\nRotation(" + rot.X.ToString(format) + ", Y:" + rot.Y.ToString(format) + ", Z:" + rot.Z.ToString(format) + ")" +
                                    "\nVelocity(" + vel.X.ToString(format) + ", Y:" + vel.Y.ToString(format) + ", Z:" + vel.Z.ToString(format) +
                                    ")\nSpeed(" + speed.ToString(format) + ") | Distance(" + dist.ToString(format) + ")\n";
                    }
                }

                if (hooks.Count > 0 && hooks.Last() != null && hooks.Last().Exists())
                {
                    if (hooks.Last().entity1 != null)
                        DebugInfo += "\n| LastHook.E1 Distance: " + Game.Player.Character.Position.DistanceTo(hooks.Last().entity1.Position).ToString("0.00");

                    if (hooks.Last().entity2 != null)
                        DebugInfo += " | LastHook.E2 Distance: " + Game.Player.Character.Position.DistanceTo(hooks.Last().entity2.Position).ToString("0.00");
                }
            }
        }

        //Callback Procedures
        private void ToggleModActiveProc()
        {
            UI.ShowSubtitle((ModActive ? "(VRope Disabled)" : "[VRope Enabled]") + "\n\n\n\n\n");
            ModActive = !ModActive;
            Script.Wait(1200);
        }

        private void AttachPlayerToEntityProc()
        {
            try
            {
                if (Game.Player.Exists() && !Game.Player.IsDead &&
                            Game.Player.CanControlCharacter && Game.Player.IsAiming)
                {
                    RaycastResult rayResult = Util.CameraRaycastForward();

                    if (rayResult.DitHitAnything)
                    {
                        ropeHook.entity1 = Game.Player.Character;
                        ropeHook.entity2 = rayResult.HitEntity;
                        ropeHook.hookPoint1 = Game.Player.Character.GetBoneCoord((Bone)57005);
                        ropeHook.hookPoint2 = rayResult.HitCoords;
                        ropeHook.ropeType = PlayerToEntityHookRopeType;

                        ropeHook.hookOffset1 = ropeHook.hookPoint1 - ropeHook.entity1.Position;

                        if (rayResult.DitHitEntity && Util.IsValid(rayResult.HitEntity))
                        {
                            ropeHook.hookOffset2 = ropeHook.hookPoint2 - ropeHook.entity2.Position;
                            ropeHook.isEntity2AMapPosition = false;
                        }
                        else
                        {
                            ropeHook.hookOffset2 = Vector3.Zero;
                            ropeHook.isEntity2AMapPosition = true;
                        }

                        if (FREE_RANGE_MODE ||
                            ropeHook.entity1.Position.DistanceTo(ropeHook.hookPoint2) < MAX_HOOK_CREATION_DISTANCE)
                        {
                            CreateHook(ropeHook);
                        }

                        ropeHook.entity1 = null;
                        ropeHook.entity2 = null;
                    }
                }
            }
            catch (Exception exc)
            {
                UI.Notify("VRope Runtime Error:\n" + exc.ToString());
            }

        }

        private void AttachEntityToEntityProc()
        {
            if (Game.Player.Exists() && !Game.Player.IsDead &&
                    Game.Player.CanControlCharacter && Game.Player.IsAiming)
            {
                Entity playerEntity = Game.Player.Character;
                RaycastResult rayResult = Util.CameraRaycastForward();

                if (rayResult.DitHitEntity && Util.IsValid(rayResult.HitEntity))
                {
                    if (ropeHook.entity1 == null)
                    {
                        if (ropeHook.entity2 != null)
                            ropeHook.entity2 = null;

                        ropeHook.entity1 = rayResult.HitEntity;
                        ropeHook.hookPoint1 = rayResult.HitCoords;
                        ropeHook.hookOffset1 = ropeHook.hookPoint1 - ropeHook.entity1.Position;
                    }
                    else if (ropeHook.entity2 == null)
                    {
                        ropeHook.entity2 = rayResult.HitEntity;
                        ropeHook.hookPoint2 = rayResult.HitCoords;
                        ropeHook.hookOffset2 = ropeHook.hookPoint2 - ropeHook.entity2.Position;

                        //Player attachment not allowed here.
                        if (ropeHook.entity2 == ropeHook.entity1 ||
                            ropeHook.entity2 == playerEntity ||
                            ropeHook.entity1 == playerEntity)
                        {
                            ropeHook.entity1 = null;
                            ropeHook.entity2 = null;
                        }
                    }

                    if (ropeHook.entity1 != null && ropeHook.entity2 != null)
                    {
                        if (FREE_RANGE_MODE || ropeHook.entity1.Position.DistanceTo(ropeHook.entity2.Position) < MAX_HOOK_CREATION_DISTANCE)
                        {
                            ropeHook.ropeType = EntityToEntityHookRopeType;
                            ropeHook.isEntity2AMapPosition = false;

                            CreateHook(ropeHook);
                        }

                        ropeHook.entity1 = null;
                        ropeHook.entity2 = null;
                    }
                }
                else if (rayResult.DitHitAnything)
                {
                    if (ropeHook.entity1 != null && ropeHook.entity2 == null &&
                       (FREE_RANGE_MODE || ropeHook.entity1.Position.DistanceTo(rayResult.HitCoords) < MAX_HOOK_CREATION_DISTANCE))
                    {
                        ropeHook.hookPoint2 = rayResult.HitCoords;
                        ropeHook.ropeType = EntityToEntityHookRopeType;
                        ropeHook.isEntity2AMapPosition = true;
                        ropeHook.hookOffset2 = Vector3.Zero;

                        CreateHook(ropeHook);
                    }

                    ropeHook.entity1 = null;
                    ropeHook.entity2 = null;
                }
                else
                {
                    ropeHook.entity1 = null;
                    ropeHook.entity2 = null;
                }
            }
        }

        private void DeleteLastHookProc()
        {
            if (hooks.Count > 0)
            {
                int indexLastHook = hooks.Count - 1;

                DeleteHookByIndex(indexLastHook);
            }
        }

        private void SetLastHookRopeWindingProc(bool winding)
        {
            if (hooks.Count > 0)
            {
                int indexLastHook = hooks.Count - 1;

                SetHookRopeWindingByIndex(indexLastHook, winding);
            }
        }

        private void SetLastHookRopeUnwindingProc(bool unwind)
        {
            if (hooks.Count > 0)
            {
                int lastHookIndex = hooks.Count - 1;

                SetHookRopeUnwindingByIndex(lastHookIndex, unwind);
            }
        }

        private void SetAllHookRopesWindingProc(bool winding)
        {
            for (int i = 0; i < hooks.Count; i++)
            {
                SetHookRopeWindingByIndex(i, winding);
            }
        }

        private void SetAllHookRopesUnwindingProc(bool unwinding)
        {
            for (int i = 0; i < hooks.Count; i++)
            {
                SetHookRopeUnwindingByIndex(i, unwinding);
            }
        }

        private void IncrementForceValueProc(bool negativeIncrement = false)
        {
            if (!negativeIncrement)
                ForceMagnitude += FORCE_INCREMENT_VALUE;
            else
                ForceMagnitude -= FORCE_INCREMENT_VALUE;

            subQueue.AddSubtitle(14, "VRope Force: " + ForceMagnitude.ToString("0.00"), 13);
        }

        private void ApplyForceAtAimedProc(bool invertForce = false)
        {
            if (Game.Player.Exists() && !Game.Player.IsDead &&
                Game.Player.CanControlCharacter && Game.Player.IsAiming)
            {
                RaycastResult rayResult = Util.CameraRaycastForward();
                Entity targetEntity = rayResult.HitEntity;

                if (rayResult.DitHitEntity && Util.IsValid(targetEntity))
                {
                    Vector3 cameraRotation = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_ROT, 0);
                    Vector3 forceDirection = Util.CalculateDirectionVector3d(cameraRotation);

                    if (Util.IsPed(rayResult.HitEntity))
                        Util.MakePedRagdoll((Ped)rayResult.HitEntity, 4000);

                    Vector3 position = rayResult.HitEntity.Position;
                    Vector3 hitPosition = rayResult.HitCoords;
                    Vector3 offset = hitPosition - position;
                    
                    if (invertForce)
                        forceDirection = -forceDirection;

                    float scaleFactor = (CONTINUOUS_FORCE ? 1f : 1.3f);

                    rayResult.HitEntity.ApplyForce(offset + (forceDirection * ForceMagnitude * scaleFactor));
                }
            }
        }

        private void ApplyForceObjectPairProc()
        {
            try
            {
                if (Game.Player.Exists() && !Game.Player.IsDead &&
                    Game.Player.CanControlCharacter && Game.Player.IsAiming)
                {
                    Entity playerEntity = Game.Player.Character;
                    RaycastResult rayResult = Util.CameraRaycastForward();

                    if (rayResult.DitHitEntity && Util.IsValid(rayResult.HitEntity))
                    {
                        if (forceHook.entity1 == null)
                        {
                            if (forceHook.entity2 != null)
                                forceHook.entity2 = null;

                            forceHook.entity1 = rayResult.HitEntity;
                            forceHook.hookPoint1 = rayResult.HitCoords;
                            forceHook.hookOffset1 = forceHook.hookPoint1 - forceHook.entity1.Position;
                        }
                        else if (forceHook.entity2 == null)
                        {
                            forceHook.entity2 = rayResult.HitEntity;
                            forceHook.hookPoint2 = rayResult.HitCoords;
                            forceHook.hookOffset2 = forceHook.hookPoint2 - forceHook.entity2.Position;
                            forceHook.isEntity2AMapPosition = false;

                            if (forceHook.entity2 == forceHook.entity1 ||
                                forceHook.entity2 == playerEntity ||
                                forceHook.entity1 == playerEntity)
                            {
                                forceHook.entity1 = null;
                                forceHook.entity2 = null;
                            }
                        }

                        if (forceHook.entity1 != null && forceHook.entity2 != null)
                        {
                            Vector3 entity2HookPosition = forceHook.entity2.Position + forceHook.hookOffset2;
                            Vector3 entity1HookPosition = forceHook.entity1.Position + forceHook.hookOffset1;

                            ApplyForce(entity1HookPosition, entity2HookPosition);

                            forceHook.entity1 = null;
                            forceHook.entity2 = null;
                        }
                    }
                    else if(rayResult.DitHitAnything)
                    {
                        if ((forceHook.entity1 != null && forceHook.entity2 == null) &&
                            (FREE_RANGE_MODE || forceHook.entity1.Position.DistanceTo(rayResult.HitCoords) < MAX_HOOK_CREATION_DISTANCE))
                        {
                            forceHook.hookPoint2 = rayResult.HitCoords;
                            forceHook.hookOffset2 = Vector3.Zero;
                            forceHook.isEntity2AMapPosition = true;

                            Vector3 entity1HookPosition = forceHook.entity1.Position + forceHook.hookOffset1;

                            ApplyForce(entity1HookPosition, forceHook.hookPoint2);
                        }

                        forceHook.entity1 = null;
                        forceHook.entity2 = null;
                    }
                    else
                    {
                        forceHook.entity1 = null;
                        forceHook.entity2 = null;
                    }
                }
            }
            catch (Exception exc)
            {
                UI.Notify("VRope Runtime Error:\n" + exc.ToString());
            }
        }

        private void ApplyForcePlayerProc()
        {
            if (Game.Player.Exists() && !Game.Player.IsDead &&
                            Game.Player.CanControlCharacter && Game.Player.IsAiming)
            {
                RaycastResult rayResult = Util.CameraRaycastForward();

                if (rayResult.DitHitAnything)
                {
                    forceHook.entity1 = Game.Player.Character;
                    forceHook.hookPoint1 = Game.Player.Character.Position;//GetBoneCoord((Bone)57005);
                    forceHook.hookPoint2 = rayResult.HitCoords;

                    forceHook.hookOffset1 = Vector3.Zero;//forceHook.hookPoint1 - forceHook.entity1.Position;

                    Vector3 entity2HookPosition = Vector3.Zero;

                    if (rayResult.DitHitEntity && Util.IsValid(rayResult.HitEntity))
                    {
                        forceHook.entity2 = rayResult.HitEntity;
                        forceHook.hookOffset2 = forceHook.hookPoint2 - forceHook.entity2.Position;
                        entity2HookPosition = forceHook.entity2.Position + forceHook.hookOffset2;
                    }
                    else
                    {
                        forceHook.hookOffset2 = Vector3.Zero;
                        entity2HookPosition = forceHook.hookPoint2;
                    }

                    //if (FREE_RANGE_MODE ||
                    //    forceHook.entity1.Position.DistanceTo(forceHook.hookPoint2) < MAX_HOOK_CREATION_DISTANCE)
                    {
                        ApplyForce(forceHook.hookPoint1, entity2HookPosition);
                    }

                    forceHook.entity1 = null;
                    forceHook.entity2 = null;
                }
            }
        }


        private void ApplyForce(Vector3 entity1HookPosition, Vector3 entity2HookPosition)
        {
            float scaleFactor = FORCE_SCALE_FACTOR;

            Vector3 distanceVector = entity2HookPosition - entity1HookPosition;
            Vector3 lookAtDirection = distanceVector.Normalized;

            if (Util.IsPed(forceHook.entity1))
            {
                scaleFactor *= 2.2f;

                if (!Util.IsPlayer(forceHook.entity1))
                    Util.MakePedRagdoll((Ped)forceHook.entity1, PED_RAGDOLL_DURATION);
            }

            forceHook.entity1.ApplyForce(forceHook.hookOffset1 + (lookAtDirection * ForceMagnitude * scaleFactor));
        }

        private void CheckForKeysHeldDown()
        {
            for (int i = 0; i < controlKeys.Count; i++)
            {
                var controlKey = controlKeys[i];

                if(controlKey.condition == TriggerCondition.HELD && Keyboard.IsKeyListPressed(controlKey.keys))
                {
                    controlKey.callback.Invoke();
                    controlKey.wasPressed = true;
                    break;
                }
            }  
        }

        private void ProcessXBoxControllerInput()
        {
            XBoxController.UpdateStateBegin();

            for(int i=0; i<controlButtons.Count; i++)
            {
                var buttonTuple = controlButtons[i];
                ControllerState button = buttonTuple.state;
                TriggerCondition condition = buttonTuple.condition;

                if( (condition == TriggerCondition.PRESSED && XBoxController.WasControllerButtonPressed(button)) ||
                    (condition == TriggerCondition.RELEASED && XBoxController.WasControllerButtonReleased(button)) ||
                    (condition == TriggerCondition.HELD && XBoxController.IsControllerButtonPressed(button)) ||
                    (condition == TriggerCondition.ANY))
                {
                    buttonTuple.callback.Invoke();
                    break;
                }                
            }

            XBoxController.UpdateStateEnd();
        }


        private void CheckCurrentModState()
        {
            ModRunning = (Game.Player.Exists() && !FirstTime &&
                            Game.Player.IsAlive && Game.Player.CanControlCharacter);

            if (FirstTime && Game.IsScreenFadedIn)
            {
                Script.Wait(300);

                UI.Notify(MOD_NAME + " " + GetModVersion() + "\nby " + MOD_DEVELOPER, true);

                if (XBoxController.IsControllerConnected())
                    UI.Notify("XBox controller detected.", false);

                FirstTime = false;
            }
        }

        private void ShowScreenInfo()
        {
            if (ropeHook.entity1 != null && ropeHook.entity2 == null)
            {
                GlobalSubtitle += ("VRope: Select a second object to attach.");
            }

            if (forceHook.entity1 != null && forceHook.entity2 == null)
            {
                GlobalSubtitle += ("VRope: Select the target object.");
            }

            //Temp
            GlobalSubtitle += subQueue.MountSubtitle();

            if (DebugMode)
                GlobalSubtitle += "\n" + DebugInfo;

            UI.ShowSubtitle(GlobalSubtitle);
            //subQueue.ShowSubtitle();
        }


        public void OnTick(object sender, EventArgs e)
        {
            try
            {
                GlobalSubtitle = "";
                DebugInfo = "";

                if (!ModActive)
                {
                    Script.Wait(0);
                    return;
                }

                CheckCurrentModState();
                //----------------------------------------------------------------------------------

                if (!ModRunning)
                    return;

                if (DebugMode)
                    UpdateDebugStuff();

                if (XBoxController.IsControllerConnected())
                    ProcessXBoxControllerInput();

                CheckForKeysHeldDown();

                ProcessHooks();

                ShowScreenInfo();
            }
            catch (Exception exc)
            {
                UI.Notify("VRope Runtime Error:\n" + exc.ToString() + "\nMod execution halted.");
                DeleteAllHooks();
                ModRunning = false;
                ModActive = false;
            }
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                for (int i = 0; i < controlKeys.Count; i++)
                {
                    if (!ModActive || !ModRunning)
                    {
                        if (controlKeys[i].name == "ToggleModActiveKey" && Keyboard.IsKeyListPressed(controlKeys[i].keys))
                        {
                            controlKeys[i].callback.Invoke();
                            controlKeys[i].wasPressed = true;
                            break;
                        }
                    }
                    else
                    {
                        if (controlKeys[i].condition.HasFlag(TriggerCondition.PRESSED) && Keyboard.IsKeyListPressed(controlKeys[i].keys))
                        {
                            controlKeys[i].callback.Invoke();
                            controlKeys[i].wasPressed = true;
                            break;
                        }
                        else if(controlKeys[i].condition.HasFlag(TriggerCondition.HELD) && Keyboard.IsKeyListPressed(controlKeys[i].keys))
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                UI.Notify("VRope OnKeyDown Error:\n" + exc.Message, false);
            }
        }

        public void OnKeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                if (!ModActive || !ModRunning)
                {
                    Script.Wait(0);
                    return;
                }

                foreach(var control in controlKeys)
                {
                    if (control.wasPressed)
                    {
                        if (Keyboard.IsKeyListUp(control.keys))
                        {
                            if (control.name == "WindLastHookRopeKey") SetLastHookRopeWindingProc(false);
                            else if (control.name == "WindAllHookRopesKey") SetAllHookRopesWindingProc(false);
                            else if (control.name == "UnwindLastHookRopeKey") SetLastHookRopeUnwindingProc(false);
                            else if (control.name == "UnwindAllHookRopesKey") SetAllHookRopesUnwindingProc(false);

                            control.wasPressed = false;
                            break;
                        } 
                    }
                }
            }
            catch (Exception exc)
            {
                UI.Notify("VRope OnKeyUp Error:\n" + exc.ToString(), false);
            }
        }

            
        private void SetHookRopeWindingByIndex(int index, bool winding)
        {
            if(index >= 0 && index < hooks.Count)
            {
                if(hooks[index] != null && hooks[index].Exists())
                {
                    if(!hooks[index].isWinding && winding)
                    {
                        Function.Call(Hash.START_ROPE_WINDING, hooks[index].rope);
                        hooks[index].isWinding = true;
                    }
                    else if(hooks[index].isWinding && !winding)
                    {
                        Function.Call(Hash.STOP_ROPE_WINDING, hooks[index].rope);
                        hooks[index].rope.ResetLength(true);
                        hooks[index].isWinding = false;
                    }
                }
            }
        }

        private void SetHookRopeUnwindingByIndex(int index, bool unwinding)
        {
            if (index >= 0 && index < hooks.Count)
            {
                if (hooks[index] != null && hooks[index].Exists())
                {
                    if (!hooks[index].isUnwinding && unwinding)
                    {
                        Function.Call(Hash.START_ROPE_UNWINDING_FRONT, hooks[index].rope);
                        hooks[index].isUnwinding = true;
                    }
                    else if (hooks[index].isUnwinding && !unwinding)
                    {
                        Function.Call(Hash.STOP_ROPE_UNWINDING_FRONT, hooks[index].rope);
                        hooks[index].rope.ResetLength(true);
                        hooks[index].isUnwinding = false;
                    }
                }
            }
        }

        private void DeleteAllHooks()
        {
            for (int i = hooks.Count - 1; i >= 0; i--)
            {
                DeleteHookByIndex(i, false);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void DeleteHookByIndex(int index, bool callGC = true, bool removeFromHooks = true)
        {
            if(index >= 0 && index < hooks.Count)
            {
                if(hooks[index] != null)
                {
                    hooks[index].Delete();
                    hooks[index] = null;
                }

                if(removeFromHooks)
                    hooks.RemoveAt(index);

                if (callGC)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
        }


        private HookGroup CreateEntityHook(HookGroup hook, bool copyHook = true, bool hookAtBonePositions = true)
        {
            try
            {
                if (hook.entity1 == null ||
                    (hook.entity2 == null && !hook.isEntity2AMapPosition))
                    return null;

                Vector3 entity1HookPosition = hook.entity1.Position + hook.hookOffset1;
                Vector3 entity2HookPosition = Vector3.Zero;

                if (hook.isEntity2AMapPosition)
                {
                    hook.entity2 = CreateTargetProp(hook.hookPoint2, false, true, true, true, false);
                    entity2HookPosition = hook.hookPoint2;
                }
                else
                {
                    entity2HookPosition = hook.entity2.Position + hook.hookOffset2;
                }

                if(hookAtBonePositions)
                {
                    if(Util.IsPed(hook.entity1) && !Util.IsPlayer(hook.entity1))
                    {
                        entity1HookPosition = Util.GetNearestBonePosition((Ped)hook.entity1, entity1HookPosition);
                    }

                    if(Util.IsPed(hook.entity2))
                    {
                        entity2HookPosition = Util.GetNearestBonePosition((Ped)hook.entity2, entity2HookPosition);
                    }
                }

                float ropeLength = entity1HookPosition.DistanceTo(entity2HookPosition); //TRY1

                if (ropeLength < MIN_ROPE_LENGTH)
                    ropeLength = MIN_ROPE_LENGTH;

                hook.rope = World.AddRope(hook.ropeType, entity1HookPosition, Vector3.Zero, (ropeLength), MIN_ROPE_LENGTH, false); //ORIGINAL
                hook.rope.ActivatePhysics();

                hook.rope.AttachEntities(hook.entity1, entity1HookPosition, hook.entity2, entity2HookPosition, ropeLength);

                if (Util.IsVehicle(hook.entity1))
                    hook.entity1.ApplyForce(new Vector3(0, 1, 0));

                if (Util.IsVehicle(hook.entity2))
                    hook.entity2.ApplyForce(new Vector3(1, 0, 0));

                if (copyHook)
                    return new HookGroup(hook);
                else
                    return (hook);
            }
            catch(Exception exc)
            {
                UI.Notify("VRope CreateEntityHook() Error:\n" + exc.Message + "\n" + exc.StackTrace);
                return hook;
            }
        }

        private void RecreateEntityHook(int hookIndex)
        {
            if(hookIndex >= 0 && hookIndex < hooks.Count && 
                hooks[hookIndex] != null && hooks[hookIndex].entity1 != null)
            {
                HookGroup hook = new HookGroup(hooks[hookIndex]);
                
                DeleteHookByIndex(hookIndex);

                hooks.Insert(hookIndex, CreateEntityHook(hook, false, true));
            }
        }

        private void CreateHook(HookGroup source, bool copyHook = true)
        {
            HookGroup resultHook = CreateEntityHook(source, copyHook);

            if (resultHook != null)
                hooks.Add(resultHook);
        }

        private Prop CreateTargetProp(Vector3 position, bool isDynamic, bool hasCollision, bool isVisible, bool hasFrozenPosition, bool placeOnGround)
        {
            Prop targetProp = World.CreateProp(targetPropModel, position, isDynamic, placeOnGround);

            targetProp.HasCollision = hasCollision;
            targetProp.IsVisible = isVisible;
            targetProp.FreezePosition = hasFrozenPosition;

            return targetProp;
        }


    }
}
