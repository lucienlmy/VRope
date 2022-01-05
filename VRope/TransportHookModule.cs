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
        public static void CycleTransportHookFilterProc(bool nextFilter)
        {
            if (nextFilter)
            {
                if ((CurrentHookFilterIndex + 1) < HookFilter.DefaultFilters.Count)
                    CurrentHookFilterIndex++;
                else
                    CurrentHookFilterIndex = 0;
            }
            else
            {
                if ((CurrentHookFilterIndex - 1) >= 0)
                    CurrentHookFilterIndex--;
                else
                    CurrentHookFilterIndex = HookFilter.DefaultFilters.Count - 1;
            }

            SubQueue.AddSubtitle(1239, "VRope Transport Hook Filter: " + HookFilter.DefaultFilters[CurrentHookFilterIndex].label, 55);

        }


        public static bool CheckTransportHookPermission(Entity entity)
        {
            if (entity == null || !entity.Exists())
                return false;

            if (entity == Util.GetVehiclePlayerIsIn() ||

                Util.IsPlayer(entity) ||

                !HookFilter.DefaultFilters[CurrentHookFilterIndex].matches(entity) ||

                (Util.IsPed(entity) && ((Ped)entity).IsSittingInVehicle()) ||

                PlayerAttachments.Contains(entity))
            {
                return false;
            }

            return true;
        }

        public static void CreateLandVehicleTransportHooks()
        {

        }

        public static void CreateAirVehicleTransportHooks(bool singleHook = false)
        {
            Vehicle flyingVehicle = Util.GetVehiclePlayerIsIn();

            Entity[] nearbyEntities = World.GetNearbyEntities(flyingVehicle.Position, 32.0f);

            for (int i = 0; i < nearbyEntities.Length; i++)
            {
                Entity entity = nearbyEntities[i];

                if (!CheckTransportHookPermission(entity) ||
                    (singleHook && i > 0))
                {
                    continue;
                }

                Vector3 flyingVehicleHookPoint = flyingVehicle.Position + (-flyingVehicle.UpVector * 35.0f);

                //const int maxDistance = 10;
                //flyingVehicleHookPoint.X += Util.GetGlobalRandom().Next(-maxDistance, maxDistance);
                //flyingVehicleHookPoint.Y += Util.GetGlobalRandom().Next(-maxDistance, maxDistance);

                RopeHook.ropeType = TransportHooksRopeType;
                RopeHook.entity1 = Game.Player.Character;

                RopeHook.hookPoint1 = flyingVehicleHookPoint;

                RopeHook.entity2 = entity;
                RopeHook.hookPoint1 = entity.Position + (entity.UpVector * 30.0f);

                RopeModule.CreateHook(RopeHook);

                PlayerAttachments.Add(entity);
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
