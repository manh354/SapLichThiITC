using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SapLichThiITCAlgo
{
    public static class ComparerExtension
    {
        public static Comparer<T> MakeParentForComparer<T>(this Comparer<T> childComparer, Comparer<T> parentComparer )
        {
            return Comparer<T>.Create((x, y) =>
            {
                int result = parentComparer.Compare( x, y );
                return result != 0 ? result : childComparer.Compare(x, y);
            });
        }
        public static Comparer<T> MakeChildForComparer<T>(this Comparer<T> parentComparer, Comparer<T> childComparer)
        {
            return Comparer<T>.Create((x, y) =>
            {
                int result = parentComparer.Compare(x, y);
                return result != 0 ? result : childComparer.Compare(x, y);
            });
        }
    }

}
