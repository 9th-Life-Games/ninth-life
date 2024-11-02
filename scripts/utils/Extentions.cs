#nullable enable

namespace NinthLife.scripts.utils
{
    public static class Extensions
    {
        public static Maybe<T> ToMaybe<T>(this T? value)
            where T : class
        {
            return value != null ? Maybe<T>.Some(value) : Maybe<T>.None();
        }
    }
}
