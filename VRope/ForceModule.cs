
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
    public static class ForceModule
    {
        public static void ApplyForceAtAimedProc(bool invertForce = false)
        {
            if (CanUseModFeatures())
            {
                RaycastResult rayResult = Util.CameraRaycastForward();
                Entity targetEntity = null;

                if (Util.GetEntityPlayerIsAimingAt(ref targetEntity) && targetEntity != null)
                {
                    Vector3 cameraRotation = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_ROT, 0);
                    Vector3 forceDirection = Util.CalculateDirectionVector3d(cameraRotation);

                    Vector3 entity1Offset = (rayResult.HitCoords != Vector3.Zero ? (rayResult.HitCoords - targetEntity.Position) : Vector3.Zero);

                    if (Util.IsPed(targetEntity))
                    {
                        Util.MakePedRagdoll((Ped)targetEntity, 4000);
                        entity1Offset = Vector3.Zero;
                    }

                    //Vector3 entity1ForcePosition = targetEntity.Position + entity1Offset;

                    if (invertForce)
                        forceDirection = -forceDirection;


                    float scaleFactor = (ContinuousForce ? 1f : 1.3f);

                    Function.Call<bool>(Hash.NETWORK_REQUEST_CONTROL_OF_ENTITY, targetEntity.Handle);

                    targetEntity.ApplyForce((forceDirection * ForceMagnitude * scaleFactor));
                }
            }
        }

        public static void ApplyForceObjectPairProc()
        {
            try
            {
                if (CanUseModFeatures())
                {
                    Entity targetEntity = null;
                    RaycastResult rayResult = Util.CameraRaycastForward();
                    Entity playerEntity = Game.Player.Character;

                    if (SelectedHooks.Count > 0)
                    {
                        bool hasTargetEntity = Util.GetEntityPlayerIsAimingAt(ref targetEntity);

                        foreach (var hook in SelectedHooks)
                        {
                            if (hook.entity1 == null || hook.entity1 == playerEntity ||
                                hook.entity1 == targetEntity || targetEntity == playerEntity)
                                continue;

                            hook.entity2 = targetEntity;
                            hook.hookPoint2 = rayResult.HitCoords;
                            hook.hookOffset2 = Vector3.Zero;

                            Vector3 entity1HookPosition = hook.entity1.Position + hook.hookOffset1;
                            Vector3 entity2HookPosition = Vector3.Zero;

                            if (hasTargetEntity && targetEntity != null)
                            {
                                hook.hookOffset2 = (hook.hookPoint2 != Vector3.Zero ? (hook.hookPoint2 - hook.entity2.Position) : Vector3.Zero);
                                entity2HookPosition = hook.entity2.Position + hook.hookOffset2;
                            }
                            else if (rayResult.DitHitAnything)
                            {
                                entity2HookPosition = hook.hookPoint2;
                            }
                            else continue;

                            ApplyForce(hook, entity1HookPosition, entity2HookPosition);
                        }

                        SelectedHooks.Clear();
                        return;
                    }

                    if (Util.GetEntityPlayerIsAimingAt(ref targetEntity))
                    {
                        if (ForceHook.entity1 == null)
                        {
                            if (ForceHook.entity2 != null)
                                ForceHook.entity2 = null;

                            ForceHook.entity1 = targetEntity;//rayResult.HitEntity;
                            ForceHook.hookPoint1 = rayResult.HitCoords;//targetEntity.Position;
                            ForceHook.hookOffset1 = (ForceHook.hookPoint1 != Vector3.Zero ? (ForceHook.hookPoint1 - ForceHook.entity1.Position) : Vector3.Zero);
                        }
                        else if (ForceHook.entity2 == null)
                        {
                            ForceHook.entity2 = targetEntity;//rayResult.HitEntity;
                            ForceHook.hookPoint2 = rayResult.HitCoords;//targetEntity.Position;
                            ForceHook.hookOffset2 = (ForceHook.hookPoint2 != Vector3.Zero ? (ForceHook.hookPoint2 - ForceHook.entity2.Position) : Vector3.Zero);

                            ForceHook.isEntity2AMapPosition = false;

                            if (ForceHook.entity2 == ForceHook.entity1 ||
                                ForceHook.entity2 == playerEntity ||
                                ForceHook.entity1 == playerEntity)
                            {
                                ForceHook.entity1 = null;
                                ForceHook.entity2 = null;
                            }
                        }

                        if (ForceHook.entity1 != null && ForceHook.entity2 != null)
                        {
                            Vector3 entity2HookPosition = ForceHook.entity2.Position + ForceHook.hookOffset2;
                            Vector3 entity1HookPosition = ForceHook.entity1.Position + ForceHook.hookOffset1;

                            ApplyForce(entity1HookPosition, entity2HookPosition);

                            ForceHook.entity1 = null;
                            ForceHook.entity2 = null;
                        }
                    }
                    else if (rayResult.DitHitAnything)
                    {
                        if ((ForceHook.entity1 != null && ForceHook.entity2 == null) &&
                            (FreeRangeMode || ForceHook.entity1.Position.DistanceTo(rayResult.HitCoords) < MaxHookCreationDistance))
                        {
                            ForceHook.hookPoint2 = rayResult.HitCoords;
                            ForceHook.hookOffset2 = Vector3.Zero;
                            ForceHook.isEntity2AMapPosition = true;

                            Vector3 entity1HookPosition = ForceHook.entity1.Position + ForceHook.hookOffset1;

                            ApplyForce(entity1HookPosition, ForceHook.hookPoint2);
                        }

                        ForceHook.entity1 = null;
                        ForceHook.entity2 = null;
                    }
                    else
                    {
                        ForceHook.entity1 = null;
                        ForceHook.entity2 = null;
                    }
                }
            }
            catch (Exception exc)
            {
                UI.Notify("VRope ApplyForceObjectPairProc Error:\n" + GetErrorMessage(exc));
            }
        }

        public static void ApplyForcePlayerProc()
        {
            if (CanUseModFeatures())
            {
                RaycastResult rayResult = Util.CameraRaycastForward();

                if (rayResult.DitHitAnything)
                {
                    ForceHook.entity1 = Game.Player.Character;
                    ForceHook.hookPoint1 = Game.Player.Character.Position;//GetBoneCoord((Bone)57005);
                    ForceHook.hookPoint2 = rayResult.HitCoords;

                    ForceHook.hookOffset1 = (ForceHook.hookPoint1 != Vector3.Zero ? (ForceHook.hookPoint1 - ForceHook.entity1.Position) : Vector3.Zero);

                    Vector3 entity2HookPosition = Vector3.Zero;

                    if (rayResult.DitHitEntity && Util.IsValid(rayResult.HitEntity))
                    {
                        ForceHook.entity2 = rayResult.HitEntity;
                        ForceHook.hookOffset2 = ForceHook.hookPoint2 - ForceHook.entity2.Position;
                        entity2HookPosition = ForceHook.entity2.Position + ForceHook.hookOffset2;
                    }
                    else
                    {
                        ForceHook.hookOffset2 = Vector3.Zero;
                        entity2HookPosition = ForceHook.hookPoint2;
                    }

                    ApplyForce(ForceHook.hookPoint1, entity2HookPosition);

                    ForceHook.entity1 = null;
                    ForceHook.entity2 = null;
                }
            }
        }


        public static void ApplyForce(Vector3 entity1HookPosition, Vector3 entity2HookPosition)
        {
            float scaleFactor = ForceScaleFactor;

            Vector3 distanceVector = entity2HookPosition - entity1HookPosition;
            Vector3 lookAtDirection = distanceVector.Normalized;

            if (Util.IsPed(ForceHook.entity1) && !Util.IsPlayer(ForceHook.entity1))
            {
                scaleFactor *= 2.2f;

                if (!Util.IsPlayer(ForceHook.entity1))
                    Util.MakePedRagdoll((Ped)ForceHook.entity1, PED_RAGDOLL_DURATION);
            }

            ForceHook.entity1.ApplyForce((lookAtDirection * ForceMagnitude * scaleFactor));
        }

        public static void ApplyForce(HookPair hook, Vector3 entity1HookPosition, Vector3 entity2HookPosition)
        {
            if (hook == null || hook.entity1 == null)
                return;

            float scaleFactor = ForceScaleFactor;

            Vector3 distanceVector = entity2HookPosition - entity1HookPosition;
            Vector3 lookAtDirection = distanceVector.Normalized;

            if (Util.IsPed(hook.entity1) && !Util.IsPlayer(hook.entity1))
            {
                scaleFactor *= 2.2f;

                if (!Util.IsPlayer(hook.entity1))
                    Util.MakePedRagdoll((Ped)hook.entity1, PED_RAGDOLL_DURATION);
            }

            hook.entity1.ApplyForce((lookAtDirection * ForceMagnitude * scaleFactor));
        }


        public static void IncrementForceProc(bool negativeIncrement = false, bool halfIncrement = false)
        {
            float increment = ForceIncrementValue;

            if (negativeIncrement)
                increment = -increment;

            if (halfIncrement)
                increment = increment / 2f;

            ForceMagnitude += increment;

            SubQueue.AddSubtitle(14, "VRope Force Value: " + ForceMagnitude.ToString("0.00"), 220);
        }

        public static void IncrementBalloonUpForce(bool negativeIncrement = false, bool halfIncrement = false)
        {
            float increment = BalloonUpForceIncrement;

            if (negativeIncrement)
                increment = -increment;

            if (halfIncrement)
                increment = increment / 2f;

            BalloonUpForce += increment;

            SubQueue.AddSubtitle(333, "VRope Balloon Up Force: " + BalloonUpForce.ToString("0.00"), 220);
        }

        public static void ToggleBalloonHookModeProc()
        {
            BalloonHookMode = !BalloonHookMode;

            SubQueue.AddSubtitle(1230, "VRope Balloon Hook Mode: " + (BalloonHookMode ? "[ON]" : "(OFF)"), 550);
        }
    }
}
