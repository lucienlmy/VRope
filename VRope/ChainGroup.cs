using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTA;
using GTA.Math;

namespace VRope
{
    class ChainGroup
    {
        public const int MAX_CHAIN_SEGMENTS = 15;

        public List<HookPair> segments = new List<HookPair>(MAX_CHAIN_SEGMENTS);

        public float segmentLength;

        public ChainGroup()
        {

        }

        public int SegmentsCount()
        {
            return segments.Count;
        }

        public bool IsValid()
        {
            if (segments.Count == 0)
                return false;

            foreach(var seg in segments)
            {
                if (seg == null || !seg.IsValid())
                    return false;
            }

            return true;
        }

        public void Delete()
        {
            if(segments != null && segments.Count > 0)
            {
                for(int i=0; i<segments.Count; i++)
                {
                    if (segments[i] != null)
                    {
                        segments[i].Delete();

                        if(i < segments.Count - 1 && Util.IsProp(segments[i].entity2) || segments.Count == 1)
                        {
                            Util.DeleteEntity(segments[i].entity2);
                        }
                    }
                }

                segments.Clear();
            }
        }
    }
}
