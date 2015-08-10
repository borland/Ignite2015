// This is free and unencumbered software released into the public domain.
// Anyone is free to copy, modify, publish, use, compile, sell, or
// distribute this software, either in source code form or as a compiled
// binary, for any purpose, commercial or non-commercial, and by any
// means.
using System;
using System.Diagnostics;

/// <summary>Pretty much exactly the same as Nullable but you can only use it for reference types (for value types use nullable)
/// Use this as a way to signal to callers that something is DELIBERATELY possibly null, as opposed to accidentally</summary>
/// <typeparam name="T"></typeparam>
/// <remarks>Not threadsafe. Can get torn pointer reads on 64-bit CLR and that kind of thing</remarks>
public struct Optional<T> where T : class
{
    readonly T m_value;

    public Optional(T value)
    { m_value = value; }

    /// <summary>Throws NullReferenceException if the wrapped value is actually null. Else returns it</summary>
    public T Value
    {
        get
        {
            if (m_value == null)
                throw new NullReferenceException($"Attempt to access .Value for an empty Optional<{typeof(T)}>");
            return m_value;
        }
    }

    /// <summary>Simply returns value with no null check</summary>
    public T UnsafeValue => m_value;

    public bool HasValue => m_value != null;

    public static implicit operator Optional<T>(T value) => new Optional<T>(value);

    public static bool operator ==(Optional<T> optional, object other)
    {
        if (other == null)
            return !optional.HasValue;

        if (other is Optional<T>)
        {
            Optional<T> otherOptional = (Optional<T>)other;
            return optional == otherOptional.m_value;
        }

        if (!optional.HasValue)
            return false;

        T value = optional.Value;
        Debug.Assert(value != null);

        return Object.Equals(value, other);
    }

    public static bool operator !=(Optional<T> optional, object other) => !(optional == other);

    public override bool Equals(object obj) => this == obj;

    public override int GetHashCode() => HasValue ? m_value.GetHashCode() : 0;
}

public static class OptionalExtensions
{
    public static T Unwrap<T>(this Optional<T> opt, T defaultValue) where T : class
    {
        if (defaultValue == null)
            throw new ArgumentException("Calling Unwrap with a defaultValue of null", "none");
        return opt.HasValue ? opt.UnsafeValue : defaultValue;
    }

    public static TResult Unwrap<T, TResult>(this Optional<T> opt, Func<T, TResult> some, TResult defaultValue = default(TResult)) where T :class
        => Unwrap(opt, some, () => defaultValue);

    public static TResult Unwrap<T, TResult>(this Optional<T> opt, Func<T, TResult> some, Func<TResult> none) where T : class
        => opt.HasValue ? some(opt.UnsafeValue) : none();

    public static void Unwrap<T>(this Optional<T> opt, Action<T> some) where T : class
        => Unwrap(opt, some, () => { });

    public static void Unwrap<T>(this Optional<T> opt, Action<T> some, Action none) where T : class
    {
        if (opt.HasValue)
            some(opt.UnsafeValue);
        else
            none();
    }
}

public static class OptionalExtensionsForPlainTypes
{
    public static T Unwrap<T>(this T value, T defaultValue) where T : class
    {
        if (defaultValue == null)
            throw new ArgumentException("Calling Unwrap with a defaultValue of null", "none");
        return value != null ? value : defaultValue;
    }

    public static TResult Unwrap<T, TResult>(this T value, Func<T, TResult> some, TResult defaultValue = default(TResult))
        => Unwrap(value, some, () => defaultValue);

    public static TResult Unwrap<T, TResult>(this T value, Func<T, TResult> some, Func<TResult> none)
        => value != null ? some(value) : none();

    public static void Unwrap<T>(this T value, Action<T> some)
        => Unwrap(value, some, () => { });

    public static void Unwrap<T>(this T value, Action<T> some, Action none)
    {
        if (value != null)
            some(value);
        else
            none();
    }
}

/// <summary>Helper methods for dealing with optionals</summary>
public static class Optional
{
    /// <summary>Creates a new Optional wrapping value. If value is null, will throw a NullReferenceException immediately</summary>
    public static Optional<T> Create<T>(T value) where T : class
    {
        if (value == null)
            throw new NullReferenceException($"Attempt to create Optional<{typeof(T)}> with null value. Use CreateMaybeNull if you expect nulls sometimes");
        return new Optional<T>(value);
    }

    /// <summary>Creates a new Optional wrapping value. No checks are performed</summary>
    public static Optional<T> CreateMaybeNull<T>(T value) where T : class => new Optional<T>(value);

    /// <summary>If the optional has a value, Invokes selector with that value and returns it's result.
    /// If the optional has no value, returns default(TResult)</summary>
    public static Optional<TResult> Select<T, TResult>(this Optional<T> opt1, Func<T, TResult> selector) where T : class where TResult : class 
        => opt1.Unwrap(selector);

    /// <summary>If both optionals have a value, Invokes selector with those values and returns it's result.
    /// If either optional has no value, returns default(TResult)</summary>
    public static Optional<TResult> Select<T1, T2, TResult>(Optional<T1> opt1, Optional<T2> opt2, Func<T1, T2, TResult> selector) where T1 : class where T2 : class where TResult : class 
        => opt1.Unwrap(o1 => opt2.Unwrap(o2 => selector(o1, o2)));

    /// <summary>If both optionals have a value, invokes action with those values.
    /// If neither has a value, does nothing</summary>
    /// <typeparam name="T1">Type of object1. Must be a reference type</typeparam>
    /// <typeparam name="T2">Type of object2. Must be a reference type</typeparam>
    /// <param name="opt1">The first optional value</param>
    /// <param name="opt2">The second optional value</param>
    /// <param name="action">Code to be run if both optionals have a value</param>
    public static void Unwrap<T1, T2>(Optional<T1> opt1, Optional<T2> opt2, Action<T1, T2> action) where T1 : class where T2 : class 
        => opt1.Unwrap(o1 => opt2.Unwrap(o2 => action(o1, o2)));

    /// <summary>If both optionals have a value, invokes action with those values.
    /// If neither has a value, does nothing</summary>
    /// <typeparam name="T1">Type of object1. Must be a reference type</typeparam>
    /// <typeparam name="T2">Type of object2. Must be a reference type</typeparam>
    /// <param name="opt1">The first optional value</param>
    /// <param name="opt2">The second optional value</param>
    /// <param name="action">Code to be run if both optionals have a value</param>
    public static void Unwrap<T1, T2>(T1 opt1, T2 opt2, Action<T1, T2> action) where T1 : class where T2 : class 
        => opt1.Unwrap(o1 => opt2.Unwrap(o2 => action(o1, o2)));
}
