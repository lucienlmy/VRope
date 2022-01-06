using GTA;
using GTA.Math;
using System;
using System.Collections.Generic;
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

        private static readonly String[] AllTransportHookModeLabels = {
            "{ Single (Center) }",
            "{ Double (Left/Right) }",
            "{ Double (Front/Back) }",
            "{ Cross (4-ropes) }"
        };

        public static readonly TransportHookMode[] AllTransportHookModes = (TransportHookMode[])Enum.GetValues(typeof(TransportHookMode));


        public static void CycleTransportHookFilterProc(bool nextFilter = true)
        {
            if (Util.IsPlayerAlive() && Util.IsPlayerSittingInFlyingVehicle())
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

                SubQueue.AddSubtitle(1239, "VRope Transport Hook Filter: " + HookFilter.DefaultFilters[CurrentTransportHookFilterIndex].label, 59); 
            }

        }

        public static void CycleTransportHookModeProc(bool nextMode = true)
        {
            if (Util.IsPlayerAlive() && Util.IsPlayerSittingInFlyingVehicle())
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

                SubQueue.AddSubtitle(1239, "VRope Transport Hook Mode: " + AllTransportHookModeLabels[CurrentTransportHookModeIndex], 59); 
            }

        }


        public static bool CheckTransportHookPermission(Entity entity)
        {
            if (entity == null || !entity.Exists())
                return false;

            if (entity == Util.GetVehiclePlayerIsIn() ||

                Util.IsPlayer(entity) ||

                !HookFilter.DefaultFilters[CurrentTransportHookFilterIndex].matches(entity) ||

                (Util.IsPed(entity) && ((Ped)entity).IsSittingInVehicle()) ||

                RopeModule.IsEntityHookedToThePlayer(entity))
            {
                return false;
            }

            return true;
        }

        public static void CreateAirVehicleTransportHooks(bool singleHook = false)
        {

            List<Entity> transportEntities = GetTransportHookEntities(singleHook);


            for (int i = 0; i < transportEntities.Count; i++)
            {
                Entity entity = transportEntities[i];

                if (!CheckTransportHookPermission(entity))
                    continue;

                HookPair transHook = new HookPair();
                TransportHookMode hookMode = TransportHookMode.SINGLE;

                transHook.isTransportHook = true;
                transHook.ropeType = TransportHooksRopeType;
                transHook.entity1 = Game.Player.Character;
                transHook.entity2 = entity;

                if (Util.IsVehicle(entity))
                {
                    hookMode = AllTransportHookModes[CurrentTransportHookModeIndex];
                }

                switch (hookMode)
                {
                    case TransportHookMode.SINGLE:
                        CreateTransportHookSingleMode(transHook);
                        break;
                }
            }
        }

        public static void AttachTransportHooksProc(bool singleHook)
        {
            try
            {
                if (Game.Player.Character.IsAlive && Game.Player.Character.IsSittingInVehicle())
                {
                    if (Game.Player.Character.IsInFlyingVehicle)
                    {
                        CreateAirVehicleTransportHooks(singleHook);
                    }

                }
            }
            catch (Exception exc)
            {
                UI.Notify("VRope AttachTransportHooks Error:\n" + GetErrorMessage(exc));
            }
        }



        private static Entity GetNearestTransportEntity(Vector3 position, float radius)
        {
            float shortestDistance = 0.0f;
            Entity nearestEntity = null;

            Entity[] nearbyEntities = World.GetNearbyEntities(position, radius);

            if (nearbyEntities != null && nearbyEntities.Length > 0)
            {
                nearestEntity = nearbyEntities[0];
                shortestDistance = nearestEntity.Position.DistanceTo(position);

                for (int i = 1; i < nearbyEntities.Length; i++)
                {
                    Entity entity = nearbyEntities[i];

                    if (!CheckTransportHookPermission(entity))
                        continue;

                    float distance = entity.Position.DistanceTo(position);

                    if (distance < shortestDistance)
                    {
                        shortestDistance = distance;
                        nearestEntity = entity;
                    }
                }
            }

            return nearestEntity;

        }

        private static void CreateTransportHookSingleMode(HookPair transHook)
        {
            Vector3 entity2Offset = Vector3.Zero;

            if (!Util.IsPed(transHook.entity2))
            {
                entity2Offset = (transHook.entity2.UpVector.Normalized * TRANSPORTED_ENTITY_UPVECTOR_MULT);
            }

            transHook.hookPoint1 = Game.Player.Character.Position;// + (-playerAirVehicle.UpVector * 1.05f);
            transHook.CalculateOffset1();

            transHook.hookPoint2 = transHook.entity2.Position + entity2Offset;
            transHook.CalculateOffset2();

            RopeModule.CreateHook(transHook);
        }

        private static List<Entity> GetTransportHookEntities(bool singleHook)
        {
            Vehicle playerAirVehicle = Util.GetVehiclePlayerIsIn();

            List<Entity> nearbyEntities = new List<Entity>(50);


            if (!singleHook)
            {
                nearbyEntities.AddRange(World.GetNearbyEntities(playerAirVehicle.Position, TransportEntitiesRadius));
            }
            else
            {
                nearbyEntities.Add(GetNearestTransportEntity(playerAirVehicle.Position, TransportEntitiesRadius));
            }

            return nearbyEntities;
        }

    }
}
