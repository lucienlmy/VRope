using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using GTA;
using GTA.Math;
using GTA.Native;

namespace VRope
{
    static class Util
    {
        private static Bone[] PedBoneArray = (Bone[])Enum.GetValues(typeof(Bone));

        [DllImport("User32.dll")]
        public static extern short GetAsyncKeyState(Keys vKey);

        public static bool IsKeyPressed(Keys key)
        {
            return ((GetAsyncKeyState(key) & 0x8000) == 0x8000);
        }

        public static bool IsPed(Entity e)
        {
            return (e != null && e.GetType().ToString() == "GTA.Ped");
        }

        public static bool IsVehicle(Entity e)
        {
            return (e != null && e.GetType().ToString() == "GTA.Vehicle");
        }

        public static bool IsPlayer(Entity e)
        {
            return (e != null && e == Game.Player.Character);
        }

        public static bool IsProp(Entity e)
        {
            return (e != null && e.GetType().ToString() == "GTA.Prop");
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

            //Vector3 multiplied = new Vector3(directionVec.X * 1000.0f, directionVec.Y * 1000.0f, directionVec.Z * 1000.0f);

            //RaycastResult rayResult = World.Raycast(cameraPosition, cameraPosition + (multiplied * 5000f), IntersectOptions.Everything);

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

    }
}
