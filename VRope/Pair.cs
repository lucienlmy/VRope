

/*
 * 
 * created by jeffsturm4nn
 * 
 */

namespace VRope
{
    public class Pair<T, U>
    {
        public T first;
        public U second;

        public Pair()
        {
        }

        public Pair(T first, U second)
        {
            this.first = first;
            this.second = second;
        }

        public override string ToString()
        {
            return ("[" + first.ToString() + "][" + second.ToString() + "]");
        }
    }

    static class Pair
    {
        public static Pair<T, U> Make<T, U>(T t, U u)
        {
            return new Pair<T, U>(t, u);
        }
    }
}
