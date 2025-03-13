using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SapLichThiITCAlgo
{
    public static class ListExtension
    {
        public static T GetLargest<T>(this List<T> list, Comparer<T> comparer)
        {
            if (list == null || list.Count == 0)
                throw new ArgumentException("List cannot be null or empty");

            T largest = list[0];
            foreach (var item in list)
            {
                if (comparer.Compare(item, largest) > 0)
                {
                    largest = item;
                }
            }
            return largest;
        }

        public static T GetSmallest<T>(this List<T> list, Comparer<T> comparer)
        {
            if (list == null || list.Count == 0)
                throw new ArgumentException("List cannot be null or empty");

            T smallest = list[0];
            foreach (var item in list)
            {
                if (comparer.Compare(item, smallest) < 0)
                {
                    smallest = item;
                }
            }
            return smallest;
        }

        public static void SortDescending<T>(this List<T> list, Comparer<T> comparer)
        {
            if (list == null || list.Count == 0)
                throw new ArgumentException("List cannot be null or empty");

            list.Sort((a, b) => comparer.Compare(b, a));
        }
    }

}
