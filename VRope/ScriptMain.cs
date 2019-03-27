
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
        
        private bool ENABLE_XBOX_CONTROLLER_INPUT;
        private bool FREE_RANGE_MODE;
        private String CONFIG_FILE_NAME;
        private int UPDATE_INTERVAL;
        private float MIN_ROPE_LENGTH; 
        private float MAX_HOOK_CREATION_DISTANCE; 
        private float MAX_HOOKED_ENTITY_DISTANCE;
        private byte LEFT_TRIGGER_THRESHOLD;
        private byte RIGHT_TRIGGER_THRESHOLD;

        private bool CONTINUOUS_FORCE;
        private int FORCE_INCREMENT_VALUE;
        private float FORCE_SCALE_FACTOR = 1.4f;

        private const int INIT_HOOK_LIST_CAPACITY = 500;
        private const float  MAX_HOOKED_PED_SPEED = 0.57f;
        private const int PED_RAGDOLL_DURATION = 7000;
        private const char SEPARATOR_CHAR = '+';

        private SubtitleQueue subQueue = new SubtitleQueue();
        private Keyboard keyboard = new Keyboard();

        private bool ModActive = false;
        public bool ModRunning = false;
        private bool FirstTime = true;
        private bool DebugMode = false;
        
        private Model targetPropModel;

        private String DebugInfo = "";
        private String GlobalSubtitle = ""; 

        private List<HookGroup> hooks = new List<HookGroup>(INIT_HOOK_LIST_CAPACITY);
        private HookGroup globalHook = new HookGroup();
        private HookGroup forceHook = new HookGroup();
        private Pair<Vector3, Vector3> prevEntityPosition = new Pair<Vector3, Vector3>();

        private List<Keys> ToggleModActiveKey;
        private List<Keys> AttachPlayerToEntityKey;
        private List<Keys> AttachEntityToEntityKey;
        private List<Keys> DeleteLastHookKey;
        private List<Keys> DeleteAllHooksKey;
        private List<Keys> WindLastHookRopeKey;
        private List<Keys> WindAllHookRopesKey;
        private List<Keys> UnwindLastHookRopeKey;
        private List<Keys> UnwindAllHookRopesKey;
        private List<Keys> ApplyForceKey;
        private List<Keys> ApplyInvertedForceKey;
        private List<Keys> ToggleDebugInfoKey;
        private List<Keys> IncreaseForceKey;
        private List<Keys> DecreaseForceKey;
        private List<Keys> ApplyForceAttachedPairKey;

        private KeyLists controlKeys = new KeyLists();

        private Gamepad AttachPlayerToEntityButton;
        private Gamepad AttachEntityToEntityButton;
        private Gamepad DeleteLastHookButton;
        private Gamepad DeleteAllHooksButton;
        private Gamepad WindLastHookRopeButton;
        private Gamepad WindAllHookRopesButton;
        private Gamepad UnwindLastHookRopeButton;
        private Gamepad UnwindAllHookRopesButton;
        private Gamepad ApplyForceButton;
        private Gamepad ApplyInvertedForceButton;
        private Gamepad IncreaseForceButton;
        private Gamepad DecreaseForceButton;
        private Gamepad ApplyForceAttachedPairButton;

        private RopeType EntityToEntityHookRopeType;
        private RopeType PlayerToEntityHookRopeType;

        private int ForceMagnitude;

        public ScriptMain()
        {
            try
            {
                CONFIG_FILE_NAME = (Directory.GetCurrentDirectory() + "\\scripts\\VRope.ini");
                ProcessConfigFile();

                keyboard.ExtractUniqueKeys(controlKeys);

                if(ENABLE_XBOX_CONTROLLER_INPUT)
                    XBoxController.CheckForController();

                targetPropModel = new Model("prop_golf_ball"); //We don't talk about this. Keep scrolling.

                Tick += OnTick;
                //KeyDown += OnKeyDown;
                //KeyUp += OnKeyUp;
                
                Interval = UPDATE_INTERVAL;
            }
            catch(Exception exc)
            {
                UI.Notify("VRope Init Error:\n" + exc.Message);
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

        private Gamepad TranslateButtonStringToButtonData(String buttonData)
        {
            if (buttonData == null || buttonData.Length == 0)
                return new Gamepad();

            buttonData = buttonData.Replace(" ", "");

            Gamepad resultData = new Gamepad();
            resultData.LeftTrigger = 0;
            resultData.RightTrigger = 0;

            String[] buttonStrings = buttonData.Split(SEPARATOR_CHAR);

            for (int i=0; i<buttonStrings.Length; i++)
            {
                if (buttonStrings[i] != "LeftTrigger" && buttonStrings[i] != "RightTrigger")
                {
                    GamepadButtonFlags buttonFlag = (GamepadButtonFlags)Enum.Parse(typeof(GamepadButtonFlags), buttonStrings[i]);

                    resultData.Buttons |= buttonFlag; 
                }
                else if(buttonStrings[i] == "LeftTrigger")
                {
                    resultData.LeftTrigger = LEFT_TRIGGER_THRESHOLD;
                }
                else if(buttonStrings[i] == "RightTrigger")
                {
                    resultData.RightTrigger = RIGHT_TRIGGER_THRESHOLD;
                }
            }

            return resultData;
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


        public void ProcessConfigFile()
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

                UPDATE_INTERVAL = settings.GetValue<int>("GLOBAL_VARS", "UPDATE_INTERVAL", 16);
                MIN_ROPE_LENGTH = settings.GetValue<float>("GLOBAL_VARS", "MIN_ROPE_LENGTH", 1.0f);
                MAX_HOOK_CREATION_DISTANCE = settings.GetValue<float>("GLOBAL_VARS", "MAX_HOOK_CREATION_DISTANCE", 70);
                MAX_HOOKED_ENTITY_DISTANCE = settings.GetValue<float>("GLOBAL_VARS", "MAX_HOOKED_ENTITY_DISTANCE", 145);

                LEFT_TRIGGER_THRESHOLD = settings.GetValue<byte>("CONTROL_XBOX_CONTROLLER", "LEFT_TRIGGER_THRESHOLD", 255);
                RIGHT_TRIGGER_THRESHOLD = settings.GetValue<byte>("CONTROL_XBOX_CONTROLLER", "RIGHT_TRIGGER_THRESHOLD", 255);

                EntityToEntityHookRopeType = settings.GetValue<RopeType>("HOOK_ROPE_TYPES", "EntityToEntityHookRopeType", (RopeType)4);
                PlayerToEntityHookRopeType = settings.GetValue<RopeType>("HOOK_ROPE_TYPES", "PlayerToEntityHookRopeType", (RopeType)3);

                ForceMagnitude = settings.GetValue<int>("FORCE_GUN_VARS", "DEFAULT_FORCE_VALUE", 20);
                FORCE_INCREMENT_VALUE = settings.GetValue<int>("FORCE_GUN_VARS", "FORCE_INCREMENT_VALUE", 2);
                CONTINUOUS_FORCE = settings.GetValue<bool>("FORCE_GUN_VARS", "CONTINUOUS_FORCE", false);

                try
                {
                    ToggleModActiveKey = TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "ToggleModActiveKey", "None"));
                    AttachPlayerToEntityKey = TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "AttachPlayerToEntityKey", "None"));
                    AttachEntityToEntityKey = TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "AttachEntityToEntityKey", "None"));
                    DeleteLastHookKey = TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "DeleteLastHookKey", "None"));
                    DeleteAllHooksKey = TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "DeleteAllHooksKey", "None"));
                    WindLastHookRopeKey = TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "WindLastHookRopeKey", "None"));
                    WindAllHookRopesKey = TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "WindAllHookRopesKey", "None"));
                    UnwindLastHookRopeKey = TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "UnwindLastHookRopeKey", "None"));
                    UnwindAllHookRopesKey = TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "UnwindAllHookRopesKey", "None"));
                    ApplyForceKey = TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "ApplyForceKey", "None"));
                    ApplyInvertedForceKey = TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "ApplyInvertedForceKey", "None"));
                    IncreaseForceKey = TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "IncreaseForceKey", "None"));
                    DecreaseForceKey = TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "DecreaseForceKey", "None"));
                    ApplyForceAttachedPairKey = TranslateKeyDataToKeyList(settings.GetValue<String>("CONTROL_KEYBOARD", "ApplyForceAttachedPairKey", "None"));

                    ToggleDebugInfoKey = TranslateKeyDataToKeyList(settings.GetValue<String>("DEV_STUFF", "ToggleDebugInfoKey", "None")); //User build version
                }
                catch (Exception exc)
                {
                    UI.Notify("VRope Key Config Error:\n" + exc.Message + "\nInvalid ", false);
                }

                try
                {
                    AttachPlayerToEntityButton = TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "AttachPlayerToEntityButton", "None"));
                    AttachEntityToEntityButton = TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "AttachEntityToEntityButton", "None"));
                    DeleteLastHookButton = TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "DeleteLastHookButton", "None"));
                    DeleteAllHooksButton = TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "DeleteAllHooksButton", "None"));
                    WindLastHookRopeButton = TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "WindLastHookRopeButton", "None"));
                    WindAllHookRopesButton = TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "WindAllHookRopesButton", "None"));
                    UnwindLastHookRopeButton = TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "UnwindLastHookRopeButton", "None"));
                    UnwindAllHookRopesButton = TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "UnwindAllHookRopesButton", "None"));
                    ApplyForceButton = TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "ApplyForceButton", "None"));
                    ApplyInvertedForceButton = TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "InvertForceDirectionButton", "None"));
                    IncreaseForceButton = TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "IncreaseForceButton", "None"));
                    DecreaseForceButton = TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "DecreaseForceButton", "None"));
                    ApplyForceAttachedPairButton = TranslateButtonStringToButtonData(settings.GetValue<String>("CONTROL_XBOX_CONTROLLER", "ApplyForceAttachedPairButton", "None"));
                }
                catch (Exception exc)
                {
                    UI.Notify("VRope Controller Config Error:\n" + exc.Message, false);
                }
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

                    ProcessAsPeds(i);
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

        private void ProcessAsPeds(int hookIndex)
        {
            Entity entity1 = hooks[hookIndex].entity1;
            Entity entity2 = hooks[hookIndex].entity2;
            bool ropeWinding = hooks[hookIndex].isWinding;
            bool ropeUnwinding = hooks[hookIndex].isUnwinding;

            if (!ropeWinding && !ropeUnwinding)
            {
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
                                    RecreateEntityHook(hookIndex);
                                }
                            }
                            else
                            {
                                Util.MakePedRagdoll(ped1, PED_RAGDOLL_DURATION);
                                RecreateEntityHook(hookIndex); 
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
                        globalHook.entity1 = Game.Player.Character;
                        globalHook.entity2 = rayResult.HitEntity;
                        globalHook.hookPoint1 = Game.Player.Character.GetBoneCoord((Bone)57005);
                        globalHook.hookPoint2 = rayResult.HitCoords;
                        globalHook.ropeType = PlayerToEntityHookRopeType;

                        globalHook.hookOffset1 = globalHook.hookPoint1 - globalHook.entity1.Position;
                        prevEntityPosition.first = Game.Player.Character.Position;

                        if (rayResult.DitHitEntity && Util.IsValid(rayResult.HitEntity))
                        {
                            prevEntityPosition.second = rayResult.HitEntity.Position;
                            globalHook.hookOffset2 = globalHook.hookPoint2 - globalHook.entity2.Position;
                            globalHook.isEntity2AMapPosition = false;
                        }
                        else
                        {
                            prevEntityPosition.second = rayResult.HitCoords;
                            globalHook.hookOffset2 = Vector3.Zero;
                            globalHook.isEntity2AMapPosition = true;
                        }

                        if (FREE_RANGE_MODE || 
                            globalHook.entity1.Position.DistanceTo(globalHook.hookPoint2) < MAX_HOOK_CREATION_DISTANCE)
                            CreateHook(globalHook);

                        globalHook.entity1 = null;
                        globalHook.entity2 = null;
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
                    if (globalHook.entity1 == null)
                    {
                        if (globalHook.entity2 != null)
                            globalHook.entity2 = null;

                        globalHook.entity1 = rayResult.HitEntity;
                        globalHook.hookPoint1 = rayResult.HitCoords;
                        globalHook.hookOffset1 = globalHook.hookPoint1 - globalHook.entity1.Position;
                        prevEntityPosition.first = rayResult.HitEntity.Position;
                    }
                    else if (globalHook.entity2 == null)
                    {
                        globalHook.entity2 = rayResult.HitEntity;
                        globalHook.hookPoint2 = rayResult.HitCoords;
                        globalHook.hookOffset2 = globalHook.hookPoint2 - globalHook.entity2.Position;
                        prevEntityPosition.second = rayResult.HitEntity.Position;

                        //Player attachment not allowed here.
                        if (globalHook.entity2 == globalHook.entity1 ||
                            globalHook.entity2 == playerEntity ||
                            globalHook.entity1 == playerEntity)
                        {
                            globalHook.entity1 = null;
                            globalHook.entity2 = null;
                        }
                    }

                    if (globalHook.entity1 != null && globalHook.entity2 != null)
                    {
                        if (FREE_RANGE_MODE || globalHook.entity1.Position.DistanceTo(globalHook.entity2.Position) < MAX_HOOK_CREATION_DISTANCE)
                        {
                            globalHook.ropeType = EntityToEntityHookRopeType;
                            globalHook.isEntity2AMapPosition = false;

                            CreateHook(globalHook);
                        }

                        globalHook.entity1 = null;
                        globalHook.entity2 = null;
                    }
                }
                else if (rayResult.DitHitAnything)
                {
                    if (globalHook.entity1 != null && globalHook.entity2 == null &&
                       (FREE_RANGE_MODE || globalHook.entity1.Position.DistanceTo(rayResult.HitCoords) < MAX_HOOK_CREATION_DISTANCE))
                    {
                        globalHook.hookPoint2 = rayResult.HitCoords;
                        globalHook.ropeType = EntityToEntityHookRopeType;
                        globalHook.isEntity2AMapPosition = true;
                        globalHook.hookOffset2 = Vector3.Zero;

                        CreateHook(globalHook);
                    }

                    globalHook.entity1 = null;
                    globalHook.entity2 = null;
                }
                else
                {
                    globalHook.entity1 = null;
                    globalHook.entity2 = null;
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


        private void ApplyForceProc(bool invertForce = false)
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

                    rayResult.HitEntity.ApplyForce(offset + (forceDirection * ForceMagnitude));
                }
            }
        }

        private void ApplyForceAttachedPairProc()
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
                            //if (FREE_RANGE_MODE || forceHook.entity1.Position.DistanceTo(forceHook.entity2.Position) < MAX_HOOK_CREATION_DISTANCE)
                            {
                                Vector3 entity2HookPosition = forceHook.entity2.Position + forceHook.hookOffset2;
                                Vector3 entity1HookPosition = forceHook.entity1.Position + forceHook.hookOffset1;
                                Vector3 distanceVector = entity2HookPosition - entity1HookPosition;
                                Vector3 lookAtDirection = distanceVector.Normalized;

                                subQueue.AddSubtitle(99, "Applying Force", 50);

                                if (Util.IsPed(forceHook.entity1))
                                    Util.MakePedRagdoll((Ped)forceHook.entity1, PED_RAGDOLL_DURATION);

                                forceHook.entity1.ApplyForce(forceHook.hookOffset1 + (lookAtDirection * ForceMagnitude * FORCE_SCALE_FACTOR));
                            }

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
                            Vector3 distanceVector = forceHook.hookPoint2 - entity1HookPosition;
                            Vector3 lookAtDirection = distanceVector.Normalized;

                            subQueue.AddSubtitle(99, "Applying Force", 50);

                            if (Util.IsPed(forceHook.entity1))
                                Util.MakePedRagdoll((Ped)forceHook.entity1, PED_RAGDOLL_DURATION);

                            forceHook.entity1.ApplyForce(forceHook.hookOffset1 + (lookAtDirection * ForceMagnitude * FORCE_SCALE_FACTOR));
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

        private void CheckForForceChangeInput()
        {
            bool showForceInfo = false;

            if(CONTINUOUS_FORCE)
            {
                if (Keyboard.IsKeyListPressed(ApplyInvertedForceKey))
                    ApplyForceProc(true);
                else if (Keyboard.IsKeyListPressed(ApplyForceKey))
                    ApplyForceProc(false);
            }

            if (Keyboard.IsKeyListPressed(DecreaseForceKey))
            {
                ForceMagnitude -= FORCE_INCREMENT_VALUE;
                showForceInfo = true;
            }
            else if (Keyboard.IsKeyListPressed(IncreaseForceKey))
            {
                ForceMagnitude += FORCE_INCREMENT_VALUE;
                showForceInfo = true;
            }

            if (showForceInfo)
                subQueue.AddSubtitle(14, "VRope Force: " + ForceMagnitude, 10);
                //GlobalSubtitle += ("VRope Force: " + ForceMagnitude + "\n\n\n");
        }


        private void ProcessXBoxControllerInput()
        {
            XBoxController.UpdateStateBegin();

            if (XBoxController.WasControllerButtonPressed(AttachPlayerToEntityButton))
            {
                AttachPlayerToEntityProc();
            }
            else if(XBoxController.WasControllerButtonPressed(AttachEntityToEntityButton))
            {
                AttachEntityToEntityProc();
            }
            else if (XBoxController.WasControllerButtonPressed(DeleteLastHookButton))
            {
                DeleteLastHookProc();
            }
            else if(XBoxController.WasControllerButtonPressed(DeleteAllHooksButton))
            {
                DeleteAllHooks();
            }

            else if(XBoxController.IsControllerButtonPressed(ApplyForceButton))
            {
                ApplyForceProc(false);
            }
            else if(XBoxController.IsControllerButtonPressed(ApplyInvertedForceButton))
            {
                ApplyForceProc(true);
            }


            if (XBoxController.WasControllerButtonPressed(UnwindLastHookRopeButton))
            {
                SetLastHookRopeUnwindingProc(true);
            }
            else if (XBoxController.WasControllerButtonPressed(WindLastHookRopeButton))
            {
                if (DebugMode)
                    GlobalSubtitle += "SetLastHookRopeWindingProc(true);";

                SetLastHookRopeWindingProc(true);
            }
            else if (XBoxController.WasControllerButtonPressed(UnwindAllHookRopesButton))
            {
                SetAllHookRopesUnwindingProc(true);
            }
            else if (XBoxController.WasControllerButtonPressed(WindAllHookRopesButton))
            {
                SetAllHookRopesWindingProc(true);
            }

            else if (XBoxController.WasControllerButtonReleased(UnwindAllHookRopesButton) ||
                    XBoxController.WasControllerButtonReleased(UnwindLastHookRopeButton))
            {
                SetAllHookRopesUnwindingProc(false);
            }
            else if (XBoxController.WasControllerButtonReleased(WindLastHookRopeButton) ||
                    XBoxController.WasControllerButtonReleased(WindAllHookRopesButton))
            {
                SetAllHookRopesWindingProc(false);
            }

            else if(XBoxController.WasControllerButtonPressed(ApplyForceButton))
            {
                ApplyForceProc(false);
            }
            else if(XBoxController.WasControllerButtonPressed(ApplyInvertedForceButton))
            {
                ApplyForceProc(true);
            }
            else if(XBoxController.WasControllerButtonPressed(ApplyForceAttachedPairButton))
            {
                ApplyForceAttachedPairProc();
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
            if (globalHook.entity1 != null && globalHook.entity2 == null)
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


        public void InitKeyboardInput()
        {
            keyboard.ExtractUniqueKeys(controlKeys);
        }

        private void ProcessKeyboardInput()
        {
            keyboard.UpdateStateBegin();




            keyboard.UpdateStateEnd();
        }

        public void OnTick(object sender, EventArgs e)
        {
            try
            {
                GlobalSubtitle = "";
                DebugInfo = "";

                ProcessKeyboardInput();

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

                CheckForForceChangeInput();

                ProcessHooks();

                ShowScreenInfo();
            }
            catch (Exception exc)
            {
                UI.Notify("VRope Runtime Error:\n" + exc.Message + "\nMod execution halted.");
                DeleteAllHooks();
                ModRunning = false;
                ModActive = false;
            }
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (Keyboard.IsKeyListPressed(ToggleModActiveKey))
                {
                    UI.ShowSubtitle("VRope " + (ModActive ? "Disabled" : "Enabled") + "\n\n\n\n\n");
                    ModActive = !ModActive;
                    Script.Wait(1200);
                }

                if (!ModActive || !ModRunning)
                {
                    Script.Wait(0);
                    return;
                }
                
                //===================================================
                //else if (Keyboard.IsKeyListPressed(AttachPlayerToEntityKey))
                //{
                //    AttachPlayerToEntityProc();
                //}
                //else if (Keyboard.IsKeyListPressed(AttachEntityToEntityKey))
                //{
                //    AttachEntityToEntityProc();
                //}
                //else if (Keyboard.IsKeyListPressed(DeleteAllHooksKey))
                //{
                //    DeleteAllHooks();
                //}
                //else if (Keyboard.IsKeyListPressed(DeleteLastHookKey))
                //{
                //    DeleteLastHookProc();
                //}
                //else if (Keyboard.IsKeyListPressed(WindLastHookRopeKey))
                //{
                //    SetLastHookRopeWindingProc(true);
                //}
                //else if (Keyboard.IsKeyListPressed(WindAllHookRopesKey))
                //{
                //    SetAllHookRopesWindingProc(true);
                //}
                //else if (Keyboard.IsKeyListPressed(UnwindLastHookRopeKey))
                //{
                //    SetLastHookRopeUnwindingProc(true);
                //}
                //else if (Keyboard.IsKeyListPressed(UnwindAllHookRopesKey))
                //{
                //    SetAllHookRopesUnwindingProc(true);
                //}

                //else if(!CONTINUOUS_FORCE && Keyboard.IsKeyListPressed(ApplyForceKey))
                //{
                //    ApplyForceProc(false);
                //}
                //else if (!CONTINUOUS_FORCE && Keyboard.IsKeyListPressed(ApplyInvertedForceKey))
                //{
                //    ApplyForceProc(true);
                //}

                //else if(Keyboard.IsKeyListPressed(ApplyForceAttachedPairKey))
                //{
                //    ApplyForceAttachedPairProc();
                //}

                //else if (Keyboard.IsKeyListPressed(ToggleDebugInfoKey))
                //{
                //    DebugMode = !DebugMode;
                //}
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

                //if (e.KeyCode == Keys.Y)
                //{
                //    SetLastHookRopeWindingProc(false);
                //}
                //else if (e.KeyCode == Keys.J)
                //{
                //    SetAllHookRopesWindingProc(false);
                //}
                //else if (Keyboard.IsKeyListPressed(UnwindLastHookRopeKey))
                //{
                //    SetLastHookRopeUnwindingProc(false);
                //}
                //else if (Keyboard.IsKeyListPressed(UnwindAllHookRopesKey))
                //{
                //    SetAllHookRopesUnwindingProc(false);
                //}
            }
            catch (Exception exc)
            {
                UI.Notify("VRope OnKeyUp Error:\n" + exc.Message, false);
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
