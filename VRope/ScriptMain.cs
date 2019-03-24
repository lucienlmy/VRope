
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
        private int FORCE_INCREMENT_VALUE;

        private const int INIT_HOOK_LIST_CAPACITY = 500;
        private const float  MAX_HOOKED_PED_SPEED = 0.57f;
        private const int PED_RAGDOLL_DURATION = 7000;
        private const char SEPARATOR_CHAR = '+';

        private bool ModActive = false;
        public bool ModRunning = false;
        private bool FirstTime = true;
        private bool DebugMode = false;
        
        private Model targetPropModel;

        private String DebugInfo = "";
        private String GlobalSubtitle = ""; 

        private List<HookGroup> hooks = new List<HookGroup>(INIT_HOOK_LIST_CAPACITY);
        private HookGroup globalHook = new HookGroup();
        private Pair<Vector3, Vector3> prevEntityPosition = new Pair<Vector3, Vector3>();

        private Keys ToggleModActiveKey;
        private Keys AttachPlayerToEntityKey;
        private Keys AttachEntityToEntityKey;
        private Keys DeleteLastHookKey;
        private Keys DeleteAllHooksKey;
        private Keys WindLastHookRopeKey;
        private Keys WindAllHookRopesKey;
        private Keys UnwindLastHookRopeKey;
        private Keys UnwindAllHookRopesKey;
        private Keys ApplyForceKey;
        private Keys InvertForceDirectionKey;
        private Keys ToggleDebugInfoKey;

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

        private RopeType EntityToEntityHookRopeType;
        private RopeType PlayerToEntityHookRopeType;

        private int ForceMagnitude;


        public ScriptMain()
        {
            try
            {
                CONFIG_FILE_NAME = (Directory.GetCurrentDirectory() + "\\scripts\\VRope.ini");
                ReadConfigFile();
                
                if(ENABLE_XBOX_CONTROLLER_INPUT)
                    XBoxController.CheckForController();

                targetPropModel = new Model("prop_golf_ball"); //We don't talk about this. Keep scrolling.

                Tick += OnTick;
                KeyDown += OnKeyDown;
                KeyUp += OnKeyUp;
                
                Interval = UPDATE_INTERVAL;
            }
            catch(Exception exc)
            {
                UI.Notify("VRope Init Error:\n" + exc.Message);
            }
        }

        ~ScriptMain()
        {
            //if(CLEAR_ALL_ON_MOD_RELOAD)
            {
                DeleteAllHooks();
                ModActive = false;
                ModRunning = false;
            }
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
        
        public void ReadConfigFile()
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

                try
                {
                    ToggleModActiveKey = settings.GetValue<Keys>("CONTROL_KEYBOARD", "ToggleModActiveKey", Keys.F11);
                    AttachPlayerToEntityKey = settings.GetValue<Keys>("CONTROL_KEYBOARD", "AttachPlayerToEntityKey", Keys.T);
                    AttachEntityToEntityKey = settings.GetValue<Keys>("CONTROL_KEYBOARD", "AttachEntityToEntityKey", Keys.H);
                    DeleteLastHookKey = settings.GetValue<Keys>("CONTROL_KEYBOARD", "DeleteLastHookKey", Keys.X);
                    DeleteAllHooksKey = settings.GetValue<Keys>("CONTROL_KEYBOARD", "DeleteAllHooksKey", Keys.Z);
                    WindLastHookRopeKey = settings.GetValue<Keys>("CONTROL_KEYBOARD", "WindLastHookRopeKey", Keys.Y);
                    WindAllHookRopesKey = settings.GetValue<Keys>("CONTROL_KEYBOARD", "WindAllHookRopesKey", Keys.J);
                    UnwindLastHookRopeKey = settings.GetValue<Keys>("CONTROL_KEYBOARD", "UnwindLastHookRopeKey", Keys.U);
                    UnwindAllHookRopesKey = settings.GetValue<Keys>("CONTROL_KEYBOARD", "UnwindAllHookRopesKey", Keys.K);
                    ApplyForceKey = settings.GetValue<Keys>("CONTROL_KEYBOARD", "ApplyForceKey", Keys.E);
                    InvertForceDirectionKey = settings.GetValue<Keys>("CONTROL_KEYBOARD", "InvertForceDirectionKey", Keys.LShiftKey);

                    ToggleDebugInfoKey = settings.GetValue<Keys>("DEV_STUFF", "ToggleDebugInfoKey", Keys.KeyCode); //User build version
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
                        prevEntityPosition.first = Game.Player.Character.Position;

                        if (rayResult.DitHitEntity && Util.IsValid(rayResult.HitEntity))
                        {
                            prevEntityPosition.second = rayResult.HitEntity.Position;
                            globalHook.isEntity2AMapPosition = false;
                        }
                        else
                        {
                            prevEntityPosition.second = rayResult.HitCoords;
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
                        prevEntityPosition.first = rayResult.HitEntity.Position;
                    }
                    else if (globalHook.entity2 == null)
                    {
                        globalHook.entity2 = rayResult.HitEntity;
                        globalHook.hookPoint2 = rayResult.HitCoords;
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

        private void ApplyForceAtAimedObject(bool invertForce = false)
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

        private void CheckForceModifInput()
        {
            bool showForceInfo = false;

            if (Game.Player.IsAiming)
            {
                if(Util.IsKeyPressed(ApplyForceKey))
                {
                    bool invertForce = false;
                    
                    if (Util.IsKeyPressed(InvertForceDirectionKey))
                        invertForce = true;

                    ApplyForceAtAimedObject(invertForce);
                }
                else if (Util.IsKeyPressed(Keys.LMenu))
                {
                    if (Game.IsControlPressed(2, GTA.Control.WeaponWheelNext))
                    {
                        ForceMagnitude += FORCE_INCREMENT_VALUE;
                        showForceInfo = true; 
                    }

                    if (Game.IsControlPressed(1, GTA.Control.WeaponWheelPrev))
                    {
                        ForceMagnitude -= FORCE_INCREMENT_VALUE;
                        showForceInfo = true;
                    }
                }
            }

            if(showForceInfo)
                GlobalSubtitle += ("VRope Force: " + ForceMagnitude + "\n\n\n");
        }


        //private void CheckForXBoxController()
        //{
        //    UserIndex[] indexes = new UserIndex[] { UserIndex.One, UserIndex.Two, UserIndex.Three, UserIndex.Four };
            
        //    for(int i=0; i<indexes.Length; i++)
        //    {
        //        Controller controller = new Controller(indexes[i]);

        //        if (controller.IsConnected)
        //        {
        //            xboxController = controller;
        //            break;
        //        }
        //        else
        //        {
        //            controller = null;
        //        }
        //    }
        //}

        //private bool wasControllerButtonPressed(Gamepad buttonData)
        //{
        //    return (isControllerButtonPressed(buttonData, newControllerState) &&
        //            !isControllerButtonPressed(buttonData, oldControllerState));
        //}

        //private bool wasControllerButtonReleased(Gamepad buttonData)
        //{
        //    return (isControllerButtonPressed(buttonData, oldControllerState) &&
        //            !isControllerButtonPressed(buttonData, newControllerState));
        //}

        //private bool isControllerButtonPressed(Gamepad buttonData, State state)
        //{
        //    Gamepad stateData = state.Gamepad;

        //    bool isPressed = stateData.Buttons.HasFlag(buttonData.Buttons);

        //    if(buttonData.LeftTrigger > 0)
        //        isPressed = isPressed && (stateData.LeftTrigger >= buttonData.LeftTrigger);

        //    if(buttonData.RightTrigger > 0)
        //        isPressed = isPressed && (stateData.RightTrigger >= buttonData.RightTrigger);

        //    return isPressed;
        //}

        //private bool isControllerButtonReleased(Gamepad buttonData, State state)
        //{
        //    Gamepad stateData = state.Gamepad;

        //    bool isPressed = !stateData.Buttons.HasFlag(buttonData.Buttons);

        //    if (buttonData.LeftTrigger > 0)
        //        isPressed = isPressed && (stateData.LeftTrigger < buttonData.LeftTrigger);

        //    if (buttonData.RightTrigger > 0)
        //        isPressed = isPressed && (stateData.RightTrigger < buttonData.RightTrigger);

        //    return isPressed;
        //}

        //private bool isControllerButtonPressed(Gamepad buttonData)
        //{
        //    return isControllerButtonPressed(buttonData, newControllerState);
        //}

        //private bool isControllerButtonReleased(Gamepad buttonData)
        //{
        //    return isControllerButtonReleased(buttonData, newControllerState);
        //}

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
                ApplyForceAtAimedObject(false);
            }
            else if(XBoxController.IsControllerButtonPressed(ApplyInvertedForceButton))
            {
                ApplyForceAtAimedObject(true);
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

            if (DebugMode)
                GlobalSubtitle += "\n" + DebugInfo;

            UI.ShowSubtitle(GlobalSubtitle);
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

                CheckForceModifInput();

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
                if (e.KeyCode == ToggleModActiveKey)
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
                
                //=========================================TEST

                //===================================================
                else if (e.KeyCode == AttachPlayerToEntityKey)
                {
                    AttachPlayerToEntityProc();
                }
                else if (e.KeyCode == AttachEntityToEntityKey)
                {
                    AttachEntityToEntityProc();
                }
                else if (e.KeyCode == DeleteAllHooksKey)
                {
                    DeleteAllHooks();
                }
                else if (e.KeyCode == DeleteLastHookKey)
                {
                    DeleteLastHookProc();
                }
                else if (e.KeyCode == WindLastHookRopeKey)
                {
                    SetLastHookRopeWindingProc(true);
                }
                else if (e.KeyCode == WindAllHookRopesKey)
                {
                    SetAllHookRopesWindingProc(true);
                }
                else if (e.KeyCode == UnwindLastHookRopeKey)
                {
                    SetLastHookRopeUnwindingProc(true);
                }
                else if (e.KeyCode == UnwindAllHookRopesKey)
                {
                    SetAllHookRopesUnwindingProc(true);
                }
                else if (e.KeyCode == ToggleDebugInfoKey)
                {
                    DebugMode = !DebugMode;
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

                if (e.KeyCode == Keys.Y)
                {
                    SetLastHookRopeWindingProc(false);
                }
                else if (e.KeyCode == Keys.J)
                {
                    SetAllHookRopesWindingProc(false);
                }
                else if (e.KeyCode == UnwindLastHookRopeKey)
                {
                    SetLastHookRopeUnwindingProc(false);
                }
                else if (e.KeyCode == UnwindAllHookRopesKey)
                {
                    SetAllHookRopesUnwindingProc(false);
                }
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

                Vector3 hookOffset1 = hook.hookPoint1 - prevEntityPosition.first;
                Vector3 entity1HookPosition = hook.entity1.Position + hookOffset1;
                Vector3 hookOffset2 = Vector3.Zero;
                Vector3 entity2HookPosition = Vector3.Zero;

                if (hook.isEntity2AMapPosition)
                {
                    hook.entity2 = CreateTargetProp(hook.hookPoint2, false, true, true, true, false);

                    hookOffset2 = Vector3.Zero;
                    entity2HookPosition = hook.hookPoint2;
                }
                else
                {
                    hookOffset2 = hook.hookPoint2 - prevEntityPosition.second;
                    entity2HookPosition = hook.entity2.Position + hookOffset2;
                }

                UI.Notify("H:" + (hook == null) + " E1:" + (hook.entity1 == null) + " E2:" + (hook.entity2 == null));

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
