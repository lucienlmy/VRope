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
    public class HookGroup
    {
        public Rope rope;
        public RopeType ropeType;

        public Entity entity1;
        public Entity entity2;

        public bool isEntity2AMapPosition;

        public Vector3 hookPoint1;
        public Vector3 hookPoint2;

        public Vector3 hookOffset1;
        public Vector3 hookOffset2;

        public bool isWinding;
        public bool isUnwinding;

        public HookGroup()
        {
            Reset();
        }

        public HookGroup(HookGroup other)
        {
            this.rope = other.rope;
            this.entity1 = other.entity1;
            this.entity2 = other.entity2;
            this.ropeType = other.ropeType;
            this.isEntity2AMapPosition = other.isEntity2AMapPosition;
            this.isWinding = other.isWinding;
            this.isUnwinding = other.isUnwinding;
            this.hookPoint1 = other.hookPoint1;
            this.hookPoint2 = other.hookPoint2;
            this.rope = other.rope;
            this.ropeType = other.ropeType;
            this.hookOffset1 = other.hookOffset1;
            this.hookOffset2 = other.hookOffset2;
        }

        public HookGroup(Rope rope, RopeType ropeType, Entity entity1, Vector3 hookPoint1, Vector3 hookOffset1, Entity entity2, Vector3 hookPoint2,
                         Vector3 hookOffset2, bool isEntity2AMapPosition = false, bool isEntity1APed = false, bool isEntity2APed = false)
        {
            this.rope = rope;
            this.entity1 = entity1;
            this.entity2 = entity2;
            this.hookPoint1 = hookPoint1;
            this.hookPoint2 = hookPoint2;
            this.hookOffset1 = hookOffset1;
            this.hookOffset2 = hookOffset2;
            this.ropeType = ropeType;
            this.isEntity2AMapPosition = isEntity2AMapPosition;
            this.isWinding = false;
            this.isUnwinding = false;
        }

        private void Reset()
        {
            this.rope = null;
            this.entity1 = null;
            this.entity2 = null;
            this.ropeType = (RopeType)1;
            this.isEntity2AMapPosition = false;
            this.isWinding = false;
            this.isUnwinding = false;
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
                && entity1.Exists() && entity2.Exists());
        }
    }
}
