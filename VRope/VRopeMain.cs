
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using GTA;
using GTA.Native;
using GTA.Math;

using static VRope.Core;
using static VRope.InputModule;
using static VRope.RopeModule;

/*
 * 
 * created by jeffsturm4nn
 * 
 */

namespace VRope
{
    public class VRopeMain : Script
    {
        protected void ThisWillRunEveryFrame()
        {
            if (Game.Player.Character.IsInVehicle() && !Util.IsPlayerSittingInFlyingVehicle())
            {
                Vehicle vehicle = Util.GetVehiclePlayerIsIn();

                vehicle.ApplyForce(Util.AsVector(0f, 0f, -0.01f) * 70.0f);

                DebugInfo += " [Heavy Af] ";
            }
        }


        public VRopeMain()
        {
            try
            {
                ConfigFilePath = (Directory.GetCurrentDirectory() + "\\scripts\\VRope.ini");

                ProcessConfigFile();

                SortKeyTuples();

                if (EnableXBoxControllerInput)
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

        private void SortKeyTuples()
        {
            for (int i = 0; i < ControlKeys.Count; i++)
            {
                for (int j = 0; j < ControlKeys.Count - 1; j++)
                {
                    if (ControlKeys[j].keys.Count < ControlKeys[j + 1].keys.Count)
                    {
                        var keyPair = ControlKeys[j];

                        ControlKeys[j] = ControlKeys[j + 1];
                        ControlKeys[j + 1] = keyPair;
                    }
                }
            }
        }

        private void SortButtonTuples()
        {
            for (int i = 0; i < ControlButtons.Count; i++)
            {
                for (int j = 0; j < ControlButtons.Count - 1; j++)
                {
                    if (ControlButtons[j].state.buttonPressedCount < ControlButtons[j + 1].state.buttonPressedCount)
                    {
                        var buttonPair = ControlButtons[j];

                        ControlButtons[j] = ControlButtons[j + 1];
                        ControlButtons[j + 1] = buttonPair;
                    }
                }
            }
        }


        private void ProcessConfigFile()
        {
            try
            {
                ScriptSettings settings = ScriptSettings.Load(ConfigFilePath);

                if (!File.Exists(ConfigFilePath))
                {
                    UI.Notify("VRope Config File Error:\n" + ConfigFilePath + " could not be found.\nAll settings were set to default.", true);
                }

                ModActive = settings.GetValue("GLOBAL_VARS", "ENABLE_ON_GAME_LOAD", false);
                NoSubtitlesMode = settings.GetValue("GLOBAL_VARS", "NO_SUBTITLES_MODE", false);
                EnableXBoxControllerInput = settings.GetValue("GLOBAL_VARS", "ENABLE_XBOX_CONTROLLER_INPUT", true);

                XBoxController.LEFT_TRIGGER_THRESHOLD = settings.GetValue<byte>("CONTROL_XBOX_CONTROLLER", "LEFT_TRIGGER_THRESHOLD", 255);
                XBoxController.RIGHT_TRIGGER_THRESHOLD = settings.GetValue<byte>("CONTROL_XBOX_CONTROLLER", "RIGHT_TRIGGER_THRESHOLD", 255);

                //if (ENABLE_ROPE_MODULE)
                {
                    FreeRangeMode = settings.GetValue("ROPE_MECHANICS_VARS", "FREE_RANGE_MODE", true);
                    MinRopeLength = (settings.GetValue("ROPE_MECHANICS_VARS", "MIN_ROPE_LENGTH", MIN_MIN_ROPE_LENGTH * 10.0f) / 10.0f);
                    MaxHookCreationDistance = settings.GetValue("ROPE_MECHANICS_VARS", "MAX_HOOK_CREATION_DISTANCE", 70.0f);
                    MaxHookedEntityDistance = settings.GetValue("ROPE_MECHANICS_VARS", "MAX_HOOKED_ENTITY_DISTANCE", 150.0f);
                    MaxHookedPedDistance = settings.GetValue("ROPE_MECHANICS_VARS", "MAX_HOOKED_PED_DISTANCE", 70.0f);
                    RopeHookPropModel = settings.GetValue("ROPE_MECHANICS_VARS", "ROPE_HOOK_PROP_MODEL", "prop_golf_ball");
                    ShowHookRopeProp = settings.GetValue("ROPE_MECHANICS_VARS", "SHOW_ROPE_HOOK_PROP", true);
                    RopeWindingSpeed = (settings.GetValue("ROPE_MECHANICS_VARS", "ROPE_WINDING_SPEED", 15.0f) / 100.0f);

                    EntityToEntityHookRopeType = settings.GetValue<RopeType>("HOOK_ROPE_TYPES", "EntityToEntityHookRopeType", (RopeType)1);
                    PlayerToEntityHookRopeType = settings.GetValue<RopeType>("HOOK_ROPE_TYPES", "PlayerToEntityHookRopeType", (RopeType)4);
                }

                //chainJointPropModel = settings.GetValue("CHAIN_MECHANICS_VARS", "CHAIN_JOINT_PROP_MODEL", "prop_golf_ball");
                //SHOW_CHAIN_JOINT_PROP = settings.GetValue("CHAIN_MECHANICS_VARS", "SHOW_CHAIN_JOINT_PROP", true);
                //MAX_CHAIN_SEGMENTS = settings.GetValue("CHAIN_MECHANICS_VARS", "MAX_CHAIN_SEGMENTS", 15);
                //ChainSegmentRopeType = settings.GetValue<RopeType>("HOOK_ROPE_TYPES", "ChainSegmentRopeType", (RopeType)4);

                if (ENABLE_FORCE_MODULE)
                {
                    ForceMagnitude = settings.GetValue("FORCE_MECHANICS_VARS", "DEFAULT_FORCE_VALUE", 80.0f);
                    BalloonUpForce = settings.GetValue("FORCE_MECHANICS_VARS", "DEFAULT_BALLOON_UP_FORCE_VALUE", 45.0f);
                    ForceIncrementValue = settings.GetValue("FORCE_MECHANICS_VARS", "FORCE_INCREMENT_VALUE", 2.0f);
                    BalloonUpForceIncrement = settings.GetValue("FORCE_MECHANICS_VARS", "BALLOON_UP_FORCE_INCREMENT", 1.0f);
                    ContinuousForce = settings.GetValue("FORCE_MECHANICS_VARS", "CONTINUOUS_FORCE", false);
                    MaxBalloonHookAltitude = settings.GetValue("FORCE_MECHANICS_VARS", "MAX_BALLOON_HOOK_ALTITUDE", 200.0f);
                }


                if (ENABLE_TRANSPORT_MODULE)
                {
                    TransportEntitiesRadius = settings.GetValue("TRANSPORT_HOOKS_VARS", "TRANSPORT_ENTITIES_RADIUS", 34);
                    MinTransportRopeLength = settings.GetValue("TRANSPORT_HOOKS_VARS", "MIN_TRANSPORT_ROPE_LENGTH", 4.0f);
                    MinTransportPedRopeLength = settings.GetValue("TRANSPORT_HOOKS_VARS", "MIN_TRANSPORT_PED_ROPE_LENGTH", 2.0f);
                    TransportHooksRopeType = settings.GetValue<RopeType>("HOOK_ROPE_TYPES", "TransportHooksRopeType", (RopeType)3);
                }

                InitControlKeysFromConfig(settings);

                if (EnableXBoxControllerInput)
                    InitControllerButtonsFromConfig(settings);
            }
            catch (Exception e)
            {
                UI.Notify("VRope Config File Error: " + GetErrorMessage(e), false);
            }

        }


        private void ProcessHooks()
        {
            Entity playerEntity = Game.Player.Character;
            Vector3 playerPosition = Game.Player.Character.Position;
            bool deleteHook = false;

            if (Hooks.Count > 0)
            {
                if (playerEntity.Exists() && playerEntity.IsDead)
                {
                    DeleteAllHooks();
                    return;
                }

                for (int i = 0; i < Hooks.Count; i++)
                {
                    if (Hooks[i] == null)
                    {
                        if (DebugMode)
                            UI.Notify("Deleting Hook - NULL. i:" + i);

                        deleteHook = true;
                    }
                    else if (!Hooks[i].IsValid())
                    {
                        if (DebugMode)
                            UI.Notify("Deleting Hook - Invalid. i:" + i + 
                                "\nRope.null:" + (Hooks[i].rope == null).ToString());

                        deleteHook = true;
                    }
                    else if (!FreeRangeMode &&
                        (!Hooks[i].isEntity1ABalloon && !Hooks[i].isEntity2ABalloon) &&
                        (Hooks[i].entity1 != playerEntity &&
                        ((playerPosition.DistanceTo(Hooks[i].entity1.Position) > MaxHookedEntityDistance) ||
                        (playerPosition.DistanceTo(Hooks[i].entity2.Position) > MaxHookedEntityDistance))))
                    {
                        if (DebugMode)
                            UI.Notify("Deleting Hook - Too Far. i:" + i);

                        deleteHook = true;
                    }
                    else if ((Util.IsPed(Hooks[i].entity1) && Hooks[i].entity1.IsDead) ||
                            (Util.IsPed(Hooks[i].entity2) && Hooks[i].entity2.IsDead))
                    {
                        if (DebugMode)
                            UI.Notify("Deleting Ped Hook - Dead Ped. i:" + i);

                        deleteHook = true;
                    }
                    else if (((Util.IsPed(Hooks[i].entity1) && !Util.IsPlayer(Hooks[i].entity1)) || Util.IsPed(Hooks[i].entity2)) &&
                        ((playerPosition.DistanceTo(Hooks[i].entity1.Position) > MaxHookedPedDistance) ||
                        (playerPosition.DistanceTo(Hooks[i].entity2.Position) > MaxHookedPedDistance)))
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

                    if (Hooks[i].HasNPCPed())
                        ProcessPedsInHook(i);

                    if (Hooks[i].isEntity1ABalloon)
                        ProcessBalloonHook(i);

                    if (Hooks[i].isTransportHook)
                        ProcessTransportHook(i);

                    
                }
            }

        }


        private void ProcessPedsInHook(int hookIndex)
        {
            try
            {
                Ped ped = (Ped)(Util.IsNPCPed(Hooks[hookIndex].entity1) ? Hooks[hookIndex].entity1 : Hooks[hookIndex].entity2);


                if (ped.IsAlive && !ped.IsRagdoll)
                {
                    bool isWinding = Hooks[hookIndex].isWinding;
                    bool isUnwinding = Hooks[hookIndex].isUnwinding;

                    int lastHookIndex = Hooks.Count - 1;


                    if (!ped.IsInAir && !ped.IsInWater)
                    {
                        if (ped.Velocity.Length() > MAX_HOOKED_PED_SPEED ||
                            ped.HeightAboveGround > ped.Model.GetDimensions().Z * 0.48f ||
                            ped.IsInjured)
                        {
                            Util.MakePedRagdoll(ped, PED_RAGDOLL_DURATION);
                            RecreateEntityHook(hookIndex, true);

                            SetHookRopeWindingByIndex(hookIndex, isWinding);
                            SetHookRopeUnwindingByIndex(hookIndex, isUnwinding);
                        }
                    }
                    else
                    {
                        Util.MakePedRagdoll(ped, PED_RAGDOLL_DURATION);
                        RecreateEntityHook(hookIndex, true);

                        SetHookRopeWindingByIndex(hookIndex, isWinding);
                        SetHookRopeUnwindingByIndex(hookIndex, isUnwinding);
                    }

                }

            }
            catch (Exception exc)
            {
                UI.Notify("VRope ProcessPedsInHook() Error:\n" +
                    GetErrorMessage(exc, "Hook Count: " + Hooks.Count + " Hook Index: " + hookIndex) +
                    "\nMod execution halted.");

                DeleteAllHooks();
                ModRunning = false;
                ModActive = false;
            }
        }

        private void ProcessBalloonHook(int hookIndex)
        {
            Entity balloonEntity = Hooks[hookIndex].entity1;

            if (!Hooks[hookIndex].isEntity2AMapPosition && Util.IsPlayer(Hooks[hookIndex].entity1))
            {
                balloonEntity = Hooks[hookIndex].entity2;
            }

            Vector3 zAxis = new Vector3(0f, 0f, 0.01f);

            if (balloonEntity.HeightAboveGround < MaxBalloonHookAltitude)
                balloonEntity.ApplyForce(zAxis * BalloonUpForce);
        }

        private void ProcessTransportHook(int hookIndex)
        {
            Entity entity = Hooks[hookIndex].entity2;

            if (!entity.IsInAir)
            {
                if (Util.IsVehicle(entity))
                {
                    Vector3 zAxis = new Vector3(0f, 0f, 0.01f);

                    entity.ApplyForce(zAxis * 5.0f);
                }

                if (Util.IsPed(entity))
                {
                    Vector3 zAxis = new Vector3(0f, 0.01f, 0f);

                    entity.ApplyForce(zAxis * 2.0f);
                }
            }
            else
            {
                if (Util.IsVehicle(entity))
                {
                    //Vehicle playerVehicle = Util.GetVehiclePlayerIsIn();
                    Vector3 zAxisDown = new Vector3(0f, 0f, -0.01f);

                    entity.ApplyForce(zAxisDown * 40.0f);

                    //entity.Rotation = new Vector3(entity.Rotation.X, entity.Rotation.Y, playerVehicle.Rotation.Z);
                }
            }
        }

        //public void ProcessChains()
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


        private void UpdateDebugStuff()
        {
            DebugInfo += "Active Hooks: " + Hooks.Count;

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
                                    " Rotation(" + rot.X.ToString(format) + ", Y:" + rot.Y.ToString(format) + ", Z:" + rot.Z.ToString(format) + ")" +
                                    //"\n Velocity(" + vel.X.ToString(format) + ", Y:" + vel.Y.ToString(format) + ", Z:" + vel.Z.ToString(format) + ")" +
                                    "\n Speed(" + speed.ToString(format) + ") Distance(" + dist.ToString(format) + ")";
                    }
                }

                if (Hooks.Count > 0 && Hooks.Last() != null && Hooks.Last().Exists())
                {
                    if (Hooks.Last().entity1 != null)
                        DebugInfo += "\n | E1.Distance(" + Game.Player.Character.Position.DistanceTo(Hooks.Last().entity1.Position).ToString("0.00") + ")";

                    if (Hooks.Last().entity2 != null)
                        DebugInfo += " |E2.Distance(" + Game.Player.Character.Position.DistanceTo(Hooks.Last().entity2.Position).ToString("0.00") + ")";
                }

                Vector3 ppos = Game.Player.Character.Position;

                DebugInfo += "\n Player[" + " Speed(" + Game.Player.Character.Velocity.Length().ToString(format) + ")," +
                            " Position(X:" + ppos.X.ToString(format) + ", Y:" + ppos.Y.ToString(format) + ", Z:" + ppos.Z.ToString(format) + ") ]"
                            + "\nHookedPeds(" + HookedPedCount + ")"
                            + " Clock(" + DateTime.Now.ToString("HH:mm:ss") + ")";
            }
        }

        private void ShowScreenInfo()
        {
            if (!NoSubtitlesMode)
            {
                if (RopeHook.entity1 != null && RopeHook.entity2 == null)
                {
                    GlobalSubtitle += ("VRope: Select a second object to attach.\n");
                }

                if (ForceHook.entity1 != null && ForceHook.entity2 == null)
                {
                    GlobalSubtitle += ("VRope: Select the target object.\n");
                }

                if (SelectedHooks.Count > 0)
                {
                    GlobalSubtitle += "VRope: Objects Selected [ " + SelectedHooks.Count + " ].\n";
                }

                GlobalSubtitle += SubQueue.MountSubtitle();

                if (DebugMode)
                    GlobalSubtitle += "\n" + DebugInfo;


                UI.ShowSubtitle(GlobalSubtitle);
            }
        }


        public void OnTick(object sender, EventArgs e)
        {
            try
            {
                //long firstTime = Watch.ElapsedMilliseconds;

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

                if (TestAction)
                    ThisWillRunEveryFrame();

                //long elapsedTime = Watch.ElapsedMilliseconds - firstTime;
                //DebugInfo += "\n Loop Time(" + elapsedTime + " ms) ";

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
                for (int i = 0; i < ControlKeys.Count; i++)
                {
                    if (!ModActive || !ModRunning)
                    {
                        if (ControlKeys[i].name == "ToggleModActiveKey" && Keyboard.IsKeyListPressed(ControlKeys[i].keys))
                        {
                            ControlKeys[i].callback.Invoke();
                            ControlKeys[i].wasPressed = true;
                            break;
                        }
                    }
                    else
                    {
                        if (ControlKeys[i].condition.HasFlag(TriggerCondition.PRESSED) && Keyboard.IsKeyListPressed(ControlKeys[i].keys))
                        {
                            if (!ControlKeys[i].wasPressed)
                            {
                                ControlKeys[i].callback.Invoke();
                                ControlKeys[i].wasPressed = true;
                            }

                            break;
                        }
                        else if (ControlKeys[i].condition.HasFlag(TriggerCondition.HELD) && Keyboard.IsKeyListPressed(ControlKeys[i].keys))
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

    }
}
