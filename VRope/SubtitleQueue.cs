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
    public class SubtitleQueue
    {
        private class SubtitleData
        {
            public long index = 0L;
            public String subtitle = "";
            public int duration = 1;

            public SubtitleData(long index, String subtitle, int duration = 1)
            {
                this.index = index;
                this.subtitle = subtitle;
                this.duration = duration;
            }
        }

        private List<SubtitleData> queue = new List<SubtitleData>(50);
        private bool blocked { get; set; } = false;

        private SubtitleData GetSubtitle(long index)
        {
            for (int i = 0; i < queue.Count; i++)
            {
                if (queue[i].index == index)
                    return queue[i];
            }
            
            return null;
        }


        public void AddSubtitle(String subtitle, int durationMs = 10)
        {
            if(!blocked)
                AddSubtitle(Environment.TickCount, subtitle, durationMs);
        }

        public void AddSubtitle(long index, String subtitle, int durationMs)
        {
            if (blocked || subtitle == null || subtitle.Length == 0 || durationMs < 1)
                return;

            durationMs /= Core.UPDATE_INTERVAL;

            SubtitleData subData = GetSubtitle(index);

            if (subData == null)
            {
                queue.Add(new SubtitleData(index, subtitle, durationMs));
            }
            else
            {
                subData.subtitle = subtitle;
                subData.duration = durationMs;
            }
        }

        public bool HasSubtitle(long index)
        {
            for(int i=0; i<queue.Count; i++)
            {
                if (queue[i].index == index)
                    return true;
            }

            return false;
        }

        public void RemoveSubtitle(long index)
        {
            if (index < 0 || index > queue.Count - 1)
                return;

            SubtitleData subData = GetSubtitle(index);

            if (subData != null)
                queue.Remove(subData);
        }

        public void RemoveAll()
        {
            queue.Clear();
        }

        public String MountSubtitle(bool decreaseDurations = true)
        {
            String subtitle = "";

            if (blocked)
                return subtitle;

            for(int i=0; i<queue.Count; i++)
            {
                if (queue[i].duration > 0)
                {
                    subtitle += queue[i].subtitle + "\n";

                    if (decreaseDurations)
                        queue[i].duration--;
                }
                else
                    queue.RemoveAt(i);
            }

            return subtitle;
        }

        public void ShowSubtitle()
        {
            if(!blocked)
                UI.ShowSubtitle(MountSubtitle());
        }
    }
}
