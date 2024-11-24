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
    //Failed implementation
     public static class ChainModule
    {
        //public static void CreateRopeChain(HookPair hook, bool copyHook = true, bool hookAtBonePositions = true)
        //{
        //    if (hook.entity1 == null ||
        //          (hook.entity2 == null && !hook.isEntity2AMapPosition))
        //        return;

        //    Vector3 entity1HookPosition = hook.entity1.Position + hook.hookOffset1;
        //    Vector3 entity2HookPosition = Vector3.Zero;

        //    if (hook.isEntity2AMapPosition)
        //    {
        //        hook.entity2 = CreateChainJointProp(hook.hookPoint2);
        //        entity2HookPosition = hook.entity2.Position;
        //    }
        //    else
        //    {
        //        entity2HookPosition = hook.entity2.Position + hook.hookOffset2; //hook.hookPosition2
        //    }

        //    if (hookAtBonePositions)
        //    {
        //        if (Util.IsPed(hook.entity1) && !Util.IsPlayer(hook.entity1))
        //        {
        //            entity1HookPosition = Util.GetNearestBonePosition((Ped)hook.entity1, entity1HookPosition);
        //        }

        //        if (Util.IsPed(hook.entity2))
        //        {
        //            entity2HookPosition = Util.GetNearestBonePosition((Ped)hook.entity2, entity2HookPosition);
        //        }
        //    }

        //    Vector3 distanceVector = entity2HookPosition - entity1HookPosition;
        //    Vector3 lookAtDirection = distanceVector.Normalized;

        //    float ropeLength = entity1HookPosition.DistanceTo(entity2HookPosition); //TRY1
        //    float segmentLength = ropeLength / MAX_CHAIN_SEGMENTS;

        //    ChainGroup chain = new ChainGroup();
        //    chain.segmentLength = segmentLength;

        //    for (int i = 0; i < MAX_CHAIN_SEGMENTS; i++)
        //    {
        //        if (chain.SegmentsCount() == 0)
        //        {
        //            HookPair segmentPair = new HookPair();
        //            segmentPair.ropeType = ChainSegmentRopeType;
        //            segmentPair.isEntity2AMapPosition = false;

        //            segmentPair.entity1 = hook.entity1;

        //            segmentPair.entity2 = CreateChainJointProp(entity1HookPosition + (lookAtDirection * segmentLength));
        //            segmentPair.entity2.Position = entity1HookPosition + (lookAtDirection * segmentLength);
        //            segmentPair.hookPoint2 = segmentPair.entity2.Position;

        //            chain.segments.Add(CreateEntityHook(segmentPair, false, true, MIN_CHAIN_SEGMENT_LENGTH)); // TEST VALUE
        //        }
        //        else if (chain.SegmentsCount() > 0 && chain.SegmentsCount() < MAX_CHAIN_SEGMENTS - 1)
        //        {
        //            HookPair segmentPair = new HookPair();
        //            segmentPair.isEntity2AMapPosition = false;
        //            segmentPair.ropeType = ChainSegmentRopeType;

        //            HookPair lastSegmentPair = chain.segments.Last();

        //            segmentPair.entity1 = lastSegmentPair.entity2;
        //            segmentPair.hookPoint1 = lastSegmentPair.entity2.Position + (CHAIN_JOINT_OFFSET * lookAtDirection);

        //            segmentPair.entity2 = CreateChainJointProp(entity1HookPosition + lookAtDirection * segmentLength * (i + 1));
        //            segmentPair.hookPoint2 = segmentPair.entity2.Position;

        //            chain.segments.Add(CreateEntityHook(segmentPair, false, true, MIN_CHAIN_SEGMENT_LENGTH)); // TEST VALUE
        //        }
        //        else if (chain.SegmentsCount() == MAX_CHAIN_SEGMENTS - 1)
        //        {
        //            HookPair segmentPair = new HookPair(hook);
        //            segmentPair.ropeType = ChainSegmentRopeType;

        //            HookPair lastSegmentPair = chain.segments.Last();

        //            segmentPair.entity1 = lastSegmentPair.entity2;
        //            segmentPair.hookPoint1 = lastSegmentPair.hookPoint2 + (CHAIN_JOINT_OFFSET * lookAtDirection);
        //            segmentPair.hookOffset1 = lastSegmentPair.hookOffset2;

        //            chain.segments.Add(CreateEntityHook(segmentPair, false, true, MIN_CHAIN_SEGMENT_LENGTH));

        //            break;
        //        }
        //    }

        //    chains.Add(chain);
        //}


    }
}
