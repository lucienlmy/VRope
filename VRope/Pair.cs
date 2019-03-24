using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRope
{
    public class Pair<T, U>
    {
        public Pair()
        {
            
        }

        public Pair(T first, U second)
        {
            this.first = first;
            this.second = second;
        }

        public T first;
        public U second;
    };
}
