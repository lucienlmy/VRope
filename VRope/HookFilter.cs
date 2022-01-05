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

            new HookFilter("[ VEHICLES ]","GTA.Vehicle"),
            new HookFilter("[ PEDS ]","GTA.Ped"),
            new HookFilter("[ PROPS ]","GTA.Prop"),

            //new HookFilter("[ VEHICLES ] & [ PEDS ]","GTA.Vehicle | GTA.Ped"),
            new HookFilter("[ PEDS ] & [ PROPS ]","GTA.Ped | GTA.Prop"),
            new HookFilter("[ PROPS ] & [ VEHICLES ]","GTA.Prop | GTA.Vehicle"),

            //new HookFilter("{ ALL }","GTA.Prop | GTA.Vehicle | GTA.Ped")
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
