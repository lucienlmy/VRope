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
    public static class TransportModule
    {
        private const int SUBTITLE_DURATION = 500; //ms

        private const float TRANSPORTED_ENTITY_UPVECTOR_MULT = 0.7f;

        public enum TransportHookType
        {
            NONE = 0,
            SINGLE,
            MULTIPLE,
            PRECISE,
        }

        public enum TransportHookMode
        {
            CENTER = 0,
            LEFT_RIGHT,
            FRONT_BACK,
            CROSS,
            HEXAGON
        }

        public readonly static Pair<TransportHookMode, String>[] AllTransportHookModes =
        {
            Pair.Make( TransportHookMode.CENTER, "{ Center }"),
            Pair.Make( TransportHookMode.LEFT_RIGHT, "{ Left/Right }"),
            Pair.Make( TransportHookMode.FRONT_BACK, "{ Front/Back }" ),
            //Pair.Make( TransportHookMode.CROSS, "{ Cross }" )
        };

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

                SubQueue.AddSubtitle(8808, "VRope Transport Hook Filter: " + HookFilter.DefaultFilters[CurrentTransportHookFilterIndex].label, SUBTITLE_DURATION);
            }

        }

        public static void CycleTransportHookModeProc(bool nextFilter = true)
        {
            if (Util.IsPlayerAlive() && Util.IsPlayerSittingInFlyingVehicle())
            {
                if (nextFilter)
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

                SubQueue.AddSubtitle(8809, "VRope Transport Hook Mode: " + AllTransportHookModes[CurrentTransportHookModeIndex].second, SUBTITLE_DURATION);
            }

        }



        public static void CreateLandVehicleTransportHooks(TransportHookType hookType)
        {
            try
            {
                if (HookFilter.DefaultFilters[CurrentTransportHookFilterIndex].filterValue.Contains("GTA.Ped"))
                    hookType = TransportHookType.SINGLE;

                List<Entity> transportEntities = GetTransportHookEntities(hookType);
                Entity playerFlyingVehicle = Util.GetVehiclePlayerIsIn();

                for (int i = 0; i < transportEntities.Count; i++)
                {
                    Entity entity = transportEntities[i];

                    if (!CheckTransportHookPermission(entity))
                        continue;

                    Entity hookEntity1 = (!Util.IsPed(entity) ? Game.Player.Character : playerFlyingVehicle);

                    HookPair transHook = new HookPair();
                    TransportHookMode hookMode = TransportHookMode.CENTER;

                    transHook.isTransportHook = true;
                    transHook.ropeType = TransportHooksRopeType;
                    transHook.entity1 = hookEntity1;
                    transHook.entity2 = entity;

                    if (!Util.IsPed(entity) && hookType == TransportHookType.SINGLE)
                    {
                        hookMode = AllTransportHookModes[CurrentTransportHookModeIndex].first;
                    }
                    else if (hookType == TransportHookType.MULTIPLE)
                    {
                        transHook.hookOffset1 = Util.GetRandom2DPositionAround(Vector3.Zero, 0.31f);
                    }

                    switch (hookMode)
                    {
                        case TransportHookMode.CENTER:
                            CreateTransportHookCenterMode(transHook); break;

                        case TransportHookMode.LEFT_RIGHT:
                            CreateTransportHookLeftRightMode(transHook); break;

                    }
                }
            }
            catch (Exception exc)
            {
                UI.Notify("VRope CreateAirVehicleTransportHooks() Error: " + GetErrorMessage(exc));
            }
        }

        public static void CreateAirVehicleTransportHooks(TransportHookType hookType)
        {
            try
            {
                if (HookFilter.DefaultFilters[CurrentTransportHookFilterIndex].filterValue.Contains("GTA.Ped"))
                    hookType = TransportHookType.SINGLE;

                List<Entity> transportEntities = GetTransportHookEntities(hookType);
                Entity playerFlyingVehicle = Util.GetVehiclePlayerIsIn();

                for (int i = 0; i < transportEntities.Count; i++)
                {
                    Entity entity = transportEntities[i];

                    if (!CheckTransportHookPermission(entity))
                        continue;

                    Entity hookEntity1 = (!Util.IsPed(entity) ? Game.Player.Character : playerFlyingVehicle);

                    HookPair transHook = new HookPair();
                    TransportHookMode hookMode = TransportHookMode.CENTER;

                    transHook.isTransportHook = true;
                    transHook.ropeType = TransportHooksRopeType;
                    transHook.entity1 = hookEntity1;
                    transHook.entity2 = entity;

                    if (!Util.IsPed(entity) && hookType == TransportHookType.SINGLE)
                    {
                        hookMode = AllTransportHookModes[CurrentTransportHookModeIndex].first;
                    }
                    else if (hookType == TransportHookType.MULTIPLE)
                    {
                        transHook.hookOffset1 = Util.GetRandom2DPositionAround(Vector3.Zero, 0.31f);
                    }

                    switch (hookMode)
                    {
                        case TransportHookMode.CENTER:
                            CreateTransportHookCenterMode(transHook); break;
                        case TransportHookMode.LEFT_RIGHT:
                            CreateTransportHookLeftRightMode(transHook); break;
                        case TransportHookMode.FRONT_BACK:
                            CreateTransportHookFrontBackMode(transHook); break;
                        case TransportHookMode.CROSS:
                            CreateTransportHookCrossMode(transHook); break;

                    }
                }
            }
            catch (Exception exc)
            {
                UI.Notify("VRope CreateAirVehicleTransportHooks() Error: " + GetErrorMessage(exc));
            }
        }

        public static void AttachTransportHooksProc(TransportHookType hookType)
        {
            try
            {
                if (Game.Player.Character.IsAlive && Game.Player.Character.IsSittingInVehicle())
                {
                    if (Game.Player.Character.IsInFlyingVehicle)
                    {
                        CreateAirVehicleTransportHooks(hookType);
                    }
                }
            }
            catch (Exception exc)
            {
                UI.Notify("VRope AttachTransportHooks() Error:\n" + GetErrorMessage(exc));
            }
        }



        private static bool CheckTransportHookPermission(Entity entity)
        {
            //if (DebugMode)
            //{
            //    UI.Notify(
            //            "E.Null/!Exist:" + (entity == null || !entity.Exists()).ToString() +
            //            "\nE.PlayerVeh:" + (entity == Util.GetVehiclePlayerIsIn()).ToString() +
            //            "\nE.Player:" + Util.IsPlayer(entity).ToString() +
            //            "\nE.matchesFilter:" + HookFilter.DefaultFilters[CurrentTransportHookFilterIndex].matches(entity).ToString() +
            //            "\nE.PedInVehicle:" + (Util.IsPed(entity) && ((Ped)entity).IsSittingInVehicle()).ToString() +
            //            "\nE.hookedToPlayer:" + RopeModule.IsEntityHookedToPlayer(entity).ToString() +
            //            "\nE.hookedToPlayerVeh:" + RopeModule.IsEntityHookedToPlayersVehicle(entity).ToString()
            //            ); 
            //}

            if (entity == null || !entity.Exists())
                return false;

            if (entity == Util.GetVehiclePlayerIsIn() ||

                Util.IsPlayer(entity) ||

                !HookFilter.DefaultFilters[CurrentTransportHookFilterIndex].matches(entity) ||

                Util.IsPed(entity) &&
                    (
                        HookedPedCount >= MAX_HOOKED_PEDS ||
                        ((Ped)entity).IsDead ||
                        ((Ped)entity).IsSittingInVehicle()
                    ) ||

                RopeModule.IsEntityHookedToPlayer(entity) ||

                RopeModule.IsEntityHookedToPlayersVehicle(entity))
            {
                return false;
            }

            return true;
        }


        private static List<Entity> GetTransportHookEntities(TransportHookType hookType)
        {
            Vehicle playerAirVehicle = Util.GetVehiclePlayerIsIn();

            List<Entity> nearbyEntities = new List<Entity>(50);

            if (hookType == TransportHookType.MULTIPLE)
            {
                nearbyEntities.AddRange(World.GetNearbyEntities(playerAirVehicle.Position, TransportEntitiesRadius));
            }
            else if (hookType == TransportHookType.SINGLE)
            {
                nearbyEntities.Add(GetNearestTransportEntity(playerAirVehicle.Position, TransportEntitiesRadius));
                //nearbyEntities.AddRange(World.GetNearbyEntities(playerAirVehicle.Position, TransportEntitiesRadius));
            }
            //else if (hookType == TransportHookType.PRECISE)
            //{
            //    RaycastResult rayResult = World.RaycastCapsule(playerAirVehicle.Position, (-playerAirVehicle.UpVector),
            //                                (TransportEntitiesRadius * 2f), 3f, IntersectOptions.Everything);

            //    if (rayResult.DitHitEntity)
            //    {
            //        nearbyEntities.Add(rayResult.HitEntity);
            //    }
            //}

            return nearbyEntities;
        }

        private static Entity GetNearestTransportEntity(Vector3 position, float radius)
        {
            float shortestDistance = float.MaxValue;
            Entity nearestEntity = null;

            Entity[] nearbyEntities = World.GetNearbyEntities(position, radius);

            if (nearbyEntities != null && nearbyEntities.Length > 0)
            {
                for (int i = 0; i < nearbyEntities.Length; i++)
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

            //if (DebugMode)
            //    UI.Notify("nearestEntity.isNull = " + (nearestEntity == null).ToString());

            return nearestEntity;

        }


        private static void CreateTransportHookCenterMode(HookPair transHook, bool copyHook = true)
        {
            float minRopeLength = (!Util.IsPed(transHook.entity2) ? MinTransportRopeLength : MinTransportPedRopeLength);

            Vector3 entityDimensions = transHook.entity2.Model.GetDimensions();

            if (!Util.IsPed(transHook.entity2))
            {
                transHook.hookOffset2 += transHook.entity2.UpVector * entityDimensions.Z * 0.5f;
            }

            RopeModule.CreateHook(transHook, copyHook, minRopeLength);
        }


        private static void CreateTransportHookLeftRightMode(HookPair transHook)
        {
            Vehicle playerVehicle = Util.GetVehiclePlayerIsIn();
            Vector3 entityDimensions = transHook.entity2.Model.GetDimensions();

            HookPair hook1 = new HookPair(transHook);
            HookPair hook2 = new HookPair(transHook);

            Vector3 playerHookOffset = playerVehicle.RightVector * 1.2f;

            hook1.hookOffset1 = -playerHookOffset;
            hook2.hookOffset1 = playerHookOffset;

            Vector3 raySourceLeft = transHook.entity2.Position +
                                    (-transHook.entity2.RightVector * (entityDimensions.Y / 5.0f));

            Vector3 raySourceRight = transHook.entity2.Position +
                                    (transHook.entity2.RightVector * (entityDimensions.Y / 5.0f));

            RaycastResult rayLeft = World.Raycast(raySourceLeft, transHook.entity2.RightVector, 7.0f, IntersectOptions.Everything, playerVehicle);
            RaycastResult rayRight = World.Raycast(raySourceRight, -transHook.entity2.RightVector, 7.0f, IntersectOptions.Everything, playerVehicle);

            Vector3 heightOffset = (transHook.entity2.UpVector * entityDimensions.Z * 0.5f);

            if (rayLeft.DitHitEntity && rayLeft.HitEntity == transHook.entity2 &&
               rayRight.DitHitEntity && rayRight.HitEntity == transHook.entity2)
            {
                hook1.hookOffset2 = rayLeft.HitCoords - transHook.entity2.Position + heightOffset;
                hook2.hookOffset2 = rayRight.HitCoords - transHook.entity2.Position + heightOffset;
            }
            else
            {
                if (DebugMode)
                    UI.Notify("Failed L/R Raycast Hook");

                hook1.hookOffset2 = (-transHook.entity2.RightVector * (entityDimensions.Y / 5.0f)) + heightOffset;
                hook2.hookOffset2 = (transHook.entity2.RightVector * (entityDimensions.Y / 5.0f)) + heightOffset;
            }

            float rope1Length = (hook1.entity1.Position + hook1.hookOffset1).DistanceTo(hook1.entity2.Position + hook1.hookOffset2);
            float rope2Length = (hook2.entity1.Position + hook1.hookOffset1).DistanceTo(hook2.entity2.Position + hook1.hookOffset2);

            float greatestRopeLength = Math.Max(rope1Length, rope2Length);


            RopeModule.CreateHook(hook1, false, MinTransportRopeLength, greatestRopeLength);
            RopeModule.CreateHook(hook2, false, MinTransportRopeLength, greatestRopeLength);
        }

        private static void CreateTransportHookFrontBackMode(HookPair transHook)
        {
            Vehicle playerVehicle = Util.GetVehiclePlayerIsIn();
            Vector3 entityDimensions = transHook.entity2.Model.GetDimensions();

            HookPair hook1 = new HookPair(transHook);
            HookPair hook2 = new HookPair(transHook);

            Vector3 playerHookOffset = playerVehicle.ForwardVector * 1.9f;

            hook1.hookOffset1 = -playerHookOffset;
            hook2.hookOffset1 = playerHookOffset;

            
            Vector3 hookOffsetFront = (-transHook.entity2.ForwardVector * (entityDimensions.X / 1.9f))
                                   + (transHook.entity2.UpVector * (entityDimensions.Z * 0.6f));

            Vector3 hookOffsetBack = (transHook.entity2.ForwardVector * (entityDimensions.X / 1.9f))
                                  + (transHook.entity2.UpVector * (entityDimensions.Z * 0.6f));

            hook1.hookOffset2 = hookOffsetFront;
            hook2.hookOffset2 = hookOffsetBack;

            float rope1Length = (hook1.entity1.Position + hook1.hookOffset1).DistanceTo(hook1.entity2.Position + hook1.hookOffset2);
            float rope2Length = (hook2.entity1.Position + hook1.hookOffset1).DistanceTo(hook2.entity2.Position + hook1.hookOffset2);

            float greatestRopeLength = Math.Max(rope1Length, rope2Length);


            RopeModule.CreateHook(hook1, false, MinTransportRopeLength, greatestRopeLength);
            RopeModule.CreateHook(hook2, false, MinTransportRopeLength, greatestRopeLength);
        }

        private static void CreateTransportHookCrossMode(HookPair transHook)
        {
            CreateTransportHookFrontBackMode(transHook);

            CreateTransportHookLeftRightMode(transHook);
        }
    }
}
