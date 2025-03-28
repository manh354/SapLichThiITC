namespace SapLichThiITCCore
{
    public static class ComparerExtension
    {
        public static Comparer<T> PutInParent<T>(this Comparer<T> childComparer, Comparer<T> parentComparer)
        {
            return Comparer<T>.Create((x, y) =>
            {
                int result = parentComparer.Compare(x, y);
                return result != 0 ? result : childComparer.Compare(x, y);
            });
        }
        public static Comparer<T> WrapOverChild<T>(this Comparer<T> parentComparer, Comparer<T> childComparer)
        {
            return Comparer<T>.Create((x, y) =>
            {
                int result = parentComparer.Compare(x, y);
                return result != 0 ? result : childComparer.Compare(x, y);
            });
        }
    }

}
