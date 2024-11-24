﻿using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Collections.Generic;

/*
 * 
 * created by jeffsturm4nn
 * 
 */

namespace VRope
{
    public static class Util
    {
        private static Random globalRandom = new Random(Guid.NewGuid().GetHashCode());

        public static Random GetGlobalRandom()
        {
            return globalRandom; 
        }

        public static Vector3 GetRandom2DPositionAround(Vector3 center, float maxDistance)
        {
            float randX = (float)( -maxDistance + (GetGlobalRandom().NextDouble() * 2.0f * maxDistance) );
            float randY = (float)( -maxDistance + (GetGlobalRandom().NextDouble() * 2.0f * maxDistance) );

            center.X += randX;
            center.Y += randY;

            return center;
        }


        private static Bone[] PedBoneArray = (Bone[])Enum.GetValues(typeof(Bone));      


        public static Vector3 Truncate(Vector3 v)
        {
            return new Vector3((float)Math.Truncate(v.X), (float)Math.Truncate(v.Y), (float)Math.Truncate(v.Z));
        }

        public static bool IsStatic(Entity e)
        {
            return (e != null && Function.Call<bool>(Hash.IS_ENTITY_STATIC, e.Handle));
        }

        public static bool IsPed(Entity e)
        {
            return (e != null && e.GetType().ToString() == "GTA.Ped");
        }

        public static bool IsPed(int index)
        {
            return Function.Call<bool>(Hash.IS_ENTITY_A_PED, index);
        }

        public static bool IsNPCPed(Entity e)
        {
            return (IsPed(e) && e != Game.Player.Character);
        }

        public static Vector3 GetEntityDimensions(Entity entity)
        {
            if (entity == null || !entity.Exists())
                return Vector3.Zero;

            Vector3 minDimension = new Vector3();
            Vector3 maxDimension = new Vector3();

            entity.Model.GetDimensions(out minDimension, out maxDimension);

            return (maxDimension - minDimension);
        }

        public static bool IsPedSittingInAVehicle(int index)
        {
            return Function.Call<bool>(Hash.IS_PED_SITTING_IN_ANY_VEHICLE, index);
        }

        public static Vehicle GetVehiclePedIsIn(int index)
        {
            return new Vehicle(Function.Call<int>(Hash.GET_VEHICLE_PED_IS_IN, index, 0));
        }

        public static Vehicle GetVehiclePedIsIn(Ped ped, bool includeLastVehicle = false)
        {
            return Function.Call<Vehicle>(Hash.GET_VEHICLE_PED_IS_IN, ped, includeLastVehicle);
        }

        public static Vehicle GetVehiclePlayerIsIn(bool includeLastVehicle = false)
        {
            return GetVehiclePedIsIn(Game.Player.Character, includeLastVehicle);
        }

        public static bool IsPlayerInVehicle(Entity vehicle, bool includeLastVehicle = false)
        {
            return (GetVehiclePlayerIsIn(includeLastVehicle) == vehicle);
        }

        public static bool IsPlayerInVehicle()
        {
            return Game.Player.Character.IsInVehicle();
        }

        public static bool IsVehicle(Entity e)
        {
            return (e != null && e.GetType().ToString() == "GTA.Vehicle");
        }

        public static bool IsVehicle(int index)
        {
            return Function.Call<bool>(Hash.IS_ENTITY_A_VEHICLE, index);
        }

        public static bool IsPlayer(Entity e)
        {
            return (e != null && e == Game.Player.Character);
        }

        public static bool IsPlayerAlive()
        {
            return Game.Player.IsAlive;
        }

        public static bool IsPlayerSittingInVehicle()
        {
            return (Game.Player.Character.IsSittingInVehicle());
        }

        public static bool IsPlayerSittingInFlyingVehicle()
        {
            return (Game.Player.Character.IsSittingInVehicle() && Game.Player.Character.IsInFlyingVehicle);
        }

        public static bool IsPlayer(int index)
        {
            return (Function.Call<int>(Hash.GET_PLAYER_INDEX) == index);
        }

        public static bool IsProp(Entity e)
        {
            return (e != null && e.GetType().ToString() == "GTA.Prop");
        }

        public static bool IsProp(int index)
        {
            return Function.Call<bool>(Hash.IS_ENTITY_AN_OBJECT, index);
        }

        public static bool IsEntity(int index)
        {
            return Function.Call<bool>(Hash.IS_AN_ENTITY, index);
        }


        public static bool IsValid(Entity e)
        {
            return (e != null && e.Exists());
        }

        public static void DeleteEntity(Entity e)
        {
            if (e != null)
            {
                e.Delete();
                e = null;
            }
        }

        public static void DeleteEntities<E>(List<E> entities, bool removeFromList = true) where E: Entity
        {
            if(entities != null)
            {
                for(int i=(entities.Count-1); i>=0; i--)
                {
                    if (entities[i] != null)
                    {
                        entities[i].Delete();
                        entities[i] = null;

                        if(removeFromList)
                            entities.RemoveAt(i);
                    }
                }
            }
        }

        public static void DeleteEntities<E>(E[] entities) where E : Entity
        {
            if (entities != null)
            {
                for (int i = (entities.Length - 1); i >= 0; i--)
                {
                    if (entities[i] != null)
                    {
                        entities[i].Delete();
                        entities[i] = null;
                    }
                }
            }
        }

        //public static bool IsEntityAfterAnother(Entity entity1, Entity entity2)
        //{
        //    Vector3 entity1Dimensions = entity1.Model.GetDimensions();
        //    Vector3 positionEntity1 = (entity1.ForwardVector) * entity1.Position; 
        //}

        public static Vector3 GetNearestBonePosition(Ped ped, Vector3 hitPosition)
        {
            if (ped == null)
                return new Vector3(0, 0, 0);

            float shortestBoneDistance = float.MaxValue;
            Vector3 nearestBonePosition = new Vector3(0, 0, 0);

            for (int i = 0; i < PedBoneArray.Length; i++)
            {
                Vector3 bonePosition = ped.GetBoneCoord(PedBoneArray[i]);
                float boneDistance = bonePosition.DistanceTo(hitPosition);
                
                if (boneDistance < shortestBoneDistance)
                {
                    shortestBoneDistance = boneDistance;
                    nearestBonePosition = bonePosition;
                }
            }

            return nearestBonePosition;
        }

        public static int GetNearestBoneIndex(Ped ped, Vector3 hitPosition)
        {
            if (ped == null)
                return 0;

            float shortestBoneDistance = float.MaxValue;
            int nearestBoneIndex = 0;

            for (int i = 0; i < PedBoneArray.Length; i++)
            {
                Vector3 bonePosition = ped.GetBoneCoord(PedBoneArray[i]);
                float boneDistance = bonePosition.DistanceTo(hitPosition);
                Bone currentBone = PedBoneArray[i];

                if (boneDistance < shortestBoneDistance)
                {
                    shortestBoneDistance = boneDistance;
                    nearestBoneIndex = ped.GetBoneIndex(currentBone);
                }
            }

            return nearestBoneIndex;
        }

        public static Vector3 CalculateDirectionVector3d(Vector3 rotation)
        {
            float Z = rotation.Z,
                X = rotation.X;
            float ZRads = Z * 0.0174532924F, XRads = X * 0.0174532924F;

            float AbsX = (float)Math.Abs(Math.Cos(XRads));

            Vector3 directionVector = new Vector3((float)-Math.Sin(ZRads) * AbsX, (float)Math.Cos(ZRads) * AbsX, (float)Math.Sin(XRads));

            return directionVector;
        }

        public static RaycastResult CameraRaycastForward()
        {
            //Vector3 cameraRotation = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_ROT, 0);
            //Vector3 cameraPosition = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_COORD);

            //Vector3 directionVec = CalculateDirectionVector3d(cameraRotation);

            //Vector3 multiplied = new Vector3(directionVec.X * 100.0f, directionVec.Y * 100.0f, directionVec.Z * 100.0f);

            //RaycastResult rayResult = World.RaycastCapsule(cameraPosition, cameraPosition + (multiplied * 1000f), 0.5f, IntersectOptions.Everything);
            
            RaycastResult rayResult = World.GetCrosshairCoordinates();

            return rayResult;
        }

        public static Vector3 RotateVectorOnYAxis(Vector3 vector, Vector3 rotation)
        {
            if (vector == null)
                return Vector3.Zero;

            Vector3 radianRotation = rotation * 0.0174532924F;

            float X = ((float)(vector.X * Math.Cos(radianRotation.X) + vector.Z * Math.Sin(radianRotation.Z)));
            float Z = ((float)((-vector.X) * Math.Sin(radianRotation.X) + vector.Z * Math.Cos(radianRotation.Z)));

            return new Vector3(X, vector.Y, Z);
        }

        public static void MakePedRagdoll(Ped ped, int duration)
        {
            if (ped == null)
                return;

            Function.Call(Hash.SET_PED_CAN_RAGDOLL, ped.Handle, true);
            Function.Call(Hash.SET_PED_TO_RAGDOLL, ped.Handle, duration, duration, 0, 0, 0, 0);
        }

        public unsafe static bool GetEntityPlayerIsAimingAt(ref Entity entity)
        {
            int playerIndex = Function.Call<int>(Hash.GET_PLAYER_INDEX);

            int entityIndex = 0;

            if(Function.Call<bool>(Hash.GET_ENTITY_PLAYER_IS_FREE_AIMING_AT, playerIndex, &entityIndex))
            {
                if (IsPed(entityIndex))
                {
                    if(IsPedSittingInAVehicle(entityIndex))
                    {
                        entity = GetVehiclePedIsIn(entityIndex);
                    }
                    else
                    {
                        entity = new Ped(entityIndex);
                    }
                }
                else if (IsVehicle(entityIndex))
                {
                    entity = new Vehicle(entityIndex);
                }
                else if(IsProp(entityIndex))
                {
                    entity = new Prop(entityIndex);
                }

                return true;
            }

            return false;
        }
    
        public static Vector3 AsVector(float x = 0f, float y = 0f, float z = 0f)
        {
            return new Vector3(x, y, z);
        }
    
        public static Entity GetNearestEntity(Vector3 position, float radius)
        {
            float shortestDistance = 0.0f;
            Entity nearestEntity = null;
            
            Entity[] nearbyEntities = World.GetNearbyEntities(position, radius);

            if(nearbyEntities != null && nearbyEntities.Length > 0)
            {
                nearestEntity = nearbyEntities[0];
                shortestDistance = nearestEntity.Position.DistanceTo(position);

                for(int i=1; i<nearbyEntities.Length; i++)
                {
                    Entity currentEntity = nearbyEntities[i];

                    if (currentEntity == Game.Player.Character)
                        continue;
                    
                    float currentDistance = currentEntity.Position.DistanceTo(position);

                    if (currentDistance < shortestDistance)
                    {
                        shortestDistance = currentDistance;
                        nearestEntity = currentEntity;
                    }
                }
            }

            return nearestEntity;
        }

        public static bool isPlane(Entity e)
        {
            return (e != null && e.Model.IsPlane);
        }

        public static bool isHeli(Entity e)
        {
            return (e != null && e.Model.IsHelicopter);
        }
    }
}
