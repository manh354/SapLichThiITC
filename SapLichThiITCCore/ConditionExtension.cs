namespace SapLichThiITCCore
{
    public static class ConditionExtension
    {
        public static Func<T, bool> PutInParent<T>(this Func<T, bool> child, Func<T, bool> parent)
        {
            return (t) =>
            {
                if (child == null)
                {
                    return parent(t);
                }
                if (parent(t))
                    return child(t);
                else return false;
            };
        }

        public static Func<T, bool> WrapOverChild<T>(this Func<T, bool> parent, Func<T, bool> child)
        {
            return (t) =>
            {
                if (parent(t))
                {
                    return child(t);
                }
                else return false;
            };
        }

        public static Func<T, bool> And<T>(this Func<T, bool> func, bool condition)
        {
            return (t) =>
            {
                return condition && func(t);
            };
        }

        public static Func<T, bool> And<T>(this Func<T, bool> func1, Func<T, bool> func2)
        {
            return (t) =>
            {
                return func1(t) && func2(t);
            };
        }
    }
}
