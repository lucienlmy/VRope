using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTA;
using GTA.Math;
using GTA.Native;

namespace VRope
{
    public class HookPair
    {
        public Rope rope;
        public RopeType ropeType;

        public Entity entity1;
        public Entity entity2;

        public bool isEntity1AMapPosition;
        public bool isEntity2AMapPosition;

        public bool isEntity1ABalloon;

        public bool isWinding;
        public bool isUnwinding;

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
            this.isEntity1AMapPosition = other.isEntity1AMapPosition;
            this.isEntity2AMapPosition = other.isEntity2AMapPosition;
            this.isEntity1ABalloon = other.isEntity1ABalloon;
            this.isWinding = other.isWinding;
            this.isUnwinding = other.isUnwinding;
            this.hookPoint1 = other.hookPoint1;
            this.hookPoint2 = other.hookPoint2;
            this.rope = other.rope;
            this.ropeType = other.ropeType;
            this.hookOffset1 = other.hookOffset1;
            this.hookOffset2 = other.hookOffset2;
        }

        public bool Equals(HookPair other)
        {
            if (other == null)
                return false;

           return (this.isEntity2AMapPosition == other.isEntity2AMapPosition &&
                this.isEntity1ABalloon == other.isEntity1ABalloon &&
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
            this.ropeType = (RopeType)1;
            this.isEntity1AMapPosition = false;
            this.isEntity2AMapPosition = false;
            this.isWinding = false;
            this.isUnwinding = false;
            this.isEntity1ABalloon = false;
            this.hookPoint1 = Vector3.Zero;
            this.hookPoint2 = Vector3.Zero;
            this.hookOffset1 = Vector3.Zero;
            this.hookOffset2 = Vector3.Zero;
            this.rope = null;
            this.ropeType = (RopeType)4;
        }

        public void Delete()
        {
            if (Exists())
                rope.Delete();

            if (isEntity1AMapPosition && Util.IsProp(entity1))
            {
                Util.DeleteEntity(entity1);
            }

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

        public bool IsValid()
        {
            return (Exists() 
                && entity1 != null && entity2 != null 
                && entity1.Exists() && !(isEntity1AMapPosition && isEntity2AMapPosition)
                && entity2.Exists());
        }
    }
}
