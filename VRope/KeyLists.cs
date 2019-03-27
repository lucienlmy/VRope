using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VRope
{
    public class KeyData
    {
        public String name;
        public List<Keys> list;
        public Action callback;

        public KeyData(string name, List<Keys> list, Action callback)
        {
            this.name = name;
            this.list = list;
            this.callback = callback;
        }
    }

    class KeyLists
    {
        private List<KeyData> keyLists = new List<KeyData>(30);

        public void Add(String keyListName, List<Keys> keyList, Action callback)
        {
            if (keyList == null || keyList.Count == 0)
                return;

            keyLists.Add(new KeyData(keyListName, keyList, callback));
        }

        public void SortBySize()
        {
            keyLists.Sort((x,y) => (-1 * x.list.Count.CompareTo(y.list.Count)));
        }

        public List<Keys> Get(String keyListName)
        {
            List<Keys> keyList = null;

            for(int i=0; i<keyLists.Count; i++)
            {
                if (keyLists[i].name == keyListName)
                    keyList = keyLists[i].list; break;
            }

            return keyList;
        }

        public List<KeyData> GetLists()
        {
            return keyLists;
        }
    }
}
