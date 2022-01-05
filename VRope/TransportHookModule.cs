using GTA;
using GTA.Math;
using GTA.Native;
using System;

using static VRope.Core;

/*
 * 
 * created by jeffsturm4nn
 * 
 */

namespace VRope
{
    public static class TransportHookModule
    {

        public enum TransportHookMode
        {
            SINGLE = 0,

            DOUBLE_LEFT_RIGHT,
            DOUBLE_FRONT_BACK,

            CROSS
        }

        public static readonly TransportHookMode[] AllTransportHookModes = (TransportHookMode[])Enum.GetValues(typeof(TransportHookMode));

        public static void CycleTransportHookFilterProc(bool nextFilter = true)
        {
            if (nextFilter)
            {
                if ((CurrentTransportHookFilterIndex + 1) < HookFilter.DefaultFilters.Count)
                    CurrentTransportHookFilterIndex++;
                else
                    CurrentTransportHookFilterIndex = 0;
            }
            else
            {
                if ((CurrentTransportHookFilterIndex - 1) >= 0)
                    CurrentTransportHookFilterIndex--;
                else
                    CurrentTransportHookFilterIndex = HookFilter.DefaultFilters.Count - 1;
            }

            SubQueue.AddSubtitle(1239, "VRope Transport Hook Filter: " + HookFilter.DefaultFilters[CurrentTransportHookFilterIndex].label, 55);

        }

        public static void CycleTransportHookModeProc(bool nextMode = true)
        {
            if (nextMode)
            {
                if ((CurrentTransportHookModeIndex + 1) < AllTransportHookModes.Length)
                    CurrentTransportHookModeIndex++;
                else
                    CurrentTransportHookModeIndex = 0;
            }
            else
            {
                if ((CurrentTransportHookModeIndex - 1) >= 0)
                    CurrentTransportHookModeIndex--;
                else
                    CurrentTransportHookModeIndex = HookFilter.DefaultFilters.Count - 1;
            }

            SubQueue.AddSubtitle(1239, "VRope Transport Hook Mode: " + HookFilter.DefaultFilters[CurrentTransportHookModeIndex].label, 55);

        }

        public static bool CheckTransportHookPermission(Entity entity)
        {
            if (entity == null || !entity.Exists())
                return false;

            if (entity == Util.GetVehiclePlayerIsIn() ||

                Util.IsPlayer(entity) ||

                !HookFilter.DefaultFilters[CurrentTransportHookFilterIndex].matches(entity) ||

                (Util.IsPed(entity) && ((Ped)entity).IsSittingInVehicle()) ||

                PlayerAttachments.Contains(entity))
            {
                return false;
            }

            return true;
        }

        //public static void CreateLandVehicleTransportHooks()
        //{

        //}

        private static void CreateTransportHook(HookPair transHook)
        {
            RopeModule.CreateHook(RopeHook);

            PlayerAttachments.Add(transHook.entity2);
        }

        private static void CreateSingleAirTransportHook(HookPair transHook)
        {
            Vehicle playerAirVehicle = Util.GetVehiclePlayerIsIn();

            transHook.hookPoint1 = playerAirVehicle.Position + (-playerAirVehicle.UpVector * 35.0f);

            transHook.hookPoint2 = transHook.entity2.Position + (transHook.entity2.UpVector * 30.0f);
        }

        public static void CreateAirVehicleTransportHooks(bool singleHook = false)
        {
            Vehicle flyingVehicle = Util.GetVehiclePlayerIsIn();

            Entity[] nearbyEntities = World.GetNearbyEntities(flyingVehicle.Position, 32.0f);

            for (int i = 0; i < nearbyEntities.Length; i++)
            {
                Entity entity = nearbyEntities[i];

                if (singleHook && i > 0)
                    break;

                if (!CheckTransportHookPermission(entity))
                    continue;

                HookPair transHook = new HookPair();
                TransportHookMode hookMode = TransportHookMode.SINGLE;

                transHook.ropeType = TransportHooksRopeType;
                transHook.entity1 = Game.Player.Character; 
                transHook.entity2 = entity;

                if (Util.IsVehicle(entity))
                {
                    hookMode = AllTransportHookModes[CurrentTransportHookModeIndex];
                }
                else
                {
                    transHook.hookPoint1 = flyingVehicle.Position + (-flyingVehicle.UpVector * 35.0f);
                    transHook.hookPoint2 = entity.Position;
                }

                switch (hookMode)
                {
                    case TransportHookMode.SINGLE:
                        CreateSingleAirTransportHook(transHook);
                        break;
                }
            }
        }

        public static void AttachTransportHooksProc()
        {
            try
            {
                //UI.Notify("Random -100 to 100: "+ (-100 + (Util.GetGlobalRandom().NextDouble() * 2.0f * 100)));

                if (Game.Player.Character.IsAlive && Game.Player.Character.IsSittingInVehicle())
                {
                    if (Game.Player.Character.IsInFlyingVehicle)
                    {
                        CreateAirVehicleTransportHooks();
                    }

                }
            }
            catch (Exception exc)
            {
                UI.Notify("VRope AttachTransportHooks Error:\n" + GetErrorMessage(exc));
            }
        }
    }
}
