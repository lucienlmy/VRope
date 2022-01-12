
using GTA;
using GTA.Math;

/*
 * 
 * created by jeffsturm4nn
 * 
 */

namespace VRope
{
    public class HookPair
    {
        private static uint _UniqueID = 707U;


        public uint ID { get; } = ++_UniqueID;

        public Rope rope;
        public RopeType ropeType;

        public Entity entity1;
        public Entity entity2;

        public bool isTransportHook;

        public bool isEntity2AMapPosition;

        public bool isEntity1ABalloon;
        public bool isEntity2ABalloon;

        public bool isWinding;
        public bool isUnwinding;

        //public bool isRopeDetachedFromPed;

        public Vector3 hookPoint1;
        public Vector3 hookPoint2;

        public Vector3 hookOffset1;
        public Vector3 hookOffset2;

        public HookPair()
        {
            Reset();
        }

        public HookPair(HookPair other)
        {
            CopyFrom(other);
        }

        public void CopyFrom(HookPair other)
        {
            this.rope = other.rope;
            this.entity1 = other.entity1;
            this.entity2 = other.entity2;
            this.ropeType = other.ropeType;
            this.isEntity2AMapPosition = other.isEntity2AMapPosition;
            this.isEntity1ABalloon = other.isEntity1ABalloon;
            this.isEntity2ABalloon = other.isEntity2ABalloon;
            this.isWinding = other.isWinding;
            this.isUnwinding = other.isUnwinding;
            this.hookPoint1 = other.hookPoint1;
            this.hookPoint2 = other.hookPoint2;
            this.rope = other.rope;
            this.ropeType = other.ropeType;
            this.hookOffset1 = other.hookOffset1;
            this.hookOffset2 = other.hookOffset2;
            this.isTransportHook = other.isTransportHook;
        }

        public HookPair Clone()
        {
            return new HookPair(this);
        }

        public bool Equals(HookPair other)
        {
            if (other == null)
                return false;

           return (this.isEntity2AMapPosition == other.isEntity2AMapPosition &&
                this.isEntity1ABalloon == other.isEntity1ABalloon &&
                this.isTransportHook == other.isTransportHook &&
                this.entity1 == other.entity1 &&
                this.entity2 == other.entity2 &&
                Util.Truncate(this.hookPoint1) == Util.Truncate(other.hookPoint1) &&
                Util.Truncate(this.hookPoint2) == Util.Truncate(other.hookPoint2) &&
                Util.Truncate(this.hookOffset1) == Util.Truncate(other.hookOffset1) &&
                Util.Truncate(this.hookOffset2) == Util.Truncate(other.hookOffset2));
        }

        private void Reset()
        {
            this.rope = null;
            this.entity1 = null;
            this.entity2 = null;
            this.isEntity2AMapPosition = false;
            this.isWinding = false;
            this.isUnwinding = false;
            this.isEntity1ABalloon = false;
            this.isEntity2ABalloon = false;
            this.hookPoint1 = Vector3.Zero;
            this.hookPoint2 = Vector3.Zero;
            this.hookOffset1 = Vector3.Zero;
            this.hookOffset2 = Vector3.Zero;
            this.rope = null;
            this.ropeType = (RopeType)4;
            this.isTransportHook = false;
        }

        public void Delete()
        {
            if (Exists())
                rope.Delete();

            if (isEntity2AMapPosition && Util.IsProp(entity2))
            {
                Util.DeleteEntity(entity2);
            }

            //Reset();
        }

        public bool HasPed()
        {
            return (Util.IsPed(entity1) || Util.IsPed(entity2));
        }

        public bool Exists()
        {
            return (rope != null && rope.Exists());
        }

        public void CalculateOffset1()
        {
            if (entity1 == null || !entity1.Exists())
                return;

            if(hookPoint1 != Vector3.Zero)
            {
                hookOffset1 = hookPoint1 - entity1.Position;
            }
            else
            {
                hookOffset1 = Vector3.Zero;
            }
        }

        public void CalculateOffset2()
        {
            if (entity2 == null || !entity2.Exists())
                return;

            if (hookPoint2 != Vector3.Zero)
            {
                hookOffset2 = hookPoint2 - entity2.Position;
            }
            else
            {
                hookOffset2 = Vector3.Zero;
            }
        }

        public bool IsValid()
        {
            return (Exists() && 
                entity1 != null && entity1.Exists() &&
                entity2 != null && entity2.Exists());
        }
    }
}
