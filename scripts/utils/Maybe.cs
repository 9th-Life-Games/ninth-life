using System;
using System.Diagnostics.CodeAnalysis;

namespace NinthLife.scripts.utils
{
    public readonly struct Maybe<T>
    {
        private readonly T _value;

        public bool IsSome { get; }
        public bool IsNone => !IsSome;

        private Maybe(T value, bool hasValue)
        {
            _value = value;
            IsSome = hasValue;
        }

        [SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
        public static Maybe<T> None()
        {
            return new Maybe<T>(default, false);
        }

        [SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
        public static Maybe<T> Some(T value)
        {
            return new Maybe<T>(value, true);
        }

        public T Unwrap()
        {
            return !IsSome
                ? throw new InvalidOperationException("Attempted to unwrap a None value")
                : _value;
        }

        public T UnwrapOrElse(Func<T> fallback)
        {
            return IsSome ? _value : fallback();
        }

        public T UnwrapOr(T fallback)
        {
            return IsSome ? _value : fallback;
        }

        public T Expect(string errorMessage)
        {
            return IsSome ? _value : throw new InvalidOperationException(errorMessage);
        }

        public T UnwrapOrDefault()
        {
            return IsSome ? _value : default;
        }

        public override string ToString()
        {
            return IsSome ? $"Some({_value})" : "None";
        }
    }
}
