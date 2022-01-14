using GTA;
using GTA.Math;
using GTA.Native;
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
    /*
        Bone1Chest = 24818
        Bone2Chest = 20719
        Bone3LeftArm = 61007
        Bone4RightArm = 24818
    */

    public static class RopeModule
    {
        public static void DeleteLastHookProc()
        {
            if (Hooks.Count > 0)
            {
                int indexLastHook = Hooks.Count - 1;

                DeleteHookByIndex(indexLastHook);
            }
        }

        public static void DeleteFirstHookProc()
        {
            if (Hooks.Count > 0)
            {
                DeleteHookByIndex(0);
            }
        }

        public static void DeleteAllHooks()
        {
            while (Hooks.Count > 0)
            {
                DeleteHookByIndex(Hooks.Count - 1, true);
            }

            //while (chains.Count > 0)
            //{
            //    if (chains.Last() != null)
            //    {
            //        chains.Last().Delete();
            //    }

            //    chains.RemoveAt(chains.Count - 1);
            //}

            //PlayerAttachments.Clear();

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public static void DeleteHookByIndex(int hookIndex, bool removeFromHooks = true)
        {
            if (hookIndex >= 0 && hookIndex < Hooks.Count)
            {
                if (Hooks[hookIndex] != null)
                {
                    Hooks[hookIndex].Delete();
                    Hooks[hookIndex] = null;
                }
                else
                {
                    UI.Notify("DeleteHookByIndex(): Attempted to delete at invalid index (i:" + hookIndex + ").");
                }

                if (removeFromHooks)
                    Hooks.RemoveAt(hookIndex);

                //if (callGC)
                //{
                //    GC.Collect();
                //    GC.WaitForPendingFinalizers();
                //}
            }
        }

        public static void DeleteHook(HookPair hook, bool removeFromHooks = true)
        {
            if (hook != null)
            {
                hook.Delete();
                hook = null;
            }

            else
            {
                UI.Notify("DeleteHookByIndex(): Attempted to delete NULL hook.");
            }

            if (removeFromHooks)
                Hooks.Remove(hook);
        }



        public static HookPair CreateEntityHook(HookPair hook, bool copyHook = true, bool hookAtBonePositions = true,
                                                float minRopeLength = MIN_ROPE_LENGTH, float customRopeLength = 0.0f)
        {
            try
            {
                if (hook.entity1 == null ||
                    (hook.entity2 == null && !hook.isEntity2AMapPosition))
                    return null;

                Vector3 entity1HookPosition = hook.entity1.Position + hook.hookOffset1;
                Vector3 entity2HookPosition = Vector3.Zero;

                if (hook.isEntity2AMapPosition)
                {
                    hook.entity2 = CreateTargetProp(hook.hookPoint2, false, true, ShowHookRopeProp, true, false);
                    entity2HookPosition = hook.entity2.Position;
                }
                else
                {
                    entity2HookPosition = hook.entity2.Position + hook.hookOffset2;
                }


                if (Util.IsPed(hook.entity1) && !Util.IsPlayer(hook.entity1))
                {
                    if (hookAtBonePositions)
                        entity1HookPosition = Util.GetNearestBonePosition((Ped)hook.entity1, entity1HookPosition);

                    Util.MakePedRagdoll((Ped)hook.entity1, PED_RAGDOLL_DURATION);
                }


                if (Util.IsPed(hook.entity2))
                {
                    if (hookAtBonePositions)
                        entity2HookPosition = Util.GetNearestBonePosition((Ped)hook.entity2, entity2HookPosition);

                    Util.MakePedRagdoll((Ped)hook.entity2, PED_RAGDOLL_DURATION);
                }


                float ropeLength = (customRopeLength > 0.0f ? customRopeLength : entity1HookPosition.DistanceTo(entity2HookPosition)); //TRY1

                if (ropeLength < minRopeLength)
                    ropeLength = minRopeLength;

                hook.rope = World.AddRope(hook.ropeType, entity1HookPosition, Vector3.Zero, ropeLength, minRopeLength, false); //ORIGINAL
                hook.rope.ActivatePhysics();

                hook.rope.AttachEntities(hook.entity1, entity1HookPosition, hook.entity2, entity2HookPosition, ropeLength);


                if (Util.IsVehicle(hook.entity1))
                    hook.entity1.ApplyForce(new Vector3(0, 1, 0));

                if (Util.IsVehicle(hook.entity2))
                    hook.entity2.ApplyForce(new Vector3(1, 0, 0));

                hook.isEntity1ABalloon = BalloonHookMode;

                //UI.Notify("Hook Created. E1 Null: " + (hook.entity1 == null ? "true" : "false") + "| E2 Null: " +
                //    (hook.entity2 == null ? "true" : "false") + "Valid: " + hook.IsValid());

                if (copyHook)
                    return new HookPair(hook);
                else
                    return (hook);
            }
            catch (Exception exc)
            {
                UI.Notify("VRope CreateEntityHook Error:\n" + GetErrorMessage(exc));
                return hook;
            }
        }

        public static void CreateHook(HookPair source, bool copyHook = true, float minRopeLength = MIN_ROPE_LENGTH, float customRopeLength = 0.0f)
        {
            if (!CheckHookPermission(source))
                return;

            HookPair resultHook = CreateEntityHook(source, copyHook, HookPedsAtBonesCoords, minRopeLength, customRopeLength);

            if (resultHook != null)
                Hooks.Add(resultHook);
        }


        public static bool CheckHookPermission(HookPair hook)
        {
            if (hook == null || hook.entity1 == null)
                return false;

            if (hook.entity2 == null && !hook.isEntity2AMapPosition)
                return false;

            if (Util.IsPed(hook.entity1) && !Util.IsPlayer(hook.entity1))
            {
                if (((Ped)hook.entity1).IsDead || Util.IsPed(hook.entity2) || IsEntityHooked(hook.entity1))
                    return false;
            }

            if (Util.IsPed(hook.entity2))
            {
                if (((Ped)hook.entity2).IsDead || IsEntityHooked(hook.entity2))
                    return false;
            }

            return true;
        }


        public static Prop CreateTargetProp(Vector3 position, bool isDynamic, bool hasCollision, bool isVisible, bool hasFrozenPosition, bool placeOnGround)
        {
            Prop targetProp = World.CreateProp(RopeHookPropModel, position, isDynamic, placeOnGround);

            targetProp.HasCollision = hasCollision;
            targetProp.IsVisible = isVisible;
            targetProp.FreezePosition = hasFrozenPosition;
            targetProp.IsInvincible = true;
            //targetProp.IsPersistent = true;

            return targetProp;
        }

        public static bool IsEntityHooked(Entity entity)
        {
            if (entity == null || !entity.Exists())
                return false;

            for (int i = 0; i < Hooks.Count; i++)
            {
                if ((Hooks[i].entity1 != null && Hooks[i].entity1.Equals(entity)) ||
                    (Hooks[i].entity2 != null && Hooks[i].entity2.Equals(entity)))
                    return true;
            }

            return false;
        }

        public static bool AreEntitiesHooked(Entity entity1, Entity entity2)
        {
            if (entity1 == null || !entity1.Exists())
                return false;
            else if (entity2 == null || !entity2.Exists())
                return false;

            for (int i = 0; i < Hooks.Count; i++)
            {
                if (Hooks[i].entity1.Equals(entity1) && Hooks[i].entity2.Equals(entity2))
                    return true;
            }

            return false;
        }

        public static bool IsEntityHookedToPlayer(Entity entity)
        {
            return AreEntitiesHooked(Game.Player.Character, entity);
        }

        public static bool IsEntityHookedToPlayersVehicle(Entity entity)
        {
            return AreEntitiesHooked(Util.GetVehiclePlayerIsIn(), entity);
        }


        public static List<int> GetIndexOfHooksThatContains(Entity entity)
        {
            if (entity == null || !entity.Exists())
                return null;

            List<int> indexes = new List<int>();

            for (int i = 0; i < Hooks.Count; i++)
            {
                if ((Hooks[i].entity1 != null && Hooks[i].entity1.Equals(entity)) ||
                    (Hooks[i].entity2 != null && Hooks[i].entity2.Equals(entity)))
                {
                    indexes.Add(i);
                }
            }

            return indexes;
        }

        public static List<Entity> GetEntitiesAttachedTo(Entity entity)
        {
            List<Entity> entities = new List<Entity>(50);

            for (int i = 0; i < Hooks.Count; i++)
            {
                if (Hooks[i].entity1 != null && Hooks[i].entity1 == entity)
                {
                    entities.Add(Hooks[i].entity1);
                }
                else if (Hooks[i].entity2 != null && Hooks[i].entity2 == entity)
                {
                    entities.Add(Hooks[i].entity2);
                }
            }

            return entities;
        }


        public static void SetHookRopeWinding(HookPair hook, bool winding)
        {
            if (hook != null && hook.IsValid())
            {
                hook.rope.Length -= RopeWindingSpeed;

                if (!hook.isWinding && winding)
                {
                    Function.Call(Hash.START_ROPE_WINDING, hook.rope);

                    //if (hook.rope.Length + RopeWindingSpeed > MIN_ROPE_LENGTH)
                    {
                        
                    }
                    //else
                    //{
                    //    hook.rope.Length = MIN_ROPE_LENGTH;
                    //}

                    hook.isWinding = winding;

                    //hook.isUnwinding = false;
                }
                else if (hook.isWinding && !winding)
                {
                    Function.Call(Hash.STOP_ROPE_WINDING, hook.rope);
                    //hook.rope.ResetLength(true);
                    hook.isWinding = false;
                }
            }
        }

        public static void SetHookRopeWindingByIndex(int index, bool winding)
        {
            if (index >= 0 && index < Hooks.Count)
            {
                SetHookRopeWinding(Hooks[index], winding);

                //if (Hooks[index] != null && Hooks[index].Exists())
                //{
                //    if (!Hooks[index].isWinding && winding)
                //    {
                //        Function.Call(Hash.START_ROPE_WINDING, Hooks[index].rope);
                //        Hooks[index].isWinding = true;
                //    }
                //    else if (Hooks[index].isWinding && !winding)
                //    {
                //        Function.Call(Hash.STOP_ROPE_WINDING, Hooks[index].rope);
                //        Hooks[index].rope.ResetLength(true);
                //        Hooks[index].isWinding = false;
                //    }
                //}
            }
        }

        public static void SetHookRopeUnwinding(HookPair hook, bool unwinding)
        {
            if (hook != null && hook.IsValid())
            {
                //if (!hook.isUnwinding && unwinding)
                //{
                //    //Function.Call(Hash.START_ROPE_UNWINDING_FRONT, hook.rope);
                //    if (hook.rope.Length - RopeWindingSpeed < MAX_ROPE_LENGTH)
                //    {
                //        hook.rope.Length += RopeWindingSpeed;
                //    }
                //    else
                //    {
                //        hook.rope.Length = MAX_ROPE_LENGTH;
                //    }

                //    hook.isUnwinding = true;
                //}
                //else if (hook.isUnwinding && !unwinding)
                //{
                //Function.Call(Hash.STOP_ROPE_UNWINDING_FRONT, hook.rope);
                //hook.rope.ResetLength(true);
                hook.isUnwinding = unwinding;

                hook.isWinding = false;
                // }
            }
        }

        public static void SetHookRopeUnwindingByIndex(int index, bool unwinding)
        {
            if (index >= 0 && index < Hooks.Count)
            {
                SetHookRopeUnwinding(Hooks[index], unwinding);

                //if (Hooks[index] != null && Hooks[index].Exists())
                //{
                //    if (!Hooks[index].isUnwinding && unwinding)
                //    {
                //        Function.Call(Hash.START_ROPE_UNWINDING_FRONT, Hooks[index].rope);
                //        Hooks[index].isUnwinding = true;
                //    }
                //    else if (Hooks[index].isUnwinding && !unwinding)
                //    {
                //        Function.Call(Hash.STOP_ROPE_UNWINDING_FRONT, Hooks[index].rope);
                //        Hooks[index].rope.ResetLength(true);
                //        Hooks[index].isUnwinding = false;
                //    }
                //}
            }
        }


        public static void SetLastHookRopeWindingProc(bool winding)
        {
            if (Hooks.Count > 0)
            {
                int indexLastHook = Hooks.Count - 1;

                SetHookRopeWindingByIndex(indexLastHook, winding);
            }
        }

        public static void SetLastHookRopeUnwindingProc(bool unwind)
        {
            if (Hooks.Count > 0)
            {
                int lastHookIndex = Hooks.Count - 1;

                SetHookRopeUnwindingByIndex(lastHookIndex, unwind);
            }
        }

        public static void SetAllHookRopesWindingProc(bool winding)
        {
            for (int i = 0; i < Hooks.Count; i++)
            {
                SetHookRopeWindingByIndex(i, winding);
            }
        }

        public static void SetAllHookRopesUnwindingProc(bool unwinding)
        {
            for (int i = 0; i < Hooks.Count; i++)
            {
                SetHookRopeUnwindingByIndex(i, unwinding);
            }
        }


        public static void ToggleSolidRopesProc()
        {
            SolidRopes = !SolidRopes;

            SubQueue.AddSubtitle("VRope Solid Ropes: " + (SolidRopes ? "[ON]" : "(OFF)"), 24);
        }


        //public static void IncrementMIN_ROPE_LENGTH(bool negativeIncrement = false, bool halfIncrement = false)
        //{
        //    float lengthIncrement = (halfIncrement ? 0.5f : 1.0f);

        //    if (!negativeIncrement && MIN_ROPE_LENGTH < MAX_MIN_ROPE_LENGTH)
        //    {
        //        MIN_ROPE_LENGTH += lengthIncrement;
        //    }
        //    else if (negativeIncrement && MIN_ROPE_LENGTH > (MIN_MIN_ROPE_LENGTH + lengthIncrement))
        //    {
        //        MIN_ROPE_LENGTH -= lengthIncrement;
        //    }

        //    SubQueue.AddSubtitle(166, "VRope Minimum Rope Length: " + MIN_ROPE_LENGTH.ToString("0.00"), 17);
        //}


        public static void MultipleObjectSelectionProc()
        {
            if (Game.Player.Exists() && !Game.Player.IsDead &&
                Game.Player.CanControlCharacter && Game.Player.IsAiming &&
                SelectedHooks.Count < MAX_SELECTED_HOOKS)
            {
                RaycastResult rayResult = Util.CameraRaycastForward();
                HookPair selectedHook = new HookPair(RopeHook);
                Entity targetEntity = null;

                if (Util.GetEntityPlayerIsAimingAt(ref targetEntity) && targetEntity != null)
                {
                    selectedHook.entity1 = targetEntity;
                    selectedHook.hookPoint1 = rayResult.HitCoords;
                    selectedHook.hookOffset1 = (selectedHook.hookPoint1 != Vector3.Zero ? (selectedHook.hookPoint1 - selectedHook.entity1.Position) : Vector3.Zero);

                    SelectedHooks.Add(selectedHook);
                }
            }
        }


        public static void AttachPlayerToEntityProc()
        {
            try
            {
                if (CanUseModFeatures())
                {
                    RaycastResult rayResult = Util.CameraRaycastForward();
                    Entity targetEntity = null;

                    if (rayResult.DitHitAnything)
                    {
                        RopeHook.entity1 = Game.Player.Character;
                        RopeHook.hookPoint1 = Game.Player.Character.GetBoneCoord((Bone)57005);
                        RopeHook.hookPoint2 = rayResult.HitCoords;
                        RopeHook.ropeType = PlayerToEntityHookRopeType;
                        RopeHook.hookOffset1 = RopeHook.hookPoint1 - RopeHook.entity1.Position;

                        if (Util.GetEntityPlayerIsAimingAt(ref targetEntity))
                        {
                            RopeHook.entity2 = targetEntity;
                            RopeHook.hookOffset2 = (RopeHook.hookPoint2 != Vector3.Zero ? (RopeHook.hookPoint2 - RopeHook.entity2.Position) : Vector3.Zero);
                            RopeHook.isEntity2AMapPosition = false;
                        }
                        else
                        {
                            RopeHook.entity2 = null;
                            RopeHook.hookOffset2 = Vector3.Zero;
                            RopeHook.isEntity2AMapPosition = true;
                        }

                        if (FreeRangeMode ||
                            RopeHook.entity1.Position.DistanceTo(RopeHook.hookPoint2) < MaxHookCreationDistance)
                        {
                            CreateHook(RopeHook);
                        }

                        RopeHook.entity1 = null;
                        RopeHook.entity2 = null;
                    }
                }
            }
            catch (Exception exc)
            {
                UI.Notify("VRope AttachPlayerToEntity Error:\n" + GetErrorMessage(exc));
            }

        }

        public static void AttachEntityToEntityProc(bool chainRope = false)
        {
            Entity playerEntity = Game.Player.Character;
            RaycastResult rayResult = Util.CameraRaycastForward();
            Entity targetEntity = null;

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
                    hook.ropeType = EntityToEntityHookRopeType;

                    if (hasTargetEntity && targetEntity != null)
                    {
                        hook.hookOffset2 = (hook.hookPoint2 != Vector3.Zero ? (hook.hookPoint2 - hook.entity2.Position) : Vector3.Zero);
                        hook.isEntity2AMapPosition = false;
                    }
                    else if (rayResult.DitHitAnything)
                    {
                        hook.isEntity2AMapPosition = true;
                    }
                    else continue;

                    //if (!chainRope)
                    CreateHook(hook, true);
                    //else
                    //    CreateRopeChain(hook, true);
                }

                SelectedHooks.Clear();
                return;
            }


            if (Util.GetEntityPlayerIsAimingAt(ref targetEntity))
            {
                if (RopeHook.entity1 == null)
                {
                    if (RopeHook.entity2 != null)
                        RopeHook.entity2 = null;

                    RopeHook.entity1 = targetEntity;
                    RopeHook.hookPoint1 = rayResult.HitCoords;
                    RopeHook.hookOffset1 = (RopeHook.hookPoint1 != Vector3.Zero ? (RopeHook.hookPoint1 - RopeHook.entity1.Position) : Vector3.Zero);
                }
                else if (RopeHook.entity2 == null)
                {
                    RopeHook.entity2 = targetEntity;
                    RopeHook.hookPoint2 = rayResult.HitCoords;
                    RopeHook.hookOffset2 = (RopeHook.hookPoint2 != Vector3.Zero ? (RopeHook.hookPoint2 - RopeHook.entity2.Position) : Vector3.Zero);

                    //Player attachment not allowed here.
                    if (RopeHook.entity2 == RopeHook.entity1 ||
                        RopeHook.entity2 == playerEntity ||
                        RopeHook.entity1 == playerEntity)
                    {
                        RopeHook.entity1 = null;
                        RopeHook.entity2 = null;
                    }
                }

                if (RopeHook.entity1 != null && RopeHook.entity2 != null)
                {
                    if (RopeHook.entity1.Position.DistanceTo(RopeHook.entity2.Position) < MaxHookCreationDistance)
                    {
                        RopeHook.ropeType = EntityToEntityHookRopeType;
                        RopeHook.isEntity2AMapPosition = false;

                        //if (!chainRope)
                        CreateHook(RopeHook, true);
                        //else
                        //    CreateRopeChain(ropeHook, false);
                    }

                    RopeHook.entity1 = null;
                    RopeHook.entity2 = null;
                }
            }
            else if (rayResult.DitHitAnything)
            {
                if (RopeHook.entity1 != null && RopeHook.entity2 == null &&
                    //(ropeHook.entity1.Position.DistanceTo(rayResult.HitCoords) < MAX_HOOK_CREATION_DISTANCE))
                    (FreeRangeMode || RopeHook.entity1.Position.DistanceTo(rayResult.HitCoords) < MaxHookCreationDistance))
                {
                    RopeHook.hookPoint2 = rayResult.HitCoords;
                    RopeHook.ropeType = EntityToEntityHookRopeType;
                    RopeHook.isEntity2AMapPosition = true;
                    RopeHook.hookOffset2 = Vector3.Zero;

                    //if (!chainRope)
                    CreateHook(RopeHook, true);
                    //else
                    //    CreateRopeChain(ropeHook, true);
                }

                RopeHook.entity1 = null;
                RopeHook.entity2 = null;
            }
            else
            {
                RopeHook.entity1 = null;
                RopeHook.entity2 = null;
            }

        }

        public static void AttachEntityToEntityRopeProc()
        {
            AttachEntityToEntityProc(false);
        }


        public static void RecreateEntityHooks(Entity entity)
        {
            List<int> indexes = GetIndexOfHooksThatContains(entity);

            if (indexes != null)
            {
                for (int i = 0; i < indexes.Count; i++)
                {
                    RecreateEntityHook(indexes[i]);
                }
            }
        }

        public static void RecreateEntityHook(int hookIndex)
        {
            if (hookIndex >= 0 && hookIndex < Hooks.Count)
            {
                if (DebugMode)
                    UI.Notify("Recreating Entity Hook i:" + hookIndex);

                HookPair copyHook = new HookPair(Hooks[hookIndex]);

                DeleteHookByIndex(hookIndex, true);

                bool hookAtBoneCoords = (!copyHook.isTransportHook ? HookPedsAtBonesCoords : false);

                Hooks.Add(CreateEntityHook(copyHook, true, hookAtBoneCoords));
            }
        }



    }
}
