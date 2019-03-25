using GTA;
using System;
using System.Collections.Generic;

namespace VRope
{
    class SubtitleQueue
    {
        private List<Tuple<int, String, int>> queue = new List<Tuple<int, string, int>>(50);

        public void AddSubtitle(int index, String subtitle, int durationMs)
        {
            if (subtitle == null || subtitle.Length == 0 || durationMs < 1)
                return;
            
            if(!HasSubtitle(index))
                queue.Add(new Tuple<int, string, int>(index, subtitle, durationMs));
        }

        public bool HasSubtitle(int index)
        {
            if (index < 0 || index > queue.Count - 1)
                return false;

            for(int i=0; i<queue.Count; i++)
            {
                if (queue[i].Item1 == index)
                    return true;
            }

            return false;
        }

        public void RemoveSubtitle(int index)
        {
            if (index < 0 || index > queue.Count - 1)
                return;

            queue.RemoveAt(index);
        }

        public void RemoveAll()
        {
            queue.Clear();
        }

        private String MountSubtitle()
        {
            String subtitle = "";

            for(int i=0; i<queue.Count; i++)
            {
                if (queue[i].Item3 > 0)
                    subtitle += queue[i] + "\n";
                else
                    queue.RemoveAt(i);
            }

            return subtitle;
        }

        public void ShowSubtitle()
        {
            UI.ShowSubtitle(MountSubtitle());
        }
    }
}
