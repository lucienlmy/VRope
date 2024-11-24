﻿using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;

/*
 * 
 * created by jeffsturm4nn
 * 
 */

namespace VRope
{
    public static class Core
    {
        public const String MOD_NAME = "VRope";
        public const String MOD_DEVELOPER = "jeffsturm4nn"; // :D
        public const int VERSION_MINOR = 0;
        public const int VERSION_BUILD = 13;
        public const String VERSION_SUFFIX = "a {DevBuild}";

        public const int UPDATE_INTERVAL = 10; //milliseconds.
        public const int UPDATE_FPS = (1000 / UPDATE_INTERVAL); //100 FPS

        public const bool ENABLE_ROPE_MODULE = true;
        public const bool ENABLE_FORCE_MODULE = true;
        public const bool ENABLE_TRANSPORT_MODULE = true;


        public static bool EnableXBoxControllerInput;
        public static bool FreeRangeMode;
        public static String ConfigFilePath;
        public static float MaxHookCreationDistance;
        public static float MaxHookedEntityDistance;
        public static float MaxHookedPedDistance;

        public static bool ShowHookRopeProp = true;
        public static bool HookPedsAtBonesCoords = true;

        public static bool ContinuousForce;
        public static float ForceIncrementValue;
        public static float BalloonUpForceIncrement;
        public static float MaxBalloonHookAltitude;
        public const float ForceScaleFactor = 1.3f;
        public const float TransportVehicleDownForce = 42.0f;

        //public static int MAX_CHAIN_SEGMENTS;
        //public static bool SHOW_CHAIN_JOINT_PROP = true;
        //public const float MIN_CHAIN_SEGMENT_LENGTH = 0.1F;
        //public static float CHAIN_JOINT_OFFSET = 0.3f;    
        //public const float CHAIN_JOINT_PROP_MASS = 10.0f;

        public const int MAX_HOOK_COUNT = 59;

        public const float MAX_HOOKED_PED_SPEED = 1.87f;
        public const int MAX_HOOKED_PEDS = 300;
        public const int MAX_SELECTED_HOOKS = 30;
        public const int INIT_HOOK_LIST_CAPACITY = 100;
        public const int PED_RAGDOLL_DURATION = 7000; // WARNING: Values above 9000 are very likely to crash the game (GTAV v1.0.2545).
        public const char SEPARATOR_CHAR = '+';

        public const float MIN_MIN_ROPE_LENGTH = 0.5f;

        public static float MinRopeLength = MIN_MIN_ROPE_LENGTH;
        public static float MaxRopeLength = 80.0f;

        //public const float MAX_MIN_ROPE_LENGTH = 100f;
        //public const float MIN_MIN_ROPE_LENGTH = MIN_ROPE_LENGTH;

        public static SubtitleQueue SubQueue = new SubtitleQueue();


        public static bool ModActive = false;
        public static bool ModRunning = false;
        public static bool FirstTime = true;
        public static bool NoSubtitlesMode = false;

        public static bool DebugMode = true;
        public static bool TestAction = false;


        public static Model RopeHookPropModel;
        //public static  Model chainJointPropModel;

        public static String DebugInfo = "";
        public static String GlobalSubtitle = "";

        public static List<HookPair> Hooks = new List<HookPair>(INIT_HOOK_LIST_CAPACITY);
        //public static List<ChainGroup> Chains = new List<ChainGroup>(INIT_HOOK_LIST_CAPACITY);
        public static HookPair RopeHook = new HookPair();
        public static HookPair ForceHook = new HookPair();

        public static List<HookPair> SelectedHooks = new List<HookPair>(50);

        public static List<ControlKey> ControlKeys = new List<ControlKey>(30);
        public static List<ControlButton> ControlButtons = new List<ControlButton>(30);

        public static RopeType EntityToEntityHookRopeType;
        public static RopeType PlayerToEntityHookRopeType;
        public static RopeType TransportHooksRopeType;
        //public static RopeType ChainSegmentRopeType;

        public static int HookedPedCount = 0;

        public static float MinTransportPedRopeLength = 2.0f;
        public static float MinTransportRopeLength = 4.0f;
        public static float RopeWindingSpeed = 0.15f;
        public static float ForceMagnitude = 80.0f;
        public static float BalloonUpForce = 35.0f;
        public static bool SolidRopes = false;
        public static bool BalloonHookMode = false;
        public static int CurrentTransportHookFilterIndex = 0;
        public static int CurrentTransportHookModeIndex = 1;
        public static int TransportEntitiesRadius = 35;

        public static bool TestAction1 = false;

        //public static Stopwatch Watch = new Stopwatch();

        public static String GetErrorMessage(Exception exc)
        {
            return (DebugMode ? exc.ToString() : exc.Message);
        }
        public static String GetErrorMessage(Exception exc, String extraDebugInfo)
        {
            return (DebugMode ? (exc.ToString() + "\n" + extraDebugInfo)  : exc.Message);
        }

        public static bool CanUseModFeatures()
        {
            return Game.Player.Exists() && !Game.Player.IsDead &&
                !Game.Player.Character.IsRagdoll && Game.Player.CanControlCharacter &&
                Game.Player.IsAiming;
        }

        public static String GetModVersion()
        {
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

            return ("v" + version.Major + "." + VERSION_MINOR + "." + VERSION_BUILD + "." + version.Revision + VERSION_SUFFIX);
        }

        public static void CheckCurrentModState()
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


        public static void ToggleModActiveProc()
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

        public static void ToggleNoSubtitlesModeProc()
        {
            NoSubtitlesMode = !NoSubtitlesMode;

            UI.Notify("VRope 'No Subtitles' Mode " + (NoSubtitlesMode ? "[Enabled]." : "(Disabled)."));
        }


        public static void ThisIsATestFunction()
        {
            if(Game.Player.IsAlive)
            {
                //RaycastResult rayResult = Util.CameraRaycastForward();
                //Entity targetEntity = null;
                //int fireChildren = 10;
                //bool gasFire = false;

                //if(rayResult.DitHitAnything)
                //{
                //    if (Util.GetEntityPlayerIsAimingAt(ref targetEntity))
                //    {
                //        //Vector3 zAxisDown = new Vector3(0f, 0f, -0.01f);

                //        if (Util.IsNPCPed(targetEntity))
                //        {
                //            Ped ped = (Ped)targetEntity;

                //            Util.MakePedRagdoll(ped, 2000);

                //            Function.Call(Hash.START_ENTITY_FIRE, ped.Handle);
                //        }
                //        else
                //        {
                //            Vector3 pos = rayResult.HitCoords;

                //            Function.Call(Hash.START_SCRIPT_FIRE, pos.X, pos.Y, pos.Z, fireChildren, gasFire);
                //        }

                //    }
                //    else
                //    {
                //        Vector3 pos = rayResult.HitCoords;

                //        Function.Call(Hash.START_SCRIPT_FIRE, pos.X, pos.Y, pos.Z, fireChildren, gasFire);
                //    }
                //}

                Ped[] peds = World.GetNearbyPeds(Game.Player.Character.Position, 15);

                for(int i=0; i<peds.Length; i++)
                {
                    Function.Call(Hash.START_ENTITY_FIRE, peds[i].Handle);
                }
            }
        }
    }
}
