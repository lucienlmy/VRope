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
        }

        public HookGroup(Rope rope, RopeType ropeType, Entity entity1, Vector3 hookPoint1, Entity entity2, Vector3 hookPoint2,
                         bool isEntity2AMapPosition = false, bool isEntity1APed = false, bool isEntity2APed = false)
        {
            this.rope = rope;
            this.entity1 = entity1;
            this.entity2 = entity2;
            this.hookPoint1 = hookPoint1;
            this.hookPoint2 = hookPoint2;
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
            this.hookPoint1 = new Vector3(0, 0, 0);
            this.hookPoint2 = new Vector3(0, 0, 0);
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
