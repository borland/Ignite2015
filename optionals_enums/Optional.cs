using System;

public static class OptionalExtensions
{
    public static void Unwrap<T>(this Optional<T> optional, Action<T> some, Action none = null) where T : class
    {
        if (optional.HasValue)
            some(optional.Value);
        else if (none != null)
            none();
    }
    
    public static void Unwrap<T>(this T optional, Action<T> some, Action none = null) where T : class
    {
        if (optional != null)
            some(optional);
        else if (none != null)
            none();
    }
}

public struct Optional<T> where T : class
{
    readonly T m_value;

	public Optional(T value)
	{
        m_value = value;
	}

    public static implicit operator Optional<T>(T value) => new Optional<T>(value);

    public bool HasValue => m_value != null;

    public T Value
    {
        get
        {
            if (m_value == null)
                throw new InvalidOperationException($"No value for Optional<{typeof(T)}>");
            return m_value;
        }
    }

    public T UnsafeValue => m_value;
}


