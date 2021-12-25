
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using GTA;
using GTA.Math;
using GTA.Native;

/*
Bone1Chest = 24818
Bone2Chest = 20719
Bone3LeftArm = 61007
Bone4RightArm = 24818
*/

namespace VRope
{
    public class VRopeMain : Script
    {
        public const String MOD_NAME = "VRope";
        public const String MOD_DEVELOPER = "jeffsturm4nn"; // :D
        public const int VERSION_MINOR = 0;
        public const int VERSION_BUILD = 12;
        public const String VERSION_SUFFIX = "a fix 2 DevBuild";

        private const int UPDATE_INTERVAL = 11; //milliseconds.
        private const int UPDATE_FPS = (1000 / UPDATE_INTERVAL);

        private bool ENABLE_XBOX_CONTROLLER_INPUT;
        private bool FREE_RANGE_MODE;
        private String CONFIG_FILE_NAME;
        private float MAX_HOOK_CREATION_DISTANCE;
        private float MAX_HOOKED_ENTITY_DISTANCE;
        private float MAX_HOOKED_PED_DISTANCE;
        private int MAX_SELECTED_HOOKS = 50;
        private bool SHOW_HOOK_ROPE_PROP = true;

        private bool CONTINUOUS_FORCE;
        private float FORCE_INCREMENT_VALUE;
        private float BALLOON_UP_FORCE_INCREMENT;
        private const float FORCE_SCALE_FACTOR = 1.3f;
        private float MAX_BALLOON_HOOK_ALTITUDE;

        //private int MAX_CHAIN_SEGMENTS;
        //private bool SHOW_CHAIN_JOINT_PROP = true;
        //private const float MIN_CHAIN_SEGMENT_LENGTH = 0.1F;
        //private float CHAIN_JOINT_OFFSET = 0.3f;    
        //float CHAIN_JOINT_PROP_MASS = 10.0f;

        private const int INIT_HOOK_LIST_CAPACITY = 200;
        private const float MAX_HOOKED_PED_SPEED = 0.5f;
        private const int PED_RAGDOLL_DURATION = 60000;
        private const char SEPARATOR_CHAR = '+';

        private const float MAX_MIN_ROPE_LENGTH = 1000f;
        private const float MIN_MIN_ROPE_LENGTH = 0.5f;

        private SubtitleQueue subQueue = new SubtitleQueue();


        private bool ModActive = false;
        public bool ModRunning = false;
        private bool FirstTime = true;
        private bool NoSubtitlesMode = false;
        private bool DebugMode = true;


        private Model ropeHookPropModel;
        //private Model chainJointPropModel;

        private String DebugInfo = "";
        private String GlobalSubtitle = "";

        private List<HookPair> hooks = new List<HookPair>(INIT_HOOK_LIST_CAPACITY);
        private List<ChainGroup> chains = new List<ChainGroup>(INIT_HOOK_LIST_CAPACITY);
        private HookPair ropeHook = new HookPair();
        private HookPair forceHook = new HookPair();

        private List<HookPair> selectedHooks = new List<HookPair>(50);

        private List<ControlKey> controlKeys = new List<ControlKey>(30);
        private List<ControlButton> controlButtons = new List<ControlButton>(30);

        private RopeType EntityToEntityHookRopeType;
        private RopeType PlayerToEntityHookRopeType;
        //private RopeType ChainSegmentRopeType;

        private float MinRopeLength;
        private float ForceMagnitude;
        private float BalloonUpForce;
        private bool SolidRopes = false;
        private bool BalloonHookMode = false;

        public VRopeMain()
        {
            try
            {
                CONFIG_FILE_NAME = (Directory.GetCurrentDirectory() + "\\scripts\\VRope.ini");

                ProcessVRopeConfigFile();

                SortKeyTuples();

                if (ENABLE_XBOX_CONTROLLER_INPUT)
                {
                    XBoxController.CheckForController();
                    SortButtonTuples();
                }

                Tick += OnTick;
                KeyDown += OnKeyDown;
                //KeyUp += OnKeyUp;

                Interval = UPDATE_INTERVAL;
            }
            catch (Exception exc)
            {
                UI.Notify("VRope Init Error:\n" + GetErrorMessage(exc));
            }
        }

        ~VRopeMain()
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


        private void SortKeyTuples()
        {
            for (int i = 0; i < controlKeys.Count; i++)
            {
                for (int j = 0; j < controlKeys.Count - 1; j++)
                {
                    if (controlKeys[j].keys.Count < controlKeys[j + 1].keys.Count)
                    {
                        var keyPair = controlKeys[j];

                        controlKeys[j] = controlKeys[j + 1];
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
            RegisterControlKey("ToggleModActiveKey", settings.GetValue<String>("CONTROL_KEYBOARD", "ToggleModActiveKey", "None"),
                (Action)ToggleModActiveProc, TriggerCondition.PRESSED);
            RegisterControlKey("ToggleNoSubtitlesModeKey", settings.GetValue<String>("CONTROL_KEYBOARD", "ToggleNoSubtitlesModeKey", "None"),
                (Action)ToggleNoSubtitlesModeProc, TriggerCondition.PRESSED);
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

            RegisterControlKey("WindLastHookRopeKey", settings.GetValue<String>("CONTROL_KEYBOARD", "WindLastHookRopeKey", "None"),
                (Action)(() => SetLastHookRopeWindingProc(true)), TriggerCondition.HELD);
            RegisterControlKey("WindAllHookRopesKey", settings.GetValue<String>("CONTROL_KEYBOARD", "WindAllHookRopesKey", "None"),
                (Action)(() => SetAllHookRopesWindingProc(true)), TriggerCondition.HELD);
            RegisterControlKey("UnwindLastHookRopeKey", settings.GetValue<String>("CONTROL_KEYBOARD", "UnwindLastHookRopeKey", "None"),
                (Action)(() => SetLastHookRopeUnwindingProc(true)), TriggerCondition.HELD);
            RegisterControlKey("UnwindAllHookRopesKey", settings.GetValue<String>("CONTROL_KEYBOARD", "UnwindAllHookRopesKey", "None"),
                (Action)(() => SetAllHookRopesUnwindingProc(true)), TriggerCondition.HELD);

            //registerControlKey("ToggleSolidRopesKey", settings.GetValue<String>("CONTROL_KEYBOARD", "ToggleSolidRopesKey", "None"),
            //    (Action)ToggleSolidRopesProc, TriggerCondition.PRESSED);
            //registerControlKey("IncreaseMinRopeLengthKey", settings.GetValue<String>("CONTROL_KEYBOARD", "IncreaseMinRopeLengthKey", "None"),
            //    (Action)(() => IncrementMinRopeLength(false)), TriggerCondition.HELD);
            //registerControlKey("DecreaseMinRopeLengthKey", settings.GetValue<String>("CONTROL_KEYBOARD", "DecreaseMinRopeLengthKey", "None"),
            //    (Action)(() => IncrementMinRopeLength(true)), TriggerCondition.HELD);

            RegisterControlKey("ApplyForceKey", settings.GetValue<String>("CONTROL_KEYBOARD", "ApplyForceKey", "None"),
                (Action)(() => ApplyForceAtAimedProc(false)), (CONTINUOUS_FORCE ? TriggerCondition.HELD : TriggerCondition.PRESSED));
            RegisterControlKey("ApplyInvertedForceKey", settings.GetValue<String>("CONTROL_KEYBOARD", "ApplyInvertedForceKey", "None"),
                (Action)(() => ApplyForceAtAimedProc(true)), (CONTINUOUS_FORCE ? TriggerCondition.HELD : TriggerCondition.PRESSED));

            RegisterControlKey("IncreaseForceKey", settings.GetValue<String>("CONTROL_KEYBOARD", "IncreaseForceKey", "None"),
                (Action)(() => IncrementForceProc(false)), TriggerCondition.HELD);
            RegisterControlKey("DecreaseForceKey", settings.GetValue<String>("CONTROL_KEYBOARD", "DecreaseForceKey", "None"),
                (Action)(() => IncrementForceProc(true)), TriggerCondition.HELD);
            RegisterControlKey("ApplyForceObjectPairKey", settings.GetValue<String>("CONTROL_KEYBOARD", "ApplyForceObjectPairKey", "None"),
                (Action)ApplyForceObjectPairProc, TriggerCondition.PRESSED);
            RegisterControlKey("ApplyForcePlayerKey", settings.GetValue<String>("CONTROL_KEYBOARD", "ApplyForcePlayerKey", "None"),
                (Action)ApplyForcePlayerProc, (CONTINUOUS_FORCE ? TriggerCondition.HELD : TriggerCondition.PRESSED));

            RegisterControlKey("ToggleBalloonHookModeKey", settings.GetValue<String>("CONTROL_KEYBOARD", "ToggleBalloonHookModeKey", "None"),
                (Action)ToggleBalloonHookModeProc, TriggerCondition.PRESSED);
            RegisterControlKey("IncreaseBalloonUpForceKey", settings.GetValue<String>("CONTROL_KEYBOARD", "IncreaseBalloonUpForceKey", "None"),
                (Action)(() => IncrementBalloonUpForce(false)), TriggerCondition.HELD);
            RegisterControlKey("DecreaseBalloonUpForceKey", settings.GetValue<String>("CONTROL_KEYBOARD", "DecreaseBalloonUpForceKey", "None"),
                (Action)(() => IncrementBalloonUpForce(true)), TriggerCondition.HELD);

            //RegisterControlKey("CreateRopeChainKey", settings.GetValue<String>("CONTROL_KEYBOARD", "CreateRopeChainKey", "None"), 
            //    (Action)AttachEntityToEntityChainProc, TriggerCondition.PRESSED);

            RegisterControlKey("ToggleDebugInfoKey", settings.GetValue<String>("DEV_STUFF", "ToggleDebugInfoKey", "None"),
                (Action)delegate { DebugMode = !DebugMode; }, TriggerCondition.PRESSED);
        }

        private void InitControllerButtonsFromConfig(ScriptSettings settings)
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
                (Action)delegate { ApplyForceAtAimedProc(false); }, (CONTINUOUS_FORCE ? TriggerCondition.HELD : TriggerCondition.PRESSED));
            RegisterControlButton("ApplyInvertedForceButton",
                settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "ApplyInvertedForceButton", "None"),
                (Action)delegate { ApplyForceAtAimedProc(true); }, (CONTINUOUS_FORCE ? TriggerCondition.HELD : TriggerCondition.PRESSED));

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
                (Action)ApplyForcePlayerProc, (CONTINUOUS_FORCE ? TriggerCondition.HELD : TriggerCondition.PRESSED));

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
        }


        private void RegisterControlKey(String name, String keyData, Action callback, TriggerCondition condition)
        {
            List<Keys> keys = Keyboard.TranslateKeyDataToKeyList(keyData);

            if (keys.Count == 0)
            {
                UI.Notify("VRope ControlKey Error:\n Key combination for \"" + name + "\" is invalid. Check the config file. \nThe control was disabled.");
                return;
            }

            controlKeys.Add(new ControlKey(name, keys, callback, condition));
        }

        private void RegisterControlButton(String name, String buttonData, Action callback, TriggerCondition condition)
        {
            ControllerState buttonState = XBoxController.TranslateButtonStringToButtonData(buttonData);

            if (buttonState.buttonPressedCount == -1)
            {
                UI.Notify("VRope ControlButton Error:\n Button combination for \"" + name + "\" is invalid. Check the config file. \nThe control was disabled.");
                return;
            }

            controlButtons.Add(new ControlButton(name, buttonState, callback, condition));
        }



        private void ProcessVRopeConfigFile()
        {
            try
            {
                ScriptSettings settings = ScriptSettings.Load(CONFIG_FILE_NAME);

                if (!File.Exists(CONFIG_FILE_NAME))
                {
                    UI.Notify("VRope File Error:\n" + CONFIG_FILE_NAME + " could not be found.\nAll settings were set to default.", true);
                }

                ModActive = settings.GetValue("GLOBAL_VARS", "ENABLE_ON_GAME_LOAD", false);
                NoSubtitlesMode = settings.GetValue("GLOBAL_VARS", "NO_SUBTITLES_MODE", false);
                ENABLE_XBOX_CONTROLLER_INPUT = settings.GetValue("GLOBAL_VARS", "ENABLE_XBOX_CONTROLLER_INPUT", true);
                FREE_RANGE_MODE = settings.GetValue("GLOBAL_VARS", "FREE_RANGE_MODE", true);
                SHOW_HOOK_ROPE_PROP = settings.GetValue("GLOBAL_VARS", "SHOW_ROPE_HOOK_PROP", true);

                MinRopeLength = settings.GetValue("GLOBAL_VARS", "DEFAULT_MIN_ROPE_LENGTH", 1.0f);
                MAX_HOOK_CREATION_DISTANCE = settings.GetValue("GLOBAL_VARS", "MAX_HOOK_CREATION_DISTANCE", 70.0f);
                MAX_HOOKED_ENTITY_DISTANCE = settings.GetValue("GLOBAL_VARS", "MAX_HOOKED_ENTITY_DISTANCE", 150.0f);
                MAX_HOOKED_PED_DISTANCE = settings.GetValue("GLOBAL_VARS", "MAX_HOOKED_PED_DISTANCE", 70.0f);
                MAX_BALLOON_HOOK_ALTITUDE = settings.GetValue("GLOBAL_VARS", "MAX_BALLOON_HOOK_ALTITUDE", 200.0f);
                ropeHookPropModel = settings.GetValue("GLOBAL_VARS", "ROPE_HOOK_PROP_MODEL", "prop_golf_ball");

                //chainJointPropModel = settings.GetValue("CHAIN_MECHANICS_VARS", "CHAIN_JOINT_PROP_MODEL", "prop_golf_ball");
                //SHOW_CHAIN_JOINT_PROP = settings.GetValue("CHAIN_MECHANICS_VARS", "SHOW_CHAIN_JOINT_PROP", true);
                //MAX_CHAIN_SEGMENTS = settings.GetValue("CHAIN_MECHANICS_VARS", "MAX_CHAIN_SEGMENTS", 15);

                XBoxController.LEFT_TRIGGER_THRESHOLD = settings.GetValue<byte>("CONTROL_XBOX_CONTROLLER", "LEFT_TRIGGER_THRESHOLD", 255);
                XBoxController.RIGHT_TRIGGER_THRESHOLD = settings.GetValue<byte>("CONTROL_XBOX_CONTROLLER", "RIGHT_TRIGGER_THRESHOLD", 255);

                EntityToEntityHookRopeType = settings.GetValue<RopeType>("HOOK_ROPE_TYPES", "EntityToEntityHookRopeType", (RopeType)4);
                PlayerToEntityHookRopeType = settings.GetValue<RopeType>("HOOK_ROPE_TYPES", "PlayerToEntityHookRopeType", (RopeType)3);
                //ChainSegmentRopeType = settings.GetValue<RopeType>("HOOK_ROPE_TYPES", "ChainSegmentRopeType", (RopeType)4);

                ForceMagnitude = settings.GetValue("FORCE_MECHANICS_VARS", "DEFAULT_FORCE_VALUE", 70.0f);
                BalloonUpForce = settings.GetValue("FORCE_MECHANICS_VARS", "DEFAULT_BALLOON_UP_FORCE_VALUE", 7.0f);
                FORCE_INCREMENT_VALUE = settings.GetValue("FORCE_MECHANICS_VARS", "FORCE_INCREMENT_VALUE", 2.0f);
                BALLOON_UP_FORCE_INCREMENT = settings.GetValue("FORCE_MECHANICS_VARS", "BALLOON_UP_FORCE_INCREMENT", 1.0f);
                CONTINUOUS_FORCE = settings.GetValue("FORCE_MECHANICS_VARS", "CONTINUOUS_FORCE", false);

                InitControlKeysFromConfig(settings);

                if (ENABLE_XBOX_CONTROLLER_INPUT)
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
            bool deleteHook = false;

            if (hooks.Count > 0)
            {
                if (playerEntity.Exists() && playerEntity.IsDead)
                {
                    DeleteAllHooks();
                    return;
                }

                for (int i = 0; i < hooks.Count; i++)
                {
                    if (hooks[i] == null)
                    {
                        if (DebugMode)
                            UI.Notify("Deleting Hook - NULL. i:" + i);

                        deleteHook = true;
                    }
                    else if (!hooks[i].IsValid())
                    {
                        if (DebugMode)
                            UI.Notify("Deleting Hook - Invalid. i:" + i);

                        deleteHook = true;
                    }
                    else if (!FREE_RANGE_MODE && (hooks[i].entity1 != playerEntity &&
                        ((playerPosition.DistanceTo(hooks[i].entity1.Position) > MAX_HOOKED_ENTITY_DISTANCE) ||
                        (playerPosition.DistanceTo(hooks[i].entity2.Position) > MAX_HOOKED_ENTITY_DISTANCE))))
                    {
                        if (DebugMode)
                            UI.Notify("Deleting Hook - Too Far. i:" + i);

                        deleteHook = true;
                    }
                    else if ((Util.IsPed(hooks[i].entity1) && hooks[i].entity1.IsDead) ||
                            (Util.IsPed(hooks[i].entity2) && hooks[i].entity2.IsDead))
                    {
                        if (DebugMode)
                            UI.Notify("Deleting Ped Hook - Dead Ped. i:" + i);

                        deleteHook = true;
                    }
                    else if (((Util.IsPed(hooks[i].entity1) && !Util.IsPlayer(hooks[i].entity1)) || Util.IsPed(hooks[i].entity2)) &&
                        ((playerPosition.DistanceTo(hooks[i].entity1.Position) > MAX_HOOKED_PED_DISTANCE) ||
                        (playerPosition.DistanceTo(hooks[i].entity2.Position) > MAX_HOOKED_PED_DISTANCE)))
                    {
                        if (DebugMode)
                            UI.Notify("Deleting Ped Hook - Too Far. i:" + i);

                        deleteHook = true;
                    }

                    if (deleteHook)
                    {
                        DeleteHookByIndex(i--);
                        continue;
                    }

                    if (hooks[i].HasPed())
                        ProcessPedsInHook(i);

                    if (hooks[i].isEntity1ABalloon)
                        ProcessBalloonHook(i);
                }
            }

        }


        private void RecreateEntityHook(int hookIndex)
        {
            if (hookIndex >= 0 && hookIndex < hooks.Count)
            {
                if(DebugMode)
                    UI.Notify("Recreating Entity Hook");

                HookPair hook = new HookPair(hooks[hookIndex]);

                DeleteHookByIndex(hookIndex, true);

                hooks.Add(CreateEntityHook(hook, true, false));
            }
        }

        private void ProcessPedsInHook(int hookIndex)
        {
            try
            {
                Ped ped = (Ped)(Util.IsPed(hooks[hookIndex].entity1) ? hooks[hookIndex].entity1 : hooks[hookIndex].entity2);


                if (Util.IsPlayer(hooks[hookIndex].entity1))
                {
                    if (Util.IsPed(hooks[hookIndex].entity2))
                        ped = (Ped)hooks[hookIndex].entity2;
                    else return;
                }

                bool ropeWinding = hooks[hookIndex].isWinding;
                bool ropeUnwinding = hooks[hookIndex].isUnwinding;
                float pedSpeed = ped.Velocity.Length();

                if (ped.IsAlive)
                {
                    if (!ped.IsRagdoll)
                    {
                        Util.MakePedRagdoll(ped, PED_RAGDOLL_DURATION);
                        RecreateEntityHook(hookIndex);

                        SetHookRopeWinding(hooks[hookIndex], ropeWinding);
                        SetHookRopeUnwinding(hooks[hookIndex], ropeUnwinding);
                    }
                }
            }
            catch (Exception exc)
            {
                UI.Notify("VRope ProcessPedsInHook() Error:\n" + GetErrorMessage(exc) + "\nMod execution halted. \n" +
                        "Hook Count: " + hooks.Count + "\nError Hook Index: " + hookIndex);
                DeleteAllHooks();
                ModRunning = false;
                ModActive = false;
            }
        }

        private void ProcessBalloonHook(int hookIndex)
        {
            Entity balloonEntity = hooks[hookIndex].entity1;

            if (!hooks[hookIndex].isEntity2AMapPosition && Util.IsPlayer(hooks[hookIndex].entity1))
            {
                balloonEntity = hooks[hookIndex].entity2;
            }

            Vector3 zAxis = new Vector3(0f, 0f, 0.01f);

            if (balloonEntity.HeightAboveGround < MAX_BALLOON_HOOK_ALTITUDE)
                balloonEntity.ApplyForce(zAxis * BalloonUpForce);
        }


        //private void ProcessChains()
        //{
        //    for (int i = 0; i < chains.Count; i++)
        //    {
        //        for (int j = 0; j < chains[i].segments.Count; j++)
        //        {
        //            if (chains[i].segments[j].IsValid())
        //            {
        //                if (chains[i].segments[j].rope.Length > chains[i].segmentLength)
        //                {
        //                    chains[i].segments[j].rope.Length = chains[i].segmentLength;
        //                    //chains[i].segments[j].rope.ResetLength(true);
        //                }
        //            }
        //        }
        //    }
        //}


        public bool CanUseModFeatures()
        {
            return Game.Player.Exists() && !Game.Player.IsDead &&
                !Game.Player.Character.IsRagdoll && Game.Player.CanControlCharacter &&
                Game.Player.IsAiming;
        }

        public bool IsEntityHooked(Entity entity)
        {
            if (entity == null || !entity.Exists())
                return false;

            for (int i = 0; i < hooks.Count; i++)
            {
                if ((hooks[i].entity1 != null && hooks[i].entity1.Equals(entity)) ||
                    (hooks[i].entity2 != null && hooks[i].entity2.Equals(entity)))
                    return true;
            }

            return false;
        }


        private void UpdateDebugStuff()
        {
            DebugInfo += "Active Hooks: " + hooks.Count;

            String format = "0.00";

            if (Game.Player.Exists() && !Game.Player.IsDead &&
                Game.Player.CanControlCharacter)
            {
                if (Game.Player.IsAiming)
                {
                    RaycastResult rayResult = Util.CameraRaycastForward();
                    Entity targetEntity = rayResult.HitEntity;

                    if (rayResult.DitHitEntity && Util.IsValid(targetEntity))
                    {

                        Vector3 pos = targetEntity.Position;
                        Vector3 rot = targetEntity.Rotation;
                        Vector3 vel = targetEntity.Velocity;
                        float dist = targetEntity.Position.DistanceTo(Game.Player.Character.Position);
                        float speed = vel.Length();

                        DebugInfo += "\n | Entity Detected: " + targetEntity.GetType() + " | " + (Util.IsStatic(targetEntity) ? "Static" : "Dynamic") +
                                    "\n Position(X:" + pos.X.ToString(format) + ", Y:" + pos.Y.ToString(format) + ", Z:" + pos.Z.ToString(format) + ")" +
                                    "\n Rotation(" + rot.X.ToString(format) + ", Y:" + rot.Y.ToString(format) + ", Z:" + rot.Z.ToString(format) + ")" +
                                    //"\n Velocity(" + vel.X.ToString(format) + ", Y:" + vel.Y.ToString(format) + ", Z:" + vel.Z.ToString(format) + ")" +
                                    "\n Speed(" + speed.ToString(format) + ") | Distance(" + dist.ToString(format) + ")\n";
                    }
                }

                if (hooks.Count > 0 && hooks.Last() != null && hooks.Last().Exists())
                {
                    if (hooks.Last().entity1 != null)
                        DebugInfo += "\n| LastHook.E1 Distance: " + Game.Player.Character.Position.DistanceTo(hooks.Last().entity1.Position).ToString("0.00");

                    if (hooks.Last().entity2 != null)
                        DebugInfo += " | LastHook.E2 Distance: " + Game.Player.Character.Position.DistanceTo(hooks.Last().entity2.Position).ToString("0.00");
                }

                Vector3 ppos = Game.Player.Character.Position;

                DebugInfo += "\n Player[" + " Speed(" + Game.Player.Character.Velocity.Length().ToString(format) + ")," +
                            " Position(X:" + ppos.X.ToString(format) + ", Y:" + ppos.Y.ToString(format) + ", Z:" + ppos.Z.ToString(format) + ") ]";
            }
        }

        private String GetErrorMessage(Exception exc)
        {
            return (DebugMode ? exc.ToString() : exc.Message);
        }


        //Callback Procedures
        private void ToggleModActiveProc()
        {
            ModActive = !ModActive;

            if (!NoSubtitlesMode)
            {
                UI.ShowSubtitle((!ModActive ? "(VRope Disabled)" : "[VRope Enabled]") + "\n\n\n\n\n");
                Script.Wait(1200);
            }
            else
            {
                UI.Notify((!ModActive ? "VRope (Disabled)" : "VRope [Enabled]"));
            }
        }

        private void ToggleNoSubtitlesModeProc()
        {
            NoSubtitlesMode = !NoSubtitlesMode;

            UI.Notify("VRope 'No Subtitles' Mode " + (NoSubtitlesMode ? "[Enabled]." : "(Disabled)."));
        }


        private void MultipleObjectSelectionProc()
        {
            if (Game.Player.Exists() && !Game.Player.IsDead &&
                Game.Player.CanControlCharacter && Game.Player.IsAiming &&
                selectedHooks.Count < MAX_SELECTED_HOOKS)
            {
                RaycastResult rayResult = CameraRaycastForward();
                HookPair selectedHook = new HookPair(ropeHook);
                Entity targetEntity = null;

                if (Util.GetEntityPlayerIsAimingAt(ref targetEntity) && targetEntity != null)
                {
                    selectedHook.entity1 = targetEntity;
                    selectedHook.hookPoint1 = rayResult.HitCoords;
                    selectedHook.hookOffset1 = (selectedHook.hookPoint1 != Vector3.Zero ? (selectedHook.hookPoint1 - selectedHook.entity1.Position) : Vector3.Zero);

                    selectedHooks.Add(selectedHook);
                }
            }
        }


        //private void PerformTwoTargetAction(Action action, bool allowMultipleSelection = true)
        //{
        //    Entity playerEntity = Game.Player.Character;
        //    RaycastResult rayResult = CameraRaycastForward();
        //    Entity targetEntity = null;

        //    if (allowMultipleSelection && selectedHooks.Count > 0)
        //    {
        //        bool hasTargetEntity = Util.GetEntityPlayerIsAimingAt(ref targetEntity);

        //        foreach (var hook in selectedHooks)
        //        {
        //            if (hook.entity1 == null || hook.entity1 == playerEntity ||
        //                hook.entity1 == targetEntity || targetEntity == playerEntity)
        //                continue;

        //            hook.entity2 = targetEntity;
        //            hook.hookPoint2 = rayResult.HitCoords;
        //            hook.hookOffset2 = Vector3.Zero;
        //            hook.ropeType = EntityToEntityHookRopeType;

        //            if (hasTargetEntity && targetEntity != null)
        //            {
        //                hook.hookOffset2 = (hook.hookPoint2 != Vector3.Zero ? (hook.hookPoint2 - hook.entity2.Position) : Vector3.Zero);
        //                hook.isEntity2AMapPosition = false;
        //            }
        //            else if (rayResult.DitHitAnything)
        //            {
        //                hook.isEntity2AMapPosition = true;
        //            }
        //            else continue;

        //            action.Invoke();
        //        }

        //        selectedHooks.Clear();
        //        return;
        //    }


        //    if (Util.GetEntityPlayerIsAimingAt(ref targetEntity))
        //    {
        //        if (ropeHook.entity1 == null)
        //        {
        //            if (ropeHook.entity2 != null)
        //                ropeHook.entity2 = null;

        //            ropeHook.entity1 = targetEntity;
        //            ropeHook.hookPoint1 = rayResult.HitCoords;
        //            ropeHook.hookOffset1 = (ropeHook.hookPoint1 != Vector3.Zero ? (ropeHook.hookPoint1 - ropeHook.entity1.Position) : Vector3.Zero);
        //        }
        //        else if (ropeHook.entity2 == null)
        //        {
        //            ropeHook.entity2 = targetEntity;
        //            ropeHook.hookPoint2 = rayResult.HitCoords;
        //            ropeHook.hookOffset2 = (ropeHook.hookPoint2 != Vector3.Zero ? (ropeHook.hookPoint2 - ropeHook.entity2.Position) : Vector3.Zero);

        //            //Player attachment not allowed here.
        //            if (ropeHook.entity2 == ropeHook.entity1 ||
        //                ropeHook.entity2 == playerEntity ||
        //                ropeHook.entity1 == playerEntity)
        //            {
        //                ropeHook.entity1 = null;
        //                ropeHook.entity2 = null;
        //            }
        //        }

        //        if (ropeHook.entity1 != null && ropeHook.entity2 != null)
        //        {
        //            if (ropeHook.entity1.Position.DistanceTo(ropeHook.entity2.Position) < MAX_HOOK_CREATION_DISTANCE)
        //            {
        //                ropeHook.ropeType = EntityToEntityHookRopeType;
        //                ropeHook.isEntity2AMapPosition = false;

        //                action.Invoke();
        //            }

        //            ropeHook.entity1 = null;
        //            ropeHook.entity2 = null;
        //        }
        //    }
        //    else if (rayResult.DitHitAnything)
        //    {
        //        if (ropeHook.entity1 != null && ropeHook.entity2 == null &&
        //            (ropeHook.entity1.Position.DistanceTo(rayResult.HitCoords) < MAX_HOOK_CREATION_DISTANCE))
        //        //(FREE_RANGE_MODE || ropeHook.entity1.Position.DistanceTo(rayResult.HitCoords) < MAX_HOOK_CREATION_DISTANCE))
        //        {
        //            ropeHook.hookPoint2 = rayResult.HitCoords;
        //            ropeHook.ropeType = EntityToEntityHookRopeType;
        //            ropeHook.isEntity2AMapPosition = true;
        //            ropeHook.hookOffset2 = Vector3.Zero;

        //            CreateHook(ropeHook);
        //        }

        //        ropeHook.entity1 = null;
        //        ropeHook.entity2 = null;
        //    }
        //    else
        //    {
        //        ropeHook.entity1 = null;
        //        ropeHook.entity2 = null;
        //    }
        //}

        private void AttachPlayerToEntityProc()
        {
            try
            {
                if (CanUseModFeatures())
                {
                    RaycastResult rayResult = CameraRaycastForward();
                    Entity targetEntity = null;

                    if (rayResult.DitHitAnything)
                    {
                        ropeHook.entity1 = Game.Player.Character;
                        ropeHook.hookPoint1 = Game.Player.Character.GetBoneCoord((Bone)57005);
                        ropeHook.hookPoint2 = rayResult.HitCoords;
                        ropeHook.ropeType = PlayerToEntityHookRopeType;
                        ropeHook.hookOffset1 = ropeHook.hookPoint1 - ropeHook.entity1.Position;

                        if (Util.GetEntityPlayerIsAimingAt(ref targetEntity))
                        {
                            ropeHook.entity2 = targetEntity;
                            ropeHook.hookOffset2 = (ropeHook.hookPoint2 != Vector3.Zero ? (ropeHook.hookPoint2 - ropeHook.entity2.Position) : Vector3.Zero);
                            ropeHook.isEntity2AMapPosition = false;
                        }
                        else
                        {
                            ropeHook.entity2 = null;
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
                UI.Notify("VRope Runtime Error:\n" + GetErrorMessage(exc));
            }

        }

        private void AttachEntityToEntityProc(bool chainRope = false)
        {
            Entity playerEntity = Game.Player.Character;
            RaycastResult rayResult = CameraRaycastForward();
            Entity targetEntity = null;

            if (selectedHooks.Count > 0)
            {
                bool hasTargetEntity = Util.GetEntityPlayerIsAimingAt(ref targetEntity);

                foreach (var hook in selectedHooks)
                {
                    if (hook.entity1 == null || hook.entity1 == playerEntity ||
                        hook.entity1 == targetEntity || targetEntity == playerEntity)
                        continue;

                    hook.entity2 = targetEntity;
                    hook.hookPoint2 = rayResult.HitCoords;
                    hook.hookOffset2 = Vector3.Zero;
                    hook.ropeType = EntityToEntityHookRopeType;

                    if (hasTargetEntity && targetEntity != null)
                    {
                        hook.hookOffset2 = (hook.hookPoint2 != Vector3.Zero ? (hook.hookPoint2 - hook.entity2.Position) : Vector3.Zero);
                        hook.isEntity2AMapPosition = false;
                    }
                    else if (rayResult.DitHitAnything)
                    {
                        hook.isEntity2AMapPosition = true;
                    }
                    else continue;

                    //if (!chainRope)
                    CreateHook(hook, true);
                    //else
                    //    CreateRopeChain(hook, true);
                }

                selectedHooks.Clear();
                return;
            }


            if (Util.GetEntityPlayerIsAimingAt(ref targetEntity))
            {
                if (ropeHook.entity1 == null)
                {
                    if (ropeHook.entity2 != null)
                        ropeHook.entity2 = null;

                    ropeHook.entity1 = targetEntity;
                    ropeHook.hookPoint1 = rayResult.HitCoords;
                    ropeHook.hookOffset1 = (ropeHook.hookPoint1 != Vector3.Zero ? (ropeHook.hookPoint1 - ropeHook.entity1.Position) : Vector3.Zero);
                }
                else if (ropeHook.entity2 == null)
                {
                    ropeHook.entity2 = targetEntity;
                    ropeHook.hookPoint2 = rayResult.HitCoords;
                    ropeHook.hookOffset2 = (ropeHook.hookPoint2 != Vector3.Zero ? (ropeHook.hookPoint2 - ropeHook.entity2.Position) : Vector3.Zero);

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
                    if (ropeHook.entity1.Position.DistanceTo(ropeHook.entity2.Position) < MAX_HOOK_CREATION_DISTANCE)
                    {
                        ropeHook.ropeType = EntityToEntityHookRopeType;
                        ropeHook.isEntity2AMapPosition = false;

                        //if (!chainRope)
                        CreateHook(ropeHook, true);
                        //else
                        //    CreateRopeChain(ropeHook, false);
                    }

                    ropeHook.entity1 = null;
                    ropeHook.entity2 = null;
                }
            }
            else if (rayResult.DitHitAnything)
            {
                if (ropeHook.entity1 != null && ropeHook.entity2 == null &&
                    //(ropeHook.entity1.Position.DistanceTo(rayResult.HitCoords) < MAX_HOOK_CREATION_DISTANCE))
                    (FREE_RANGE_MODE || ropeHook.entity1.Position.DistanceTo(rayResult.HitCoords) < MAX_HOOK_CREATION_DISTANCE))
                {
                    ropeHook.hookPoint2 = rayResult.HitCoords;
                    ropeHook.ropeType = EntityToEntityHookRopeType;
                    ropeHook.isEntity2AMapPosition = true;
                    ropeHook.hookOffset2 = Vector3.Zero;

                    //if (!chainRope)
                    CreateHook(ropeHook, true);
                    //else
                    //    CreateRopeChain(ropeHook, true);
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

        private void AttachEntityToEntityRopeProc()
        {
            AttachEntityToEntityProc(false);
        }

        //private void AttachEntityToEntityChainProc()
        //{
        //    AttachEntityToEntityProc(true);
        //}


        private void DeleteLastHookProc()
        {
            if (hooks.Count > 0)
            {
                int indexLastHook = hooks.Count - 1;

                DeleteHookByIndex(indexLastHook);
            }
        }

        private void DeleteFirstHookProc()
        {
            if (hooks.Count > 0)
            {
                DeleteHookByIndex(0);
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

        private void ToggleSolidRopesProc()
        {
            SolidRopes = !SolidRopes;

            subQueue.AddSubtitle("VRope Solid Ropes: " + (SolidRopes ? "[ON]" : "(OFF)"), 24);
        }

        private void IncrementMinRopeLength(bool negativeIncrement = false, bool halfIncrement = false)
        {
            float lengthIncrement = (halfIncrement ? 0.5f : 1.0f);

            if (!negativeIncrement && MinRopeLength < MAX_MIN_ROPE_LENGTH)
            {
                MinRopeLength += lengthIncrement;
            }
            else if (negativeIncrement && MinRopeLength > (MIN_MIN_ROPE_LENGTH + lengthIncrement))
            {
                MinRopeLength -= lengthIncrement;
            }

            subQueue.AddSubtitle(166, "VRope Minimum Rope Length: " + MinRopeLength.ToString("0.00"), 17);
        }


        private void IncrementForceProc(bool negativeIncrement = false, bool halfIncrement = false)
        {
            float increment = FORCE_INCREMENT_VALUE;

            if (negativeIncrement)
                increment = -increment;

            if (halfIncrement)
                increment = increment / 2f;

            ForceMagnitude += increment;

            subQueue.AddSubtitle(14, "VRope Force Value: " + ForceMagnitude.ToString("0.00"), 20);
        }

        private void IncrementBalloonUpForce(bool negativeIncrement = false, bool halfIncrement = false)
        {
            float increment = BALLOON_UP_FORCE_INCREMENT;

            if (negativeIncrement)
                increment = -increment;

            if (halfIncrement)
                increment = increment / 2f;

            BalloonUpForce += increment;

            subQueue.AddSubtitle(333, "VRope Balloon Up Force: " + BalloonUpForce.ToString("0.00"), 20);
        }

        private void ToggleBalloonHookModeProc()
        {
            BalloonHookMode = !BalloonHookMode;

            subQueue.AddSubtitle(1230, "VRope Balloon Hook Mode: " + (BalloonHookMode ? "[ON]" : "(OFF)"), 54);
        }


        private void ApplyForceAtAimedProc(bool invertForce = false)
        {
            if (CanUseModFeatures())
            {
                RaycastResult rayResult = CameraRaycastForward();
                Entity targetEntity = null;

                if (Util.GetEntityPlayerIsAimingAt(ref targetEntity) && targetEntity != null)
                {
                    Vector3 cameraRotation = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_ROT, 0);
                    Vector3 forceDirection = Util.CalculateDirectionVector3d(cameraRotation);

                    Vector3 entity1Offset = (rayResult.HitCoords != Vector3.Zero ? (rayResult.HitCoords - targetEntity.Position) : Vector3.Zero);

                    if (Util.IsPed(targetEntity))
                    {
                        Util.MakePedRagdoll((Ped)targetEntity, 4000);
                        entity1Offset = Vector3.Zero;
                    }

                    //Vector3 entity1ForcePosition = targetEntity.Position + entity1Offset;

                    if (invertForce)
                        forceDirection = -forceDirection;


                    float scaleFactor = (CONTINUOUS_FORCE ? 1f : 1.3f);

                    Function.Call<bool>(Hash.NETWORK_REQUEST_CONTROL_OF_ENTITY, targetEntity.Handle);

                    targetEntity.ApplyForce((forceDirection * ForceMagnitude * scaleFactor));
                }
            }
        }

        private void ApplyForceObjectPairProc()
        {
            try
            {
                if (CanUseModFeatures())
                {
                    Entity targetEntity = null;
                    RaycastResult rayResult = CameraRaycastForward();
                    Entity playerEntity = Game.Player.Character;

                    if (selectedHooks.Count > 0)
                    {
                        bool hasTargetEntity = Util.GetEntityPlayerIsAimingAt(ref targetEntity);

                        foreach (var hook in selectedHooks)
                        {
                            if (hook.entity1 == null || hook.entity1 == playerEntity ||
                                hook.entity1 == targetEntity || targetEntity == playerEntity)
                                continue;

                            hook.entity2 = targetEntity;
                            hook.hookPoint2 = rayResult.HitCoords;
                            hook.hookOffset2 = Vector3.Zero;

                            Vector3 entity1HookPosition = hook.entity1.Position + hook.hookOffset1;
                            Vector3 entity2HookPosition = Vector3.Zero;

                            if (hasTargetEntity && targetEntity != null)
                            {
                                hook.hookOffset2 = (hook.hookPoint2 != Vector3.Zero ? (hook.hookPoint2 - hook.entity2.Position) : Vector3.Zero);
                                entity2HookPosition = hook.entity2.Position + hook.hookOffset2;
                            }
                            else if (rayResult.DitHitAnything)
                            {
                                entity2HookPosition = hook.hookPoint2;
                            }
                            else continue;

                            ApplyForce(hook, entity1HookPosition, entity2HookPosition);
                        }

                        selectedHooks.Clear();
                        return;
                    }

                    if (Util.GetEntityPlayerIsAimingAt(ref targetEntity))
                    {
                        if (forceHook.entity1 == null)
                        {
                            if (forceHook.entity2 != null)
                                forceHook.entity2 = null;

                            forceHook.entity1 = targetEntity;//rayResult.HitEntity;
                            forceHook.hookPoint1 = rayResult.HitCoords;//targetEntity.Position;
                            forceHook.hookOffset1 = (forceHook.hookPoint1 != Vector3.Zero ? (forceHook.hookPoint1 - forceHook.entity1.Position) : Vector3.Zero);
                        }
                        else if (forceHook.entity2 == null)
                        {
                            forceHook.entity2 = targetEntity;//rayResult.HitEntity;
                            forceHook.hookPoint2 = rayResult.HitCoords;//targetEntity.Position;
                            forceHook.hookOffset2 = (forceHook.hookPoint2 != Vector3.Zero ? (forceHook.hookPoint2 - forceHook.entity2.Position) : Vector3.Zero);

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
                    else if (rayResult.DitHitAnything)
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
                UI.Notify("VRope ApplyForceObjectPairProc Error:\n" + GetErrorMessage(exc));
            }
        }

        private void ApplyForcePlayerProc()
        {
            if (CanUseModFeatures())
            {
                RaycastResult rayResult = CameraRaycastForward();

                if (rayResult.DitHitAnything)
                {
                    forceHook.entity1 = Game.Player.Character;
                    forceHook.hookPoint1 = Game.Player.Character.Position;//GetBoneCoord((Bone)57005);
                    forceHook.hookPoint2 = rayResult.HitCoords;

                    forceHook.hookOffset1 = (forceHook.hookPoint1 != Vector3.Zero ? (forceHook.hookPoint1 - forceHook.entity1.Position) : Vector3.Zero);

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

                    ApplyForce(forceHook.hookPoint1, entity2HookPosition);

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

            if (Util.IsPed(forceHook.entity1) && !Util.IsPlayer(forceHook.entity1))
            {
                scaleFactor *= 2.2f;

                if (!Util.IsPlayer(forceHook.entity1))
                    Util.MakePedRagdoll((Ped)forceHook.entity1, PED_RAGDOLL_DURATION);
            }

            forceHook.entity1.ApplyForce((lookAtDirection * ForceMagnitude * scaleFactor));
        }

        private void ApplyForce(HookPair hook, Vector3 entity1HookPosition, Vector3 entity2HookPosition)
        {
            if (hook == null || hook.entity1 == null)
                return;

            float scaleFactor = FORCE_SCALE_FACTOR;

            Vector3 distanceVector = entity2HookPosition - entity1HookPosition;
            Vector3 lookAtDirection = distanceVector.Normalized;

            if (Util.IsPed(hook.entity1) && !Util.IsPlayer(hook.entity1))
            {
                scaleFactor *= 2.2f;

                if (!Util.IsPlayer(hook.entity1))
                    Util.MakePedRagdoll((Ped)hook.entity1, PED_RAGDOLL_DURATION);
            }

            hook.entity1.ApplyForce((lookAtDirection * ForceMagnitude * scaleFactor));
        }


        private void CheckForKeysHeldDown()
        {
            for (int i = 0; i < controlKeys.Count; i++)
            {
                var controlKey = controlKeys[i];

                if (controlKey.condition == TriggerCondition.HELD && Keyboard.IsKeyListPressed(controlKey.keys))
                {
                    controlKey.callback.Invoke();
                    controlKey.wasPressed = true;
                    break;
                }
            }
        }

        private void CheckForKeysReleased()
        {
            for (int i = 0; i < controlKeys.Count; i++)
            {
                var control = controlKeys[i];

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

        private void ProcessXBoxControllerInput()
        {
            XBoxController.UpdateStateBegin();

            for (int i = 0; i < controlButtons.Count; i++)
            {
                var controlButton = controlButtons[i];
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


        private void CheckCurrentModState()
        {
            ModRunning = (Game.Player.Exists() && !FirstTime &&
                            Game.Player.IsAlive && Game.Player.CanControlCharacter);

            if (FirstTime && Game.IsScreenFadedIn)
            {
                Script.Wait(500);

                UI.Notify(MOD_NAME + " " + GetModVersion() + "\nby " + MOD_DEVELOPER, true);

                if (XBoxController.IsControllerConnected())
                    UI.Notify("XBox controller detected.", false);

                FirstTime = false;
            }
        }

        private void ShowScreenInfo()
        {
            if (!NoSubtitlesMode)
            {
                if (ropeHook.entity1 != null && ropeHook.entity2 == null)
                {
                    GlobalSubtitle += ("VRope: Select a second object to attach.\n");
                }

                if (forceHook.entity1 != null && forceHook.entity2 == null)
                {
                    GlobalSubtitle += ("VRope: Select the target object.\n");
                }

                if (selectedHooks.Count > 0)
                {
                    GlobalSubtitle += "VRope: Objects Selected [ " + selectedHooks.Count + " ].\n";
                }

                GlobalSubtitle += subQueue.MountSubtitle();

                if (DebugMode)
                    GlobalSubtitle += "\n" + DebugInfo;


                UI.ShowSubtitle(GlobalSubtitle);
            }
        }


        public void OnTick(object sender, EventArgs e)
        {
            try
            {
                GlobalSubtitle = "";
                DebugInfo = "";

                if (!ModActive)
                {
                    Script.Wait(1);
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
                CheckForKeysReleased();

                ProcessHooks();
                //ProcessChains();

                ShowScreenInfo();
            }
            catch (Exception exc)
            {
                UI.Notify("VRope Runtime Error:\n" + GetErrorMessage(exc) + "\nMod execution halted.");
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
                            if (!controlKeys[i].wasPressed)
                            {
                                controlKeys[i].callback.Invoke();
                                controlKeys[i].wasPressed = true;
                            }

                            break;
                        }
                        else if (controlKeys[i].condition.HasFlag(TriggerCondition.HELD) && Keyboard.IsKeyListPressed(controlKeys[i].keys))
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                UI.Notify("VRope OnKeyDown Error:\n" + GetErrorMessage(exc), false);
            }
        }



        private void SetHookRopeWinding(HookPair hook, bool winding)
        {
            if (hook != null && hook.Exists())
            {
                if (!hook.isWinding && winding)
                {
                    Function.Call(Hash.START_ROPE_WINDING, hook.rope);
                    hook.isWinding = true;
                }
                else if (hook.isWinding && !winding)
                {
                    Function.Call(Hash.STOP_ROPE_WINDING, hook.rope);
                    hook.rope.ResetLength(true);
                    hook.isWinding = false;
                }
            }
        }

        private void SetHookRopeWindingByIndex(int index, bool winding)
        {
            if (index >= 0 && index < hooks.Count)
            {
                //SetHookRopeWinding(hooks[index], winding);

                if (hooks[index] != null && hooks[index].Exists())
                {
                    if (!hooks[index].isWinding && winding)
                    {
                        Function.Call(Hash.START_ROPE_WINDING, hooks[index].rope);
                        hooks[index].isWinding = true;
                    }
                    else if (hooks[index].isWinding && !winding)
                    {
                        Function.Call(Hash.STOP_ROPE_WINDING, hooks[index].rope);
                        hooks[index].rope.ResetLength(true);
                        hooks[index].isWinding = false;
                    }
                }
            }
        }


        private void SetHookRopeUnwinding(HookPair hook, bool unwinding)
        {
            if (hook != null && hook.Exists())
            {
                if (!hook.isUnwinding && unwinding)
                {
                    Function.Call(Hash.START_ROPE_UNWINDING_FRONT, hook.rope);
                    hook.isUnwinding = true;
                }
                else if (hook.isUnwinding && !unwinding)
                {
                    Function.Call(Hash.STOP_ROPE_UNWINDING_FRONT, hook.rope);
                    hook.rope.ResetLength(true);
                    hook.isUnwinding = false;
                }
            }
        }

        private void SetHookRopeUnwindingByIndex(int index, bool unwinding)
        {
            if (index >= 0 && index < hooks.Count)
            {
                //SetHookRopeUnwinding(hooks[index], unwinding);

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
            while (hooks.Count > 0)
            {
                DeleteHookByIndex(hooks.Count - 1, true);
            }

            while (chains.Count > 0)
            {
                if (chains.Last() != null)
                {
                    chains.Last().Delete();
                }

                chains.RemoveAt(chains.Count - 1);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void DeleteHookByIndex(int hookIndex, bool removeFromHooks = true)
        {
            if (hookIndex >= 0 && hookIndex < hooks.Count)
            {
                if (hooks[hookIndex] != null)
                {
                    hooks[hookIndex].Delete();
                    hooks[hookIndex] = null;
                }

                else
                {
                    UI.Notify("DeleteHookByIndex(): Attempted to delete NULL hook.");
                }

                if (removeFromHooks)
                    hooks.RemoveAt(hookIndex);

                //if (callGC)
                //{
                //    GC.Collect();
                //    GC.WaitForPendingFinalizers();
                //}
            }
        }

        private void DeleteHook(HookPair hook, bool removeFromHooks = true)
        {
            if (hook != null)
            {
                hook.Delete();
                hook = null;
            }

            else
            {
                UI.Notify("DeleteHookByIndex(): Attempted to delete NULL hook.");
            }

            if (removeFromHooks)
                hooks.Remove(hook);
        }



        private bool CheckHookPermission(HookPair hook)
        {
            if (hook == null || hook.entity1 == null)
                return false;

            if (Util.IsPed(hook.entity1) && !Util.IsPlayer(hook.entity1))
            {
                if (Util.IsPed(hook.entity2) || IsEntityHooked(hook.entity1))
                    return false;
            }

            if (Util.IsPed(hook.entity2) && IsEntityHooked(hook.entity2))
                return false;

            return true;
        }

        private HookPair CreateEntityHook(HookPair hook, bool copyHook = true, bool hookAtBonePositions = true, float minRopeLength = MIN_MIN_ROPE_LENGTH)
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
                    hook.entity2 = CreateTargetProp(hook.hookPoint2, false, true, SHOW_HOOK_ROPE_PROP, true, false);
                    entity2HookPosition = hook.entity2.Position;
                }
                else
                {
                    entity2HookPosition = hook.entity2.Position + hook.hookOffset2;
                }

                if (hookAtBonePositions)
                {
                    if (Util.IsPed(hook.entity1) && !Util.IsPlayer(hook.entity1))
                    {
                        entity1HookPosition = Util.GetNearestBonePosition((Ped)hook.entity1, entity1HookPosition);
                    }

                    if (Util.IsPed(hook.entity2))
                    {
                        entity2HookPosition = Util.GetNearestBonePosition((Ped)hook.entity2, entity2HookPosition);
                    }
                }

                float ropeLength = entity1HookPosition.DistanceTo(entity2HookPosition); //TRY1

                if (ropeLength < minRopeLength)
                    ropeLength = minRopeLength;

                hook.rope = World.AddRope(hook.ropeType, entity1HookPosition, Vector3.Zero, ropeLength, minRopeLength, false); //ORIGINAL
                hook.rope.ActivatePhysics();

                hook.rope.AttachEntities(hook.entity1, entity1HookPosition, hook.entity2, entity2HookPosition, ropeLength);

                if (Util.IsVehicle(hook.entity1))
                    hook.entity1.ApplyForce(new Vector3(0, 1, 0));

                if (Util.IsVehicle(hook.entity2))
                    hook.entity2.ApplyForce(new Vector3(1, 0, 0));

                hook.isEntity1ABalloon = BalloonHookMode;

                //UI.Notify("Hook Created. E1 Null: " + (hook.entity1 == null ? "true" : "false") + "| E2 Null: " +
                //    (hook.entity2 == null ? "true" : "false") + "Valid: " + hook.IsValid());

                if (copyHook)
                    return new HookPair(hook);
                else
                    return (hook);
            }
            catch (Exception exc)
            {
                UI.Notify("VRope CreateEntityHook() Error:\n" + GetErrorMessage(exc));
                return hook;
            }
        }

        private void CreateHook(HookPair source, bool copyHook = true)
        {
            if (!CheckHookPermission(source))
                return;

            HookPair resultHook = CreateEntityHook(source, copyHook);

            if (resultHook != null)
                hooks.Add(resultHook);
        }



        //private void CreateRopeChain(HookPair hook, bool copyHook = true, bool hookAtBonePositions = true)
        //{
        //    if (hook.entity1 == null ||
        //          (hook.entity2 == null && !hook.isEntity2AMapPosition))
        //        return;

        //    Vector3 entity1HookPosition = hook.entity1.Position + hook.hookOffset1;
        //    Vector3 entity2HookPosition = Vector3.Zero;

        //    if (hook.isEntity2AMapPosition)
        //    {
        //        hook.entity2 = CreateChainJointProp(hook.hookPoint2);
        //        entity2HookPosition = hook.entity2.Position;
        //    }
        //    else
        //    {
        //        entity2HookPosition = hook.entity2.Position + hook.hookOffset2; //hook.hookPosition2
        //    }

        //    if (hookAtBonePositions)
        //    {
        //        if (Util.IsPed(hook.entity1) && !Util.IsPlayer(hook.entity1))
        //        {
        //            entity1HookPosition = Util.GetNearestBonePosition((Ped)hook.entity1, entity1HookPosition);
        //        }

        //        if (Util.IsPed(hook.entity2))
        //        {
        //            entity2HookPosition = Util.GetNearestBonePosition((Ped)hook.entity2, entity2HookPosition);
        //        }
        //    }

        //    Vector3 distanceVector = entity2HookPosition - entity1HookPosition;
        //    Vector3 lookAtDirection = distanceVector.Normalized;

        //    float ropeLength = entity1HookPosition.DistanceTo(entity2HookPosition); //TRY1
        //    float segmentLength = ropeLength / MAX_CHAIN_SEGMENTS;

        //    ChainGroup chain = new ChainGroup();
        //    chain.segmentLength = segmentLength;

        //    for (int i = 0; i < MAX_CHAIN_SEGMENTS; i++)
        //    {
        //        if (chain.SegmentsCount() == 0)
        //        {
        //            HookPair segmentPair = new HookPair();
        //            segmentPair.ropeType = ChainSegmentRopeType;
        //            segmentPair.isEntity2AMapPosition = false;

        //            segmentPair.entity1 = hook.entity1;

        //            segmentPair.entity2 = CreateChainJointProp(entity1HookPosition + (lookAtDirection * segmentLength));
        //            segmentPair.entity2.Position = entity1HookPosition + (lookAtDirection * segmentLength);
        //            segmentPair.hookPoint2 = segmentPair.entity2.Position;

        //            chain.segments.Add(CreateEntityHook(segmentPair, false, true, MIN_CHAIN_SEGMENT_LENGTH)); // TEST VALUE
        //        }
        //        else if (chain.SegmentsCount() > 0 && chain.SegmentsCount() < MAX_CHAIN_SEGMENTS - 1)
        //        {
        //            HookPair segmentPair = new HookPair();
        //            segmentPair.isEntity2AMapPosition = false;
        //            segmentPair.ropeType = ChainSegmentRopeType;

        //            HookPair lastSegmentPair = chain.segments.Last();

        //            segmentPair.entity1 = lastSegmentPair.entity2;
        //            segmentPair.hookPoint1 = lastSegmentPair.entity2.Position + (CHAIN_JOINT_OFFSET * lookAtDirection);

        //            segmentPair.entity2 = CreateChainJointProp(entity1HookPosition + lookAtDirection * segmentLength * (i + 1));
        //            segmentPair.hookPoint2 = segmentPair.entity2.Position;

        //            chain.segments.Add(CreateEntityHook(segmentPair, false, true, MIN_CHAIN_SEGMENT_LENGTH)); // TEST VALUE
        //        }
        //        else if (chain.SegmentsCount() == MAX_CHAIN_SEGMENTS - 1)
        //        {
        //            HookPair segmentPair = new HookPair(hook);
        //            segmentPair.ropeType = ChainSegmentRopeType;

        //            HookPair lastSegmentPair = chain.segments.Last();

        //            segmentPair.entity1 = lastSegmentPair.entity2;
        //            segmentPair.hookPoint1 = lastSegmentPair.hookPoint2 + (CHAIN_JOINT_OFFSET * lookAtDirection);
        //            segmentPair.hookOffset1 = lastSegmentPair.hookOffset2;

        //            chain.segments.Add(CreateEntityHook(segmentPair, false, true, MIN_CHAIN_SEGMENT_LENGTH));

        //            break;
        //        }
        //    }

        //    chains.Add(chain);
        //}


        private Prop CreateTargetProp(Vector3 position, bool isDynamic, bool hasCollision, bool isVisible, bool hasFrozenPosition, bool placeOnGround)
        {
            Prop targetProp = World.CreateProp(ropeHookPropModel, position, isDynamic, placeOnGround);

            targetProp.HasCollision = hasCollision;
            targetProp.IsVisible = isVisible;
            targetProp.FreezePosition = hasFrozenPosition;
            targetProp.IsInvincible = true;
            //targetProp.IsPersistent = true;

            return targetProp;
        }

        //private Prop CreateChainJointProp(Vector3 position)
        //{
        //    Prop jointProp = World.CreateProp(chainJointPropModel, position, true, false);

        //    jointProp.HasCollision = true;
        //    jointProp.IsVisible = SHOW_CHAIN_JOINT_PROP;
        //    jointProp.FreezePosition = false;
        //    jointProp.HasGravity = true;
        //    //jointProp.IsInvincible = true;
        //    //jointProp.IsPersistent = true;

        //    Function.Call(Hash.SET_OBJECT_PHYSICS_PARAMS, new InputArgument[] { jointProp.Handle, CHAIN_JOINT_PROP_MASS, -1.0f,
        //                -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f,   2*3.1415f, 1.0f});

        //    return jointProp;
        //}


        private RaycastResult CameraRaycastForward()
        {
            return Util.CameraRaycastForward();
        }
    }
}
