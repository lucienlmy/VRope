using GTA;
using System;
using System.Collections.Generic;

/*
 * 
 * created by jeffsturm4nn
 * 
 */

namespace VRope
{
    public class HookFilter
    {
        public static readonly List<HookFilter> DefaultFilters = new List<HookFilter> {

            new HookFilter("[ Vehicles ]","GTA.Vehicle"),
            new HookFilter("[ Peds ]","GTA.Ped"),
            //new HookFilter("[ Props ]","GTA.Prop"),
        };

        public String label { get; }
        public String filterValue { get; } 

        public HookFilter(String label, String filterValue)
        {
            this.label = label;
            this.filterValue = filterValue;
        }

        public bool matches(Entity entity)
        {
            if (entity == null || !entity.Exists())
                return false;

            return filterValue.Contains(entity.GetType().ToString());
        }
    }
}
